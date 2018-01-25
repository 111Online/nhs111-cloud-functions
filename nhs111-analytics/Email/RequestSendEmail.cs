using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Models.Email;

namespace NHS111.Cloud.Functions.Email
{
    public static class RequestSendEmail
    {
        [FunctionName("RequestSendEmail")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post")]HttpRequestMessage req, [OrchestrationClient]DurableOrchestrationClient starter, TraceWriter log)
        {
            log.Info("C# RequestSendEmail trigger function processed a request.");
            var sendMail = await req.Content.ReadAsAsync<SendMail>();

            log.Info($"ToEmailRecipients={string.Join(";", sendMail.ToEmails.Select(e => e.ToString()).ToArray())}, Subject={sendMail.Subject}");
            var instanceId = await starter.StartNewAsync("OrchestrateSendMail", JsonConvert.SerializeObject(sendMail));
            log.Info($"Started orchestration with ID = '{instanceId}'.");

            return req.CreateResponse(HttpStatusCode.OK, instanceId);
        }
    }
}
