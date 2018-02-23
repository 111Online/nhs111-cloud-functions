using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Models.Analytics;

namespace NHS111.Cloud.Functions.Analytics
{
    public static class OrchestrateDailyDataSend
    {
        public static string Run(string jsonEmail, TraceWriter log)
        {
            var email = JsonConvert.DeserializeObject<AnalyticsEmail>(jsonEmail);
            var date = email.StartDate != null
                ? Convert.ToDateTime(email.StartDate).ToString("yyyy-MM-dd")
                : DateTime.Now.Date.AddDays(-1).ToString("yyyy-MM-dd");

            var data = new AnalyticsData
            {
                ToEmailRecipients = email.ToEmailRecipients,
                InstanceId = Guid.NewGuid().ToString(),
                StartDate = date,
                StpList = email.StpList,
                CcgList = email.CcgList,
                NumberOfDays = email.NumberOfDays,
                GroupName = email.RowKey
            };
            log.Info($"Calling function ExtractAnalyticsData StpList={data.StpList}, CcgList={data.CcgList}, StartDate={data.StartDate}");
            ExtractAnalyticsData.Run(JsonConvert.SerializeObject(data), log);
            
            return data.InstanceId;
        }
    }
}
