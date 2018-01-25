using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace NHS111.Cloud.Functions.Email
{
    public static class StatusSendMail
    {
        [FunctionName("StatusSendMail")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", Route = "StatusSendMail/{instanceId}")]HttpRequestMessage req, [OrchestrationClient]DurableOrchestrationClient starter, string instanceId, TraceWriter log)
        {
            log.Info($"C# StatusSendMail trigger function processed for {instanceId}.");
            var status = await starter.GetStatusAsync(instanceId);
            
            switch (status.RuntimeStatus)
            {
                case OrchestrationRuntimeStatus.Completed:
                    return req.CreateResponse(HttpStatusCode.OK, status);
                case OrchestrationRuntimeStatus.Failed:
                    return req.CreateResponse(HttpStatusCode.InternalServerError, status);
                default:
                    return req.CreateResponse(HttpStatusCode.Accepted, status);
            }
        }
    }
}
