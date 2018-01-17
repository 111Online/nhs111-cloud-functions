using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using NHS111.Cloud.Functions.Models;
using ServiceStack.Text;

namespace NHS111.Cloud.Functions
{
    public static class CreateAnalyticsBlob
    {
        [FunctionName("CreateAnalyticsBlob")]
        public static async Task<object> Run([HttpTrigger(WebHookType = "genericJson")]HttpRequestMessage req, TraceWriter log)
        {
            log.Info($"Webhook was triggered!");

            var jsonContent = await req.Content.ReadAsStringAsync();
            var blob = JsonConvert.DeserializeObject<AnalyticsBlob>(jsonContent);

            await CreateBlob(blob, log);
            return req.CreateResponse(HttpStatusCode.OK);
        }

        private static async Task CreateBlob(AnalyticsBlob blob, TraceWriter log)
        {
            if (blob.DataRecords.Any())
            {
                var connectionString = ConfigurationManager.ConnectionStrings["AzureContainerConnection"].ConnectionString;
                var storageAccount = CloudStorageAccount.Parse(connectionString);

                var client = storageAccount.CreateCloudBlobClient();
                var container = client.GetContainerReference("analytics");

                await container.CreateIfNotExistsAsync();

                
                var containerBlob = container.GetBlockBlobReference(blob.BlobName);

                containerBlob.Properties.ContentType = "application/text";
                containerBlob.Metadata.Add("emailrecipients", blob.ToEmailRecipients);

                var caseRecordsCsv = CsvSerializer.SerializeToCsv(blob.DataRecords);
                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(caseRecordsCsv)))
                {
                    log.Info($"Uploading blob {blob.BlobName}");
                    await containerBlob.UploadFromStreamAsync(stream);
                }
            }
        }

    }
}
