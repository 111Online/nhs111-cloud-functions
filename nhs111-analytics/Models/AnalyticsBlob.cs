using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NHS111.Cloud.Functions.Models
{
    [DataContract(Name = "AnalyticsBlob", Namespace = "NHS111.Cloud.Functions.Models")]
    public class AnalyticsBlob
    {
        [DataMember]
        public string ToEmailRecipients { get; set; }

        [DataMember]
        public string GroupName { get; set; }

        [DataMember]
        public string Date { get; set; }

        [DataMember]
        public string Stp { get; set; }

        [DataMember]
        public string InstanceId { get; set; }

        [DataMember]
        public IEnumerable<AnalyticsDataRecord> DataRecords { get; set; }

        [DataMember]
        public string BlobName => !string.IsNullOrEmpty(GroupName) ? $"{InstanceId}-{GroupName}-{Date}.csv" : $"{InstanceId}-{Date}.csv";
    }
}
