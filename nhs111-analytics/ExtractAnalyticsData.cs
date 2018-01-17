using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using NHS111.Cloud.Functions.Models;

namespace NHS111.Cloud.Functions
{
    public static class ExtractAnalyticsData
    {
        [FunctionName("ExtractAnalyticsData")]
        public static string Run([ActivityTrigger]string jsonContent, TraceWriter log)
        {
            log.Info($"Activity was triggered!");

            //var jsonContent = await req.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<AnalyticsData>(jsonContent);
            var date = data.Date != null
                ? Convert.ToDateTime(data.Date).ToString("yyyy-MM-dd")
                : DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            var str = ConfigurationManager.ConnectionStrings["SqlDbConnection"].ConnectionString;
            IEnumerable<AnalyticsDataRecord> dataRecords;
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
                    dataRecords = GetCaseRecords(reader, log);
                }
            }

            return JsonConvert.SerializeObject(dataRecords);
        }

        private static IEnumerable<AnalyticsDataRecord> GetCaseRecords(SqlDataReader reader, TraceWriter log)
        {
            log.Info($"Looping through reader");
            while (reader.Read())
            {
                var dataRecord = new AnalyticsDataRecord
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
                yield return dataRecord;
            }
        }

        public static string SafeGetString(this SqlDataReader reader, int colIndex)
        {
            return !reader.IsDBNull(colIndex) ? reader.GetString(colIndex) : string.Empty;
        }
    }
}
