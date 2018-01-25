using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Models.Analytics;

namespace NHS111.Cloud.Functions.Analytics
{
    public static class ScheduleDataExtract
    {
        [FunctionName("ScheduleDataExtract")]
        public static async Task Run([TimerTrigger("0 0 6 * * *")]TimerInfo timer, [OrchestrationClient]DurableOrchestrationClient starter, [Table("AnalyticsEmailTable", "AzureContainerConnection")]IQueryable<AnalyticsEmail> analyticsEmails, [Table("AnalyticsEmailTable", "AzureContainerConnection")]CloudTable outTable, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            foreach (var analyticsEmail in analyticsEmails)
            {
                log.Info($"Stp={analyticsEmail.Stp}, Ccg={analyticsEmail.Ccg}, ToEmailRecipients={analyticsEmail.ToEmailRecipients}, Date={analyticsEmail.Date}");
                var instanceId = await starter.StartNewAsync("DailyDataSend", JsonConvert.SerializeObject(analyticsEmail));
                log.Info($"Started orchestration with ID = '{instanceId}'.");

                var updateAnalyticsEmail = GetTableEntity(outTable, analyticsEmail.PartitionKey, analyticsEmail.RowKey);
                updateAnalyticsEmail.Date = Convert.ToDateTime(analyticsEmail.Date).AddDays(1).ToString("yyyy-MM-dd");
                UpdateTableEntity(outTable, updateAnalyticsEmail);
            }  
        }

        public static AnalyticsEmail GetTableEntity(CloudTable table, string partitionKey, string rowKey)
        {
            var operation = TableOperation.Retrieve<AnalyticsEmail>(partitionKey, rowKey);
            var result = table.Execute(operation);
            return result.Result as AnalyticsEmail;
        }

        public static void UpdateTableEntity(CloudTable table, AnalyticsEmail analyticsEmail)
        {
            var operation = TableOperation.Replace(analyticsEmail);
            table.Execute(operation);
        }
    }
}
