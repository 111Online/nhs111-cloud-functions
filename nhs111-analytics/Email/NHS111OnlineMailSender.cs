using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Exchange.WebServices.Data;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Models.Email;

namespace NHS111.Cloud.Functions.Email
{
    public static class SendEmail
    {
        [FunctionName("NHS111OnlineMailSender")]
        public static async Task<HttpResponseMessage> Run([ActivityTrigger]string jsonContent, TraceWriter log)
        {
            log.Info("C# NHS111OnlineMailSender trigger function processed a request.");
            var sendMail = JsonConvert.DeserializeObject<SendMail>(jsonContent);
            
            if (sendMail.ToEmails == null || sendMail.Body == null || sendMail.Subject == null)
            {
                log.Info("Usage: Args[]: str:RecipientEmail, str:Subject, str:Body");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (IsValidEmails(sendMail.ToEmails) && IsValidEmails(sendMail.CcEmails))
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                var nhs111OnlineMailAccount = await keyVaultClient.GetSecretAsync("https://nhsmailemailacctkv.vault.azure.net/secrets/nhs111OnlineMailAccount").ConfigureAwait(false);
                var nhs111OnlineMailPassword = await keyVaultClient.GetSecretAsync("https://nhsmailemailacctkv.vault.azure.net/secrets/nhs111OnlineMailPassword").ConfigureAwait(false);

                var service = new ExchangeService(ExchangeVersion.Exchange2013)
                {
                    Credentials = new WebCredentials(nhs111OnlineMailAccount.Value, nhs111OnlineMailPassword.Value),
                    Url = new Uri("https://mail.nhs.net/EWS/Exchange.asmx")
                };

                var message = new EmailMessage(service)
                {
                    Subject = sendMail.Subject,
                    Body = new MessageBody(BodyType.HTML, sendMail.Body)
                };

                foreach (var toEmail in sendMail.ToEmails)
                    message.ToRecipients.Add(toEmail);

                foreach (var ccEmail in sendMail.CcEmails)
                    message.CcRecipients.Add(ccEmail);

                foreach (var attachment in sendMail.Attachments)
                    message.Attachments.AddFileAttachment(attachment.Key, Encoding.ASCII.GetBytes(attachment.Value));

                try
                {
                    message.SendAndSaveCopy();
                    return new HttpResponseMessage(HttpStatusCode.OK);
                }
                catch (Exception e)
                {
                    log.Info(e.Message);
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError);
                }
            }

            log.Info("One or more email addresses not valid");
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }

        private static bool IsValidEmails(IEnumerable<string> emails)
        {
            return !emails.Any() || emails.All(IsValidEmail);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
