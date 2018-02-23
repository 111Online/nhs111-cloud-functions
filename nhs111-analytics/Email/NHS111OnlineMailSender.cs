using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Exchange.WebServices.Data;
using Newtonsoft.Json;
using NHS111.Cloud.Functions.Models.Email;
using Task = System.Threading.Tasks.Task;

namespace NHS111.Cloud.Functions.Email
{
    public static class SendEmail
    {
        private const string NoReply = "<br><br><p>Please don't respond to this email, if you have any questions or feedback email <a href=\"mailto: nhs111online@nhs.net\" target=\"_blank\">nhs111online@nhs.net</a></p>";

        public static async Task<HttpResponseMessage> Run(string jsonContent, TraceWriter log)
        {
            log.Info("C# NHS111OnlineMailSender trigger function processed a request.");
            var sendMail = JsonConvert.DeserializeObject<SendMail>(jsonContent);

            if (!EmailType.IsSupported(sendMail.EmailType))
            {
                log.Info($"Email type {sendMail.EmailType} is not supported!");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (sendMail.ToEmails == null || sendMail.Body == null || sendMail.Subject == null)
            {
                log.Info("Usage: Args[]: str:RecipientEmail, str:Subject, str:Body");
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }

            if (IsValidEmails(sendMail.ToEmails) && (sendMail.CcEmails == null || IsValidEmails(sendMail.CcEmails)))
            {
                var azureServiceTokenProvider = new AzureServiceTokenProvider();
                var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));

                var emailType = EmailType.GetType(sendMail.EmailType);
                var mailAccount = await keyVaultClient.GetSecretAsync($"https://analytics111kv.vault.azure.net/secrets/{emailType.AccountKey}").ConfigureAwait(false);
                var mailPassword = await keyVaultClient.GetSecretAsync($"https://analytics111kv.vault.azure.net/secrets/{emailType.PasswordKey}").ConfigureAwait(false);

                log.Info($"Exchange user {mailAccount.Value}");
                var service = new ExchangeService(ExchangeVersion.Exchange2013)
                {
                    Credentials = new WebCredentials(mailAccount.Value, mailPassword.Value),
                    Url = new Uri("https://mail.nhs.net/EWS/Exchange.asmx")
                };

                var message = new EmailMessage(service)
                {
                    Subject = sendMail.Subject,
                    Body = new MessageBody(BodyType.HTML, $"{sendMail.Body}{(sendMail.EmailType != EmailType.DataExtract.Name ? NoReply : string.Empty)}")
                };

                foreach (var toEmail in sendMail.ToEmails)
                    message.ToRecipients.Add(toEmail);

                if (sendMail.CcEmails != null)
                {
                    foreach (var ccEmail in sendMail.CcEmails)
                        message.CcRecipients.Add(ccEmail);
                }

                if (sendMail.Attachments != null)
                {
                    foreach (var attachment in sendMail.Attachments)
                        message.Attachments.AddFileAttachment(attachment.Key, Convert.FromBase64String(attachment.Value));
                }

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
