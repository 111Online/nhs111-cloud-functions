using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace NHS111.Cloud.Functions.Email
{
    public static class OrchestrateSendMail
    {
        [FunctionName("OrchestrateSendMail")]
        public static async Task<string> Run([OrchestrationTrigger] DurableOrchestrationContext orchestrationClient, TraceWriter log)
        {
            log.Info("C# OrchestrateSendMail trigger function processed a request.");

            var jsonSendMail = orchestrationClient.GetInput<string>();
            log.Info($"Calling function NHS111OnlineMailSender");
            var response = await orchestrationClient.CallActivityAsync<HttpResponseMessage>("NHS111OnlineMailSender", jsonSendMail);

            return orchestrationClient.InstanceId;
        }
    }
}
