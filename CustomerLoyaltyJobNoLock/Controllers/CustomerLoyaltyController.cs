using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapr;
using Dapr.Client;
using CustomerLoyaltyJobNoLock.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System.Net.Http;

namespace CustomerLoyaltyJobNoLock.Controllers
{
    [ApiController]
    public class CustomerLoyaltyController : ControllerBase
    {
        private DaprClient client;
        private ILogger<CustomerLoyaltyController> logger;
        private const string StateStoreNameNoLock = "loyalty-store-nolock";
        private const string AppId = "customer-loyalty-job-nolock";
        private readonly StateOptions _stateOptions = new StateOptions(){ Concurrency = ConcurrencyMode.FirstWrite, Consistency = ConsistencyMode.Eventual };

        public CustomerLoyaltyController(DaprClient client, ILogger<CustomerLoyaltyController> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        [HttpPost("/checkorders-nolock")]
        [Obsolete]
        public async Task<IActionResult> UpdateLoyaltyPointsNoLock()
        {
            logger.LogInformation($"(no lock) Checking for customer orders on {AppId}");
            
            var data = new {
                maxResults = 10
            };

            var jsonData = JsonSerializer.Serialize(data);
            var byteData = System.Text.Encoding.ASCII.GetBytes(jsonData);

            var bindingGetRequest = new BindingRequest("order-binding-nolock", "list");
            bindingGetRequest.Data = byteData;
            var bindingResponse = await client.InvokeBindingAsync(bindingGetRequest);

            var rawData = bindingResponse.Data.ToArray();
            var orders = JsonSerializer.Deserialize<BlobItem[]>(rawData);

            if (orders != null)
            {
                logger.LogInformation("(no lock) Found orders to process...");
                foreach (var order in orders.Select(order => order.Name).OrderBy(order => order))
                {
                    logger.LogInformation("(no lock) Attempting to process: {0}", order);
                    await AttemptToProcessOrderNoLock(order);
                    
                }
            }
            
            return Ok();
        }

        [Obsolete]
        private async Task AttemptToProcessOrderNoLock(string orderId)
        {
            // Get the order to update
            var order = await GetOrderNoLock(orderId);

            if (order == null)
            {
                logger.LogWarning($"(no lock) Loyalty points for order {orderId} has already been processed!");
                return;
            }

            // Update loyalty points of the customer order before committing the order to storage
            var newLoyaltyPoints = (int)Math.Round(order.OrderTotal, 0);
            logger.LogWarning("(no lock) Updating loyalty points: {@CustomerOrder}", order);

            bool isSuccess;
            StateEntry<CustomerOrder> stateEntry = null;

            // Save order result to remote storage.
            do
            {
                stateEntry = await client.GetStateEntryAsync<CustomerOrder>(StateStoreNameNoLock, order.CustomerId.ToString());
                stateEntry.Value ??= new CustomerOrder()
                {
                    CustomerId= order.CustomerId,
                    OrderTotal = order.OrderTotal,
                    LoyaltyPoints = 0,
                    OrderCount = 0
                };
                stateEntry.Value.LoyaltyPoints += newLoyaltyPoints;
                stateEntry.Value.OrderCount += 1;
                isSuccess = await stateEntry.TrySaveAsync(_stateOptions);
            }
            while(!isSuccess);

            // Try removing it from Azure storage bucket, sometimes fails since it has already been removed.
            logger.LogWarning($"(no lock) Deleting processed order: {orderId}");
            try {

                var bindingDeleteRequest = new BindingRequest("order-binding-nolock", "delete");
                bindingDeleteRequest.Metadata["blobName"] = orderId;
                await client.InvokeBindingAsync(bindingDeleteRequest);

                logger.LogInformation($"(no lock) Done processing order: {orderId}");

            } catch (DaprException)
            {
                logger.LogInformation($"(no lock) Order has been deleted already: {orderId}");
            } 
        }

        private async Task<CustomerOrder> GetOrderNoLock(string orderId)
        {
            try
            {
                var bindingGetRequest = new BindingRequest("order-binding-nolock", "get");
                bindingGetRequest.Metadata["blobName"] = orderId;

                var bindingResponse = await client.InvokeBindingAsync(bindingGetRequest);
                var rawData = bindingResponse.Data.ToArray();
                string result = System.Text.Encoding.UTF8.GetString(bindingResponse.Data.ToArray());

                var order = JsonSerializer.Deserialize<CustomerOrder>(result);
                return order;
            }
            catch (DaprException)
            {
                return null;
            }            
        }
    }
}
