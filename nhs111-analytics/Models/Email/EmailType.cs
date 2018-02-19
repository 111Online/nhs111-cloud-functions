using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NHS111.Cloud.Functions.Models.Email
{
    public class EmailType
    {
        public static EmailType Itk = new EmailType { Name = "Itk", AccountKey = "nhs111OnlineMailAccount", PasswordKey = "nhs111OnlineMailPassword" };
        public static EmailType DataExtract = new EmailType { Name = "DataExtract", AccountKey = "nhs111OnlineItkMailAccount", PasswordKey = "nhs111OnlineItkMailPassword" };

        public string Name { get; private set; }

        public string AccountKey { get; private set; }

        public string PasswordKey { get; private set; }

        public static bool IsSupported(string emailType)
        {
            return !string.IsNullOrEmpty(emailType) && SupportedTypes.Any(t => t.Name.ToLower() == emailType.ToLower());
        }

        public static EmailType GetType(string emailType)
        {
            return SupportedTypes.FirstOrDefault(t => t.Name.ToLower().Equals(emailType.ToLower()));
        }

        private EmailType() { }
        private static readonly EmailType[] SupportedTypes = { Itk, DataExtract };
    }
}
