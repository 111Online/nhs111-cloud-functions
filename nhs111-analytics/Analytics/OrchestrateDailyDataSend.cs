using System.Collections.Generic;
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

            var data = new AnalyticsData
            {
                Date = email.Date,
                Stp = email.Stp,
                Ccg = email.Ccg,
            };

            log.Info($"Calling function ExtractAnalyticsData");
            var jsonDataRecords = await orchestrationClient.CallActivityAsync<string>("ExtractAnalyticsData", JsonConvert.SerializeObject(data));

            var blob = new AnalyticsBlob
            {
                ToEmailRecipients = email.ToEmailRecipients,
                Date = email.Date,
                Stp = email.Stp,
                DataRecords = JsonConvert.DeserializeObject<IEnumerable<AnalyticsDataRecord>>(jsonDataRecords)
            };
            log.Info($"Calling function CreateAnalyticsBlob");
            await orchestrationClient.CallActivityAsync("CreateAnalyticsBlob", JsonConvert.SerializeObject(blob));

            return orchestrationClient.InstanceId;
        }
    }
}
