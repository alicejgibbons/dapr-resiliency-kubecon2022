using System;
using System.Net;
using System.Threading.Tasks;
using Dapr.Client;
using CustomerOrderService.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CustomerOrderService.Controllers
{
    [ApiController]
    public class CustomerOrderController : ControllerBase
    {
        private DaprClient client;
        private ILogger<CustomerOrderController> logger;
        private const string Topic = "order";
        private const string PubSubName = "order-queue";
        private const string AppId = "customer-order-service";

        public CustomerOrderController(DaprClient client, ILogger<CustomerOrderController> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        [HttpPost("/order")]
        public async Task<IActionResult> CustomerOrder(CustomerOrder order)
        {
            logger.LogInformation("Received customer order request: {@CustomerOrder}", order);
            logger.LogInformation("Publishing customer order on topic: {@CustomerOrder}", order);
        
            try
            {
                await client.PublishEventAsync<CustomerOrder>(PubSubName, Topic, order);
                logger.LogInformation("Published customer order: {@CustomerOrder}", order);
            }
            catch(Exception e)
            {
                logger.LogError("Error publishing customer order on queue: {@CustomerOrder}, Message: {Message}", order, e.InnerException?.Message ?? e.Message);
                return Problem(e.Message, null, (int)HttpStatusCode.InternalServerError);
            }

            return Ok($"{AppId}: successful invocation. Order published on topic.");
        }
    }
}
