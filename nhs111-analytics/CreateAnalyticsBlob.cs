using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
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
            var data = JsonConvert.DeserializeObject<AnalyticsBlob>(jsonContent);
            var date = data.Date != null ? Convert.ToDateTime(data.Date).ToString("yyyy-MM-dd") : DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            var str = ConfigurationManager.ConnectionStrings["SqlDbConnection"].ConnectionString;
            using (var conn = new SqlConnection(str))
            {
                conn.Open();
                log.Info($"Opened SQL connection");
                using (var cmd = new SqlCommand("[dbo].[spGetCcgData]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (data.Stp != null) cmd.Parameters.Add(new SqlParameter("@CAMPAIGN", data.Stp));
                    if (data.Ccg != null) cmd.Parameters.Add(new SqlParameter("@CAMPAIGNSOURCE", data.Ccg));
                    log.Info($"Using date {date}");
                    if (data.Date != null) cmd.Parameters.Add(new SqlParameter("@DATE", date));

                    var reader = cmd.ExecuteReader();
                    log.Info($"Executed stored procedure");
                    var caseRecords = GetCaseRecords(reader, log);

                    log.Info($"Creating blob!");
                    await CreateBlob(caseRecords, data.ToEmailRecipients, date, log);
                }
            }

            return req.CreateResponse(HttpStatusCode.OK);
        }

        private static async Task CreateBlob(IEnumerable<CaseRecord> caseRecords, string recipients, string date, TraceWriter log)
        {
            if (caseRecords.Any())
            {
                var connectionString = ConfigurationManager.ConnectionStrings["AzureContainerConnection"].ConnectionString;
                var storageAccount = CloudStorageAccount.Parse(connectionString);

                var client = storageAccount.CreateCloudBlobClient();
                var container = client.GetContainerReference("analytics");

                await container.CreateIfNotExistsAsync();

                var guid = Guid.NewGuid().ToString("n");
                var name = $"{guid}-{date}.csv";
                var blob = container.GetBlockBlobReference(name);

                blob.Properties.ContentType = "application/text";
                blob.Metadata.Add("emailrecipients", recipients);

                var caseRecordsCsv = CsvSerializer.SerializeToCsv(caseRecords);
                using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(caseRecordsCsv)))
                {
                    log.Info($"Uploading blob {name}");
                    await blob.UploadFromStreamAsync(stream);
                }
            }
        }

        private static IEnumerable<CaseRecord> GetCaseRecords(SqlDataReader reader, TraceWriter log)
        {
            log.Info($"Looping through reader");
            while (reader.Read())
            {
                var caseRecord = new CaseRecord
                {
                    JourneyId = reader.GetGuid(0),
                    Age = Convert.ToInt32(reader.GetString(1)),
                    Gender = reader.SafeGetString(2),
                    Stp = reader.SafeGetString(3),
                    Ccg = reader.SafeGetString(4),
                    TriageStart = reader.GetDateTime(5),
                    TriageEnd = reader.GetDateTime(6),
                    StartPathwayTitle = reader.SafeGetString(7),
                    Dx = reader.SafeGetString(8),
                    FinalDispositionGroup = reader.SafeGetString(9),
                    Itk = reader.SafeGetString(10),
                    ItkSelected = reader.SafeGetString(11)
                };
                yield return caseRecord;
            }
        }

        public static string SafeGetString(this SqlDataReader reader, int colIndex)
        {
            return !reader.IsDBNull(colIndex) ? reader.GetString(colIndex) : string.Empty;
        }
    }
}
