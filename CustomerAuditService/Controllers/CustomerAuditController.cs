using System;
using System.Net;
using System.Threading.Tasks;
using System.Text.Json;
using Dapr;
using Dapr.Client;
using CustomerAuditService.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CustomerAuditService.Controllers
{
    [ApiController]
    public class CustomerAuditController : ControllerBase
    {
        private DaprClient client;
        private ILogger<CustomerAuditController> logger;

        public CustomerAuditController(DaprClient client, ILogger<CustomerAuditController> logger)
        {
            this.client = client;
            this.logger = logger;
        }

        [HttpPost("/order")]
        public async Task<IActionResult> HandleOrderEvent([FromBody] CustomerOrder order)
        {
            logger.LogInformation("Received order on Customer Audit Service: {@CustomerOrder}", order);
            try 
            {
                // Output order to Azure Blob via Dapr binding
                var data = order;
                var jsonData = JsonSerializer.Serialize(data);
                var byteData = System.Text.Encoding.ASCII.GetBytes(jsonData);

                var bindingGetRequest = new BindingRequest("order-binding", "create");
                bindingGetRequest.Data = byteData;
                var bindingResponse = await client.InvokeBindingAsync(bindingGetRequest);

                logger.LogInformation("Successfully output order for auditing: {@CustomerOrder}", order);

                // attempting 
                await HandleOrderEventNoLock(order);
            }
            catch (Exception e)
            {
                logger.LogError("Error on outputting order event: {@CustomerOrder}, Message: {Message}", order, e.InnerException?.Message ?? e.Message);
                return Problem(e.Message, null, (int)HttpStatusCode.InternalServerError);
            }
            return Ok();
        }

        [Obsolete]
        private async Task<IActionResult> HandleOrderEventNoLock(CustomerOrder order)
        {
            logger.LogInformation("(no lock) Received order on Customer Audit Service: {@CustomerOrder}", order);
            try 
            {
                // Output order to Azure Blob via Dapr binding
                var data = order;
                var jsonData = JsonSerializer.Serialize(data);
                var byteData = System.Text.Encoding.ASCII.GetBytes(jsonData);

                var bindingGetRequest = new BindingRequest("order-binding-nolock", "create");
                bindingGetRequest.Data = byteData;
                var bindingResponse = await client.InvokeBindingAsync(bindingGetRequest);

                logger.LogInformation("(no lock) Successfully output order for auditing: {@CustomerOrder}", order);
            }
            catch (Exception e)
            {
                logger.LogError("(no lock) Error on outputting order event: {@CustomerOrder}, Message: {Message}", order, e.InnerException?.Message ?? e.Message);
                return Problem(e.Message, null, (int)HttpStatusCode.InternalServerError);
            }
            return Ok();
        }
    }
}
