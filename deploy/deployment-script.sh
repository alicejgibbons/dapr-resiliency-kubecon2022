UNIQUE_SUFFIX=kubecon2022
# Remove Underscores and Dashes (Not Allowed in AKS and ACR Names)
UNIQUE_SUFFIX="${UNIQUE_SUFFIX//_}"
UNIQUE_SUFFIX="${UNIQUE_SUFFIX//-}"

# Check Unique Suffix Value (Should be No Underscores or Dashes)
echo $UNIQUE_SUFFIX

RGNAME=aks-rg-$UNIQUE_SUFFIX
LOCATION=eastus
KV_NAME=kv-$UNIQUE_SUFFIX
CLUSTERNAME=aks${UNIQUE_SUFFIX}
SUBSCRIPTION_ID=$(az account show --query id --output tsv)

# Look at AKS Cluster Name for Future Reference
echo $CLUSTERNAME

# Get list of AKS versions for the location 
az aks get-versions -l $LOCATION --output table
K8SVERSION=1.24.6

# ------------ Deploy AKS cluster --------------------------------------

# Create Resource Group
az group create -n $RGNAME -l $LOCATION

# Create AKS Cluster
az aks create -n $CLUSTERNAME -g $RGNAME \
--kubernetes-version $K8SVERSION \
--enable-managed-identity \
--generate-ssh-keys -l $LOCATION \
--node-count 3 \
--no-wait

az aks list -o table
az aks get-credentials -n $CLUSTERNAME -g $RGNAME
kubectl cluster-info

# ------------ Set up AKS cluster --------------------------------------

k create ns dapr-resiliency

# Install Dapr
helm repo add dapr https://dapr.github.io/helm-charts/
helm repo update

helm upgrade --install dapr dapr/dapr \
--version=1.9 \
--namespace dapr-system \
--create-namespace \
--wait

# Install Redis for state store
kubectl create ns redis
helm install redis bitnami/redis -n redis --set architecture=standalone # need single replica for lock api
export REDIS_PASSWORD=$(kubectl get secret --namespace redis redis -o jsonpath="{.data.redis-password}" | base64 -d) 
kubectl create secret generic redis-password --from-literal=redis-password=$REDIS_PASSWORD -n dapr-resiliency

# Install Kafka for pub/sub
kubectl create ns kafka
helm install --set persistence.enabled=false --set zookeeper.persistence.enabled=false --set auth.clientProtocol=sasl kafka bitnami/kafka -n kafka
export KAFKA_PASSWORD=$(kubectl get secret kafka-jaas --namespace kafka -o jsonpath='{.data.client-passwords}' | base64 -d | cut -d , -f 1)
kubectl create secret generic kafka-password --from-literal=kafka-password=$KAFKA_PASSWORD -n dapr-resiliency

# Install Zipkin for tracing
kubectl create ns zipkin
kubectl create deployment zipkin -n zipkin --image openzipkin/zipkin
kubectl expose deployment zipkin -n zipkin --type LoadBalancer --port 9411 
kubectl get svc -n zipkin -w
export ZIPKIN_DASHBOARD=$(kubectl get svc --namespace zipkin zipkin -o jsonpath="{.status.loadBalancer.ingress[0].ip}"):9411
echo "View tracing dashboard at $ZIPKIN_DASHBOARD"
kubectl apply -f components/k8s/config.yaml

# Create blob storage, copy access key
kubectl create secret generic blob-key --from-literal=blob-key="jD5VEG3xmb+ehh5GTDmByB6A3bWS22YuSIIhE4eAyV2pnLpFbEvurTmlonRz6DObPSsN/71dmDk4+AStRpw7tg==" -n dapr-resiliency

# Apply dapr components
kubectl apply -f components/k8s/

# Create ACR and attach it to AKS
echo $UNIQUE_SUFFIX
export ACRNAME=acr$UNIQUE_SUFFIX
echo $ACRNAME
az acr create --resource-group $RGNAME --name $ACRNAME --sku Basic
az aks update -n $CLUSTERNAME -g $RGNAME --attach-acr $ACRNAME

# Build locally (from root dir)
# docker build . -f ./CustomerOrderService/Dockerfile    
# docker build . -f ./CustomerLoyaltyJob/Dockerfile   
# docker build . -f ./CustomerAuditService/Dockerfile   

# Build services for ACR (from root dir)
az acr build -t dapr-resiliency/customer-order-service:1.0 -r $ACRNAME -f ./CustomerOrderService/Dockerfile .
az acr build -t dapr-resiliency/customer-audit-service:1.0 -r $ACRNAME -f ./CustomerAuditService/Dockerfile .
az acr build -t dapr-resiliency/customer-loyalty-job:1.0 -r $ACRNAME -f ./CustomerLoyaltyJob/Dockerfile .
az acr build -t dapr-resiliency/customer-loyalty-job-nolock:1.0 -r $ACRNAME -f ./CustomerLoyaltyJobNoLock/Dockerfile .

# In the UI folder:
az acr build -t dapr-resiliency/frontend:1.0 -r acrkubecon2022  . 

# deploy apps
k apply -f deploy/manifests/
