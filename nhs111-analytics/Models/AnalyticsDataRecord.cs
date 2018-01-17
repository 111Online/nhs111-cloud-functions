using System;
using System.Runtime.Serialization;

namespace NHS111.Cloud.Functions.Models
{
    [DataContract(Name = "AnalyticsDataRecord", Namespace = "NHS111.Cloud.Functions.Models")]
    public class AnalyticsDataRecord
    {
        [DataMember]
        public Guid JourneyId { get; set; }
        [DataMember]
        public int Age { get; set; }
        [DataMember]
        public string Gender { get; set; }
        [DataMember]
        public string Stp { get; set; }
        [DataMember]
        public string Ccg { get; set; }
        [DataMember]
        public DateTime TriageStart { get; set; }
        [DataMember]
        public DateTime TriageEnd { get; set; }
        [DataMember]
        public string StartPathwayTitle { get; set; }
        [DataMember]
        public string Dx { get; set; }
        [DataMember]
        public string FinalDispositionGroup { get; set; }
        [DataMember]
        public string Itk { get; set; }
        [DataMember]
        public string ItkSelected { get; set; }
    }
}
