using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace NHS111.Cloud.Functions.Email
{
    public static class OrchestrateSendMail
    {
        [FunctionName("OrchestrateSendMail")]
        public static async Task<object> Run([OrchestrationTrigger] DurableOrchestrationContext orchestrationClient, TraceWriter log)
        {
            var jsonSendMail = orchestrationClient.GetInput<string>();
            log.Info($"Calling function NHS111OnlineMailSender");
            var jsonDataRecords = await orchestrationClient.CallActivityAsync<string>("NHS111OnlineMailSender", jsonSendMail);

            return orchestrationClient.InstanceId;
        }
    }
}
