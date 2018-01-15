using System;
using System.Configuration;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.WindowsAzure.Storage.Blob;

namespace NHS111.Cloud.Functions
{
    public static class SendAnalyticsEmail
    {
        [FunctionName("SendAnalyticsEmail")]
        public static void Run([BlobTrigger("analytics/{name}.csv", Connection = "AzureContainerConnection")]CloudBlockBlob analyticsBlob, string name, TraceWriter log)
        {
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {analyticsBlob.StreamWriteSizeInBytes} Bytes");
            log.Info($"Email for recipients : {analyticsBlob.Metadata["emailrecipients"]}");

            var username = ConfigurationManager.AppSettings["Office365Username"];
            var password = ConfigurationManager.AppSettings["Office365Password"];

            var credentials = new WebCredentials(username, password);
            SendEmail(credentials, ConfigurationManager.AppSettings["EmailFromAddress"], analyticsBlob.Metadata["emailrecipients"], $"{name} has been updated/created", analyticsBlob);
        }

        private static async void SendEmail(ExchangeCredentials credentials, string fromAddress, string recipients, string subject, CloudBlockBlob analyticsBlob)
        {
            var service = new ExchangeService
            {
                Credentials = credentials
            };

            service.AutodiscoverUrl(fromAddress, RedirectionUrlValidationCallback);

            var message = new EmailMessage(service)
            {
                From = fromAddress,
                Subject = subject,
                Body = new MessageBody(BodyType.HTML, $"<h1>This is a test, generated at ${DateTime.Now}</h1>")
            };
            message.Attachments.AddFileAttachment("all-data.csv", await analyticsBlob.OpenReadAsync());
            
            foreach (var recipient in recipients.Split(';'))
                 message.ToRecipients.Add(recipient);

            message.Send();
        }

        private static bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            var result = false;
            var redirectionUri = new Uri(redirectionUrl);

            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            if (redirectionUri.Scheme == "https")
            {
                result = true;
            }
            return result;
        }
    }
}
