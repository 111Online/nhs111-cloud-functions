using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NHS111.Cloud.Functions.Models.Analytics
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
        public IEnumerable<AnalyticsDataRecord> DataRecords { get; set; }

        [DataMember]
        public string BlobName
        {
            get
            {
                var guid = Guid.NewGuid().ToString("n");
                return !string.IsNullOrEmpty(Stp) ? $"{guid}-{Stp}-{Date}.csv" : $"{guid}-{Date}.csv";
            }
        }

    }
}
