using Mailjet.Client;
using Mailjet.Client.Resources;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace DCBStore.Helpers
{
    // SỬA LỖI LOGIC: Triển khai giao diện IEmailSender
    public class EmailSender : IEmailSender
    {
        private readonly MailjetClient _client;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public EmailSender(IConfiguration config)
        {
            var apiKey    = config["Mailjet:ApiKey"];
            var secretKey = config["Mailjet:SecretKey"];
            _client       = new MailjetClient(apiKey, secretKey);
            _fromEmail    = config["Mailjet:FromEmail"];
            _fromName     = config["Mailjet:FromName"];
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var request = new MailjetRequest
            {
                Resource = Send.Resource,
            }
            .Property(Send.FromEmail,   _fromEmail)
            .Property(Send.FromName,    _fromName)
            .Property(Send.Subject,     subject)
            .Property(Send.HtmlPart,    htmlContent)
            .Property(Send.Recipients, new JArray
            {
                new JObject { ["Email"] = toEmail }
            });

            await _client.PostAsync(request);
        }
    }
}