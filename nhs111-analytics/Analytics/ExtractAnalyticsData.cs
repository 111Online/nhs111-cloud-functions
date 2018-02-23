using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Models.Analytics;

namespace NHS111.Cloud.Functions.Analytics
{
    public static class ExtractAnalyticsData
    {
        public static void Run(string jsonContent, TraceWriter log)
        {
            log.Info($"Activity was triggered!");
            
            var data = JsonConvert.DeserializeObject<AnalyticsData>(jsonContent);
            log.Info($"StpList={data.StpList}, CcgList={data.CcgList}, Date={data.StartDate}");

            var str = ConfigurationManager.ConnectionStrings["SqlDbConnection"].ConnectionString;
            var dataRecords = new List<AnalyticsDataRecord>();
            using (var conn = new SqlConnection(str))
            {
                conn.Open();
                log.Info($"Opened SQL connection");
                using (var cmd = new SqlCommand("[dbo].[spGetCcgData]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    if (!string.IsNullOrEmpty(data.StpList)) cmd.Parameters.Add(new SqlParameter("@CAMPAIGNS", data.StpList));
                    if (!string.IsNullOrEmpty(data.CcgList)) cmd.Parameters.Add(new SqlParameter("@CAMPAIGNSOURCES", data.CcgList));
                    log.Info($"Using start date {data.StartDate}");
                    if (data.StartDate != null) cmd.Parameters.Add(new SqlParameter("@STARTDATE", data.StartDate));
                    log.Info($"Extracting {data.NumberOfDays} days of data");
                    cmd.Parameters.Add(new SqlParameter("@DAYS", data.NumberOfDays));

                    var reader = cmd.ExecuteReader();
                    log.Info($"Executed stored procedure");

                    log.Info($"Looping through reader");
                    while (reader.Read())
                    {
                        var dataRecord = new AnalyticsDataRecord
                        {
                            JourneyId = reader.GetGuid(0),
                            Age = reader.GetInt32(1),
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
                        dataRecords.Add(dataRecord);
                    }
                }
            }

            var blob = new AnalyticsBlob
            {
                ToEmailRecipients = data.ToEmailRecipients,
                GroupName = data.GroupName,
                Date = data.StartDate,
                Stp = data.StpList,
                InstanceId = data.InstanceId,
                DataRecords = dataRecords
            };
            CreateAnalyticsBlob.Run(JsonConvert.SerializeObject(blob), log);
        }

        public static string SafeGetString(this SqlDataReader reader, int colIndex)
        {
            return !reader.IsDBNull(colIndex) ? reader.GetString(colIndex) : string.Empty;
        }
    }
}
