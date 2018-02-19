using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Analytics;
using NHS111.Cloud.Functions.Models.Analytics;

namespace NHS111.Cloud.Functions
{
    public static class ScheduleDataExtract
    {
        [FunctionName("ScheduleDataExtract")]
        public static async Task Run([TimerTrigger("0 0 6 * * *")]TimerInfo timer, [Table("AnalyticsEmailTable", "Email", Connection = "AzureContainerConnection")]IQueryable<AnalyticsEmail> analyticsEmails, [Table("AnalyticsEmailTable", "Email", Connection = "AzureContainerConnection")]CloudTable outTable, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");
            foreach (var analyticsEmail in analyticsEmails)
            {
                var endDate = Convert.ToDateTime(analyticsEmail.StartDate).AddDays(analyticsEmail.NumberOfDays);
                if (endDate.Date > DateTime.Now.Date) continue;
                log.Info($"StpList={analyticsEmail.StpList}, CcgList={analyticsEmail.CcgList}, ToEmailRecipients={analyticsEmail.ToEmailRecipients}, StartDate={analyticsEmail.StartDate}");
                try
                {
                    OrchestrateDailyDataSend.Run(JsonConvert.SerializeObject(analyticsEmail), log);
                    var updateAnalyticsEmail = await GetTableEntity(outTable, analyticsEmail.PartitionKey, analyticsEmail.RowKey);
                    updateAnalyticsEmail.StartDate = Convert.ToDateTime(analyticsEmail.StartDate).AddDays(analyticsEmail.NumberOfDays).ToString("yyyy-MM-dd");
                    await UpdateTableEntity(outTable, updateAnalyticsEmail);
                }
                catch (Exception e)
                {
                    log.Error("An error occurred", e);
                    throw;
                }
            }  
        }

        public static async Task<AnalyticsEmail> GetTableEntity(CloudTable table, string partitionKey, string rowKey)
        {
            var operation = TableOperation.Retrieve<AnalyticsEmail>(partitionKey, rowKey);
            var result = await table.ExecuteAsync(operation);
            return result.Result as AnalyticsEmail;
        }

        public static async Task UpdateTableEntity(CloudTable table, AnalyticsEmail analyticsEmail)
        {
            var operation = TableOperation.Replace(analyticsEmail);
            await table.ExecuteAsync(operation);
        }
    }
}
