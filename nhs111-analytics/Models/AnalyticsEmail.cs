using System.Runtime.Serialization;
using Microsoft.WindowsAzure.Storage.Table;

namespace NHS111.Cloud.Functions.Models
{
    public class AnalyticsEmail : TableEntity
    {
        [DataMember]
        public string ToEmailRecipients { get; set; }

        [DataMember]
        public string Date { get; set; }

        [DataMember]
        public string Stp => PartitionKey;

        [DataMember]
        public string Ccg => RowKey;
    }
}
