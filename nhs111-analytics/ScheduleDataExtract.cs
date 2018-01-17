using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Models;

namespace NHS111.Cloud.Functions
{
    public static class ScheduleDataExtract
    {
        [FunctionName("ScheduleDataExtract")]
        public static async Task Run([TimerTrigger("0 * * * * *")]TimerInfo timer, [OrchestrationClient]DurableOrchestrationClient starter, [Table("AnalyticsEmailTable", "AzureContainerConnection")]IQueryable<AnalyticsEmail> analyticsEmails, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            foreach (var analyticsEmail in analyticsEmails)
            {
                log.Info($"Stp={analyticsEmail.PartitionKey}, Ccg={analyticsEmail.RowKey}, ToEmailRecipients={analyticsEmail.ToEmailRecipients}");
                analyticsEmail.Date = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");
                var instanceId = await starter.StartNewAsync("DailyDataSend", JsonConvert.SerializeObject(analyticsEmail));
                log.Info($"Started orchestration with ID = '{instanceId}'.");
            }  
        }
    }
}
