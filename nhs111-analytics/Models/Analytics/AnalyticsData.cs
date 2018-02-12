using System.Runtime.Serialization;

namespace NHS111.Cloud.Functions.Models.Analytics
{
    [DataContract(Name = "AnalyticsData", Namespace = "NHS111.Cloud.Functions.Models")]
    public class AnalyticsData
    {
        [DataMember]
        public string Date { get; set; }

        [DataMember]
        public string Stp { get; set; }

        [DataMember]
        public string Ccg { get; set; }

        [DataMember]
        public string ToEmailRecipients { get; set; }

        [DataMember]
        public string InstanceId { get; set; }
    }
}
