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
            var executionDate = DateTime.Now;
            log.Info($"C# Timer trigger function executed at: {executionDate.ToString("yyyy-MM-dd")}");

            log.Info($"Checking overnight data import has completed successfully for {executionDate.AddDays(-1).ToString("yyyy-MM-dd")}");
            var cnt = ExtractAnalyticsData.Run(executionDate.AddDays(-1), log);
            if (cnt == 0)
            {
                //send email to slack channel to raise error
                var importFailSubject = "Data extract import failure";
                var importFailBody = $"The scheduled data extract returned {cnt} rows for the extract date {executionDate.ToString("yyyy-MM-dd")}";
                await SendMail(importFailSubject, importFailBody, log);
                throw new Exception($"No records imported for {executionDate} exception");
            }
            
            foreach (var analyticsEmail in analyticsEmails)
            {
                //set the initial end date
                var endDate = Convert.ToDateTime(analyticsEmail.StartDate).AddDays(analyticsEmail.NumberOfDays);
                var numberOfDays = analyticsEmail.NumberOfDays;
                while (executionDate.Date >= endDate.Date)
                {
                    log.Info($"StpList={analyticsEmail.StpList}, CcgList={analyticsEmail.CcgList}, ToEmailRecipients={analyticsEmail.ToEmailRecipients}, StartDate={analyticsEmail.StartDate}");
                    try
                    {
                        OrchestrateDailyDataSend.Run(JsonConvert.SerializeObject(analyticsEmail), log);
                        var updateAnalyticsEmail = await GetTableEntity(outTable, analyticsEmail.PartitionKey, analyticsEmail.RowKey);
                        updateAnalyticsEmail.StartDate = Convert.ToDateTime(analyticsEmail.StartDate).AddDays(numberOfDays).ToString("yyyy-MM-dd");
                        await UpdateTableEntity(outTable, updateAnalyticsEmail);
                        //set end data again to see if we need to send again
                        endDate = endDate.AddDays(numberOfDays);
                        analyticsEmail.StartDate = updateAnalyticsEmail.StartDate;
                    }
                    catch (Exception e)
                    {
                        log.Error("An error occurred", e);
                        log.Error($"Error detail - {e.Message}", e);
                        if (e.InnerException != null) log.Error($" --> inner Error detail - {e.InnerException}", e.InnerException);

                        //send email to slack channel to raise error
                        var sendFailSubject = "Data extract sending function failure";
                        var sendFailBody = $"Error detail - {e.Message} {(e.InnerException != null ? $" --> inner Error detail - {e.InnerException}" : $"{string.Empty}")}";
                        await SendMail(sendFailSubject, sendFailBody, log);
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

        private static async Task SendMail(string subject, string body, TraceWriter log)
        {
            var sendMail = new SendMail
            {
                ToEmails = ConfigurationManager.AppSettings["SendMailFailureAddresses"].Split(';'),
                EmailType = "DataExtract",
                Subject = subject,
                Body = body
            };
            await SendEmail.Run(JsonConvert.SerializeObject(sendMail), log);
        }
    }
}
