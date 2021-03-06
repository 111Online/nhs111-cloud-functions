﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace NHS111.Cloud.Functions.Models.Email
{
    public class SendMail
    {
        public SendMail()
        {
            EmailType = Email.EmailType.DataExtract.Name;
        }

        [DataMember]
        public string[] ToEmails { get; set; }

        [DataMember]
        public string[] CcEmails { get; set; }

        [DataMember]
        public string Body { get; set; }

        [DataMember]
        public string Subject { get; set; }

        [DataMember]
        public string EmailType { get; set; }

        [DataMember]
        public KeyValuePair<string, string>[] Attachments { get; set; }
    }
}
