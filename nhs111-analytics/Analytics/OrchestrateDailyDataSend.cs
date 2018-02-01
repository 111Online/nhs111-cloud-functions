using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Models.Analytics;

namespace NHS111.Cloud.Functions.Analytics
{
    public static class OrchestrateDailyDataSend
    {
        [FunctionName("OrchestrateDailyDataSend")]
        public static async Task<string>Run([OrchestrationTrigger] DurableOrchestrationContext orchestrationClient, TraceWriter log)
        {
            var jsonEmail = orchestrationClient.GetInput<string>();
            var email = JsonConvert.DeserializeObject<AnalyticsEmail>(jsonEmail);
            var date = email.Date != null
                ? Convert.ToDateTime(email.Date).ToString("yyyy-MM-dd")
                : orchestrationClient.CurrentUtcDateTime.AddDays(-1).ToString("yyyy-MM-dd");

            var data = new AnalyticsData
            {
                Date = date,
                Stp = email.Stp,
                Ccg = email.Ccg,
            };
            log.Info($"Calling function ExtractAnalyticsData Stp={data.Stp}, Ccg={data.Ccg}, Date={data.Date}");
            var jsonDataRecords = await orchestrationClient.CallActivityAsync<string>("ExtractAnalyticsData", JsonConvert.SerializeObject(data));
            
            var blob = new AnalyticsBlob
            {
                ToEmailRecipients = email.ToEmailRecipients,
                Date = date,
                Stp = email.Stp,
                InstanceId = orchestrationClient.InstanceId,
                DataRecords = JsonConvert.DeserializeObject<IEnumerable<AnalyticsDataRecord>>(jsonDataRecords)
            };
            log.Info($"Calling function CreateAnalyticsBlob ToEmailRecipients={blob.ToEmailRecipients}, Date={blob.Date}, DataRecords.Count={blob.DataRecords.Count()}");
            await orchestrationClient.CallActivityAsync("CreateAnalyticsBlob", JsonConvert.SerializeObject(blob));

            return orchestrationClient.InstanceId;
        }
    }
}
