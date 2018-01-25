using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Models.Email;
using Task = System.Threading.Tasks.Task;

namespace NHS111.Cloud.Functions.Analytics
{
    public static class SendAnalyticsEmail
    {
        [FunctionName("SendAnalyticsEmail")]
        public static async Task Run([BlobTrigger("analytics/{name}.csv", Connection = "AzureContainerConnection")]CloudBlockBlob analyticsBlob, [OrchestrationClient]DurableOrchestrationClient starter, string name, TraceWriter log)
        {
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {analyticsBlob.StreamWriteSizeInBytes} Bytes");
            log.Info($"Email for recipients : {analyticsBlob.Metadata["emailrecipients"]}");

            var date = name.Substring(name.IndexOf('-') + 1);
            var subject = $"Data extract for {date} has been created";
            log.Info($"ToEmailRecipients={analyticsBlob.Metadata["emailrecipients"]}, Subject={subject}");

            var sendMail = new SendMail
            {
                ToEmails = analyticsBlob.Metadata["emailrecipients"].Split(';'),
                Subject = subject,
                Body = $"<h1>Data generated at {DateTime.Now:dd/MM/yyyy hh:mm:ss}</h1>",
                Attachments = new[] { new KeyValuePair<string, Stream>($"{name}.csv", await analyticsBlob.OpenReadAsync()) }
            };
            var instanceId = await starter.StartNewAsync("NHS111OnlineMailSender", JsonConvert.SerializeObject(sendMail));
            log.Info($"Started orchestration with ID = '{instanceId}'.");
        }
    }
}
