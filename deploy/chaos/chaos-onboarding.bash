RESOURCE_GROUP=aks-rg-kubecon2022
CLUSTER_NAME=akskubecon2022
RESOURCE_ID=/subscriptions/43e73082-4b78-41c9-b27f-49a6969248ef/resourceGroups/aks-rg-kubecon2022/providers/Microsoft.ContainerService/managedClusters/akskubecon2022
CAPABILITY=NetworkChaos-2.1
SUBSCRIPTION_ID=43e73082-4b78-41c9-b27f-49a6969248ef
EXPERIMENT_NAME=NetworkFaultTest
PRINCIPAL_ID=de62be02-a0a6-4ff3-904b-ee98b01368b7

az aks get-credentials -g $RESOURCE_GROUP -n $CLUSTER_NAME
helm repo add chaos-mesh https://charts.chaos-mesh.org
helm repo update
kubectl create ns chaos-testing
helm install chaos-mesh chaos-mesh/chaos-mesh --namespace=chaos-testing --set chaosDaemon.runtime=containerd --set chaosDaemon.socketPath=/run/containerd/containerd.sock

# Validate 
kubectl get po -n chaos-testing 

# Onboard the AKS cluster to chaos studio 
az rest --method put --url "https://management.azure.com/$RESOURCE_ID/providers/Microsoft.Chaos/targets/Microsoft-AzureKubernetesServiceChaosMesh?api-version=2021-09-15-preview" --body "{\"properties\":{}}"

# Create capabilities on the target AKS cluster 
az rest --method put --url "https://management.azure.com/$RESOURCE_ID/providers/Microsoft.Chaos/targets/Microsoft-AzureKubernetesServiceChaosMesh/capabilities/$CAPABILITY?api-version=2021-09-15-preview"  --body "{\"properties\":{}}"

# Create the experiment on the target AKS Cluster 
az rest --method put --uri https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Chaos/experiments/$EXPERIMENT_NAME?api-version=2021-09-15-preview --body @experiment.json

# Give the experiment principal ID permissions on the AKS target cluster
az role assignment create --role "0ab0b1a8-8aac-4efd-b8c2-3ee1fb270be8" --assignee-principal-type "ServicePrincipal" --assignee-object-id $PRINCIPAL_ID --scope $RESOURCE_ID

# Run the experiment 
az rest --method post --uri https://management.azure.com/subscriptions/$SUBSCRIPTION_ID/resourceGroups/$RESOURCE_GROUP/providers/Microsoft.Chaos/experiments/$EXPERIMENT_NAME/start?api-version=2021-09-15-preview