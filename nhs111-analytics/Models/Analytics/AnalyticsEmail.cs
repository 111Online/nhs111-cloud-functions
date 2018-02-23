using System.Runtime.Serialization;
using Microsoft.WindowsAzure.Storage.Table;

namespace NHS111.Cloud.Functions.Models.Analytics
{
    public class AnalyticsEmail : TableEntity
    {
        [DataMember]
        public string ToEmailRecipients { get; set; }

        [DataMember]
        public string StartDate { get; set; }

        [DataMember]
        public int NumberOfDays { get; set; }

        [DataMember]
        public string StpList { get; set; }

        [DataMember]
        public string CcgList { get; set; }
    }
}
