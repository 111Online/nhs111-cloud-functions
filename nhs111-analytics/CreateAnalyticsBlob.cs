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
        public static void Run(string jsonContent, TraceWriter log)
        {
            log.Info($"Activity was triggered!");
            var blob = JsonConvert.DeserializeObject<AnalyticsBlob>(jsonContent);

            if (!blob.DataRecords.Any()) return;

            var connectionString = ConfigurationManager.ConnectionStrings["AzureContainerConnection"].ConnectionString;
            var storageAccount = CloudStorageAccount.Parse(connectionString);

            var client = storageAccount.CreateCloudBlobClient();

            var container = client.GetContainerReference("analytics");
            container.CreateIfNotExists();

            var containerBlob = container.GetBlockBlobReference(blob.BlobName);
            containerBlob.Properties.ContentType = "application/text";
            containerBlob.Metadata.Add("emailrecipients", blob.ToEmailRecipients);

            var caseRecordsCsv = CsvSerializer.SerializeToCsv(blob.DataRecords);
            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(caseRecordsCsv)))
            {
                log.Info($"Uploading blob {blob.BlobName}");
                containerBlob.UploadFromStream(stream);
            }
        }
    }
}
