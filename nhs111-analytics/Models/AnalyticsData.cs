using System.Runtime.Serialization;

namespace NHS111.Cloud.Functions.Models
{
    [DataContract(Name = "AnalyticsData", Namespace = "NHS111.Cloud.Functions.Models")]
    public class AnalyticsData
    {
        [DataMember]
        public string StartDate { get; set; }

        [DataMember]
        public int NumberOfDays { get; set; }

        [DataMember]
        public string GroupName { get; set; }

        [DataMember]
        public string StpList { get; set; }

        [DataMember]
        public string CcgList{ get; set; }

        [DataMember]
        public string ToEmailRecipients { get; set; }

        [DataMember]
        public string InstanceId { get; set; }
    }
}
