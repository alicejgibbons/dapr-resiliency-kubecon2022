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
using CustomerLoyaltyJob.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using System.Net.Http;

namespace CustomerLoyaltyJob.Controllers
{
    [ApiController]
    public class CustomerLoyaltyController : ControllerBase
    {
        private DaprClient client;
        private ILogger<CustomerLoyaltyController> logger;
        private const string StateStoreName = "loyalty-store";
        private const string LockName = "loyalty-lock";
        private const string AppId = "customer-loyalty-job";
        private readonly StateOptions _stateOptions = new StateOptions(){ Concurrency = ConcurrencyMode.FirstWrite, Consistency = ConsistencyMode.Eventual };

        public CustomerLoyaltyController(DaprClient client, ILogger<CustomerLoyaltyController> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        [HttpPost("/checkorders")]
        [Obsolete]
        public async Task<IActionResult> UpdateLoyaltyPoints()
        {
            logger.LogInformation($"Checking for customer orders on {AppId}");
            
            var data = new {
                maxResults = 10
            };

            var jsonData = JsonSerializer.Serialize(data);
            var byteData = System.Text.Encoding.ASCII.GetBytes(jsonData);

            var bindingGetRequest = new BindingRequest("order-binding", "list");
            bindingGetRequest.Data = byteData;
            var bindingResponse = await client.InvokeBindingAsync(bindingGetRequest);

            var rawData = bindingResponse.Data.ToArray();
            var orders = JsonSerializer.Deserialize<BlobItem[]>(rawData);

            if (orders != null)
            {
                logger.LogInformation("Found orders to process...");
                foreach (var order in orders.Select(order => order.Name).OrderBy(order => order))
                {
                    logger.LogInformation("Attempting to process: {0}", order);
                    await AttemptToProcessOrder(order);
                }
            }
            
            return Ok();
        }

        [Obsolete]
        private async Task AttemptToProcessOrder(string orderId)
        {
            // Locks are Disposable and will automatically unlock at the end of a 'using' statement.
            logger.LogInformation($"Attempting to lock order: {orderId}");
            await using (var orderLock = await client.Lock(LockName, orderId, AppId, 60))
            {
                if (orderLock.Success)
                {
                    logger.LogInformation($"Successfully locked order: {orderId}");

                    // Get the order after we've locked it, we're safe here because of the lock.
                    var order = await GetOrder(orderId);

                    if (order == null)
                    {
                        logger.LogWarning($"Loyalty points for order {orderId} has already been processed!");
                        return;
                    }

                    // Update loyalty points of the customer order before committing the order to storage
                    var newLoyaltyPoints = (int)Math.Round(order.OrderTotal, 0);
                    logger.LogWarning("Updating loyalty points: {@CustomerOrder}", order);

                    bool isSuccess;
                    StateEntry<CustomerOrder> stateEntry = null;

                    // Save order result to remote storage.
                    do
                    {
                        stateEntry = await client.GetStateEntryAsync<CustomerOrder>(StateStoreName, order.CustomerId.ToString());
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

                    // Remove order from Azure blob storage
                    logger.LogWarning($"Deleting processed order: {orderId}");
                    var bindingDeleteRequest = new BindingRequest("order-binding", "delete");
                    bindingDeleteRequest.Metadata["blobName"] = orderId;
                    await client.InvokeBindingAsync(bindingDeleteRequest);

                    logger.LogInformation($"Done processing order: {orderId}");
                }
                else
                {
                    logger.LogWarning($"Failed to lock order: {orderId}");
                }
            }
        }

        private async Task<CustomerOrder> GetOrder(string orderId)
        {
            try
            {
                var bindingGetRequest = new BindingRequest("order-binding", "get");
                bindingGetRequest.Metadata["blobName"] = orderId;

                var bindingResponse = await client.InvokeBindingAsync(bindingGetRequest);
                var rawData = bindingResponse.Data.ToArray();
                string stringResult = System.Text.Encoding.UTF8.GetString(bindingResponse.Data.ToArray());

                CustomerOrder order = JsonSerializer.Deserialize<CustomerOrder>(stringResult);
                return order;
            }
            catch (DaprException)
            {
                return null;
            }            
        }
    }
}
