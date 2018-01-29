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

            var data = new AnalyticsData
            {
                Date = email.Date,
                Stp = email.Stp,
                Ccg = email.Ccg,
            };
            log.Info($"Calling function ExtractAnalyticsData Stp={data.Stp}, Ccg={data.Ccg}, Date={data.Date}");
            var jsonDataRecords = await orchestrationClient.CallActivityAsync<string>("ExtractAnalyticsData", JsonConvert.SerializeObject(data));
            
            var blob = new AnalyticsBlob
            {
                ToEmailRecipients = email.ToEmailRecipients,
                Date = email.Date,
                DataRecords = JsonConvert.DeserializeObject<IEnumerable<AnalyticsDataRecord>>(jsonDataRecords)
            };
            log.Info($"Calling function CreateAnalyticsBlob ToEmailRecipients={blob.ToEmailRecipients}, Date={blob.Date}, DataRecords.Count={blob.DataRecords.Count()}");
            await orchestrationClient.CallActivityAsync("CreateAnalyticsBlob", JsonConvert.SerializeObject(blob));

            return orchestrationClient.InstanceId;
        }
    }
}
