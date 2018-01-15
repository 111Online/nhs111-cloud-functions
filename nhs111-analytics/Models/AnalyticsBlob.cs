using System.Runtime.Serialization;

namespace NHS111.Cloud.Functions.Models
{
    [DataContract(Name = "AnalyticsBlob", Namespace = "NHS111.Cloud.Functions.Models")]
    public class AnalyticsBlob
    {
        [DataMember]
        public string ToEmailRecipients { get; set; }

        [DataMember]
        public string Date { get; set; }

        [DataMember]
        public string Stp { get; set; }

        [DataMember]
        public string Ccg { get; set; }
    }
}
