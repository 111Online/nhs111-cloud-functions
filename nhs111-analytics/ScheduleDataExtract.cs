using System;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Analytics;
using NHS111.Cloud.Functions.Email;
using NHS111.Cloud.Functions.Models.Analytics;
using NHS111.Cloud.Functions.Models.Email;

namespace NHS111.Cloud.Functions
{
    public static class ScheduleDataExtract
    {
        [FunctionName("ScheduleDataExtract")]
        public static async Task Run([TimerTrigger("0 0 6 * * *")]TimerInfo timer, [Table("AnalyticsEmailTable", "Email", Connection = "AzureContainerConnection")]IQueryable<AnalyticsEmail> analyticsEmails, [Table("AnalyticsEmailTable", "Email", Connection = "AzureContainerConnection")]CloudTable outTable, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            log.Info($"Checking data import has completed successfully for {DateTime.Now.AddDays(-1).Date.ToShortDateString()}");
            //
            //some code
            //log error (no it hasn't)
            
            foreach (var analyticsEmail in analyticsEmails)
            {
                //set the initial end date
                var endDate = Convert.ToDateTime(analyticsEmail.StartDate).AddDays(analyticsEmail.NumberOfDays);
                while (DateTime.Now.Date > endDate.Date)
                {
                    log.Info($"StpList={analyticsEmail.StpList}, CcgList={analyticsEmail.CcgList}, ToEmailRecipients={analyticsEmail.ToEmailRecipients}, StartDate={analyticsEmail.StartDate}");
                    try
                    {
                        OrchestrateDailyDataSend.Run(JsonConvert.SerializeObject(analyticsEmail), log);
                        var updateAnalyticsEmail = await GetTableEntity(outTable, analyticsEmail.PartitionKey, analyticsEmail.RowKey);
                        updateAnalyticsEmail.StartDate = Convert.ToDateTime(analyticsEmail.StartDate).AddDays(analyticsEmail.NumberOfDays).ToString("yyyy-MM-dd");
                        await UpdateTableEntity(outTable, updateAnalyticsEmail);
                        //set end data again to see if we need to send again
                        endDate = endDate.AddDays(analyticsEmail.NumberOfDays);
                    }
                    catch (Exception e)
                    {
                        log.Error("An error occurred", e);
                        log.Error($"Error detail - {e.Message}", e);
                        if (e.InnerException != null) log.Error($" --> inner Error detail - {e.InnerException}", e.InnerException);

                        //send email to slack channel to raise error
                        var sendMail = new SendMail
                        {
                            ToEmails = new[] { ConfigurationManager.AppSettings["SlackChannelMailAddress"] },
                            EmailType = "DataExtract",
                            Body = $"Error detail - {e.Message}",// {e.InnerException!= null ? $"--> inner Error detail - {e.InnerException}" : $"{string.Empty}" },
                            Subject = "Data extract sending function failure"
                        };
                        await SendEmail.Run(JsonConvert.SerializeObject(sendMail), log);
                        throw;
                    }
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
