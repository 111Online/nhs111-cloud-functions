using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Models;

namespace NHS111.Cloud.Functions
{
    public static class DailyDataSend
    {
        [FunctionName("DailyDataSend")]
        public static async Task<string>Run([OrchestrationTrigger] DurableOrchestrationContext orchestrationClient, TraceWriter log)
        {
            var jsonEmail = orchestrationClient.GetInput<string>();
            var email = JsonConvert.DeserializeObject<AnalyticsEmail>(jsonEmail);
            var date = email.Date != null
                ? Convert.ToDateTime(email.Date).ToString("yyyy-MM-dd")
                : orchestrationClient.CurrentUtcDateTime.AddDays(-1).ToString("yyyy-MM-dd");

            var data = new AnalyticsData
            {
                ToEmailRecipients = email.ToEmailRecipients,
                InstanceId = orchestrationClient.InstanceId,
                Date = date,
                Stp = email.Stp,
                Ccg = email.Ccg,
            };
            log.Info($"Calling function ExtractAnalyticsData Stp={data.Stp}, Ccg={data.Ccg}, Date={data.Date}");
            await orchestrationClient.CallActivityAsync<string>("ExtractAnalyticsData", JsonConvert.SerializeObject(data));
            
            return orchestrationClient.InstanceId;
        }
    }
}
