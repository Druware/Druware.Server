using MailKit;
using MailKit.Security;
using System.ComponentModel;
using MimeKit;
using MimeKit.Text;
using Druware.Extensions;
using MailKit.Net.Smtp;

namespace Druware.Server
{
    public class MailHelper
    {
        private readonly MailSettings? _configuration = null;
        private readonly string _assemblyName;

        private static void OnSendComplete(object sender, AsyncCompletedEventArgs? e)
        {
            // Get the unique identifier for this asynchronous operation.
            var obj = (MailHelper)e!.UserState!;
            try
            {
                if (e?.UserState == null) return;

                // write to the error log if there is an issue
                if (e.Error != null)
                    Console.Error.WriteLine("[{0}] {1}", obj, e.Error.ToString());
            }
            catch (Exception exc)
            {
                Console.Error.WriteLine(exc.Message);
            }
        }

        public MailHelper(MailSettings configuration, string assemblyName)
        {
            _configuration = configuration;
            _assemblyName = assemblyName;
        }

        public async Task SendAsync(string to, string from, string replyTo,
            string subject, string body) =>
            Send(to, from, replyTo, subject, body);

        public async Task SendHtmlAsync(string to, string from, string replyTo,
            string subject, string htmlBody, string? plainTextBody = null) =>
            SendHtml(to, from, replyTo, subject, htmlBody, plainTextBody);

        public async Task SendWithAttachmentsAsync(string to, string from, string replyTo,
            string subject, string body, bool isHtml = false, List<string>? attachmentPaths = null) =>
            SendWithAttachments(to, from, replyTo, subject, body, isHtml, attachmentPaths);


        // TODO: Move this to Async and then wrap the async call from the sync side.
        public void Send(string to, string from, string replyTo, string subject, string body)
        {
            try
            {
                if (_configuration == null) return;

                var message = CreateMessage(to, from, replyTo, subject);
                message.Body = new TextPart("plain")
                {
                    Text = body
                };

                SendMessage(message);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"An Error Occurred while trying to send an email: {e.Message}");
            }
        }

        public void SendHtml(string to, string from, string replyTo, string subject,
            string htmlBody, string? plainTextBody = null)
        {
            try
            {
                if (_configuration == null) return;

                var message = CreateMessage(to, from, replyTo, subject);

                // Create multipart alternative for both HTML and plain text
                var multipart = new Multipart("alternative");

                // Add plain text version - either provided or strip HTML tags
                var plainText = plainTextBody ?? StripHtmlTags(htmlBody);
                multipart.Add(new TextPart(TextFormat.Plain) { Text = plainText });

                // Add HTML version
                multipart.Add(new TextPart(TextFormat.Html) { Text = htmlBody });

                message.Body = multipart;

                SendMessage(message);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"An Error Occurred while trying to send an HTML email: {e.Message}");
            }
        }

        public void SendWithAttachments(string to, string from, string replyTo, string subject,
            string body, bool isHtml = false, List<string>? attachmentPaths = null)
        {
            try
            {
                if (_configuration == null) return;

                var message = CreateMessage(to, from, replyTo, subject);

                // Create multipart for body and attachments
                var multipart = new Multipart("mixed");

                // Add body
                if (isHtml)
                {
                    var alternative = new Multipart("alternative");
                    alternative.Add(new TextPart(TextFormat.Plain) { Text = StripHtmlTags(body) });
                    alternative.Add(new TextPart(TextFormat.Html) { Text = body });
                    multipart.Add(alternative);
                }
                else
                {
                    multipart.Add(new TextPart(TextFormat.Plain) { Text = body });
                }

                // Add attachments if provided
                if (attachmentPaths != null)
                {
                    foreach (var path in attachmentPaths)
                    {
                        if (File.Exists(path))
                        {
                            var attachment = new MimePart()
                            {
                                Content = new MimeContent(File.OpenRead(path)),
                                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                                ContentTransferEncoding = ContentEncoding.Base64,
                                FileName = Path.GetFileName(path)
                            };
                            multipart.Add(attachment);
                        }
                    }
                }

                message.Body = multipart;

                SendMessage(message);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"An Error Occurred while trying to send an email with attachments: {e.Message}");
            }
        }

        private MimeMessage CreateMessage(string to, string from, string replyTo, string subject)
        {
            var message = new MimeMessage();
            message.ReplyTo.Add(new MailboxAddress(replyTo, replyTo));
            message.From.Add(new MailboxAddress(from, from));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;
            return message;
        }

        private void SendMessage(MimeMessage message)
        {
            if (_configuration == null) return;

            using var client = new SmtpClient(new ProtocolLogger(Console.OpenStandardOutput()));
            client.Connect(_configuration.HostName, (int)_configuration.Port!, SecureSocketOptions.SslOnConnect);
            client.Authenticate(_configuration.UserName, _configuration.Password!.Decrypt(_assemblyName, "4D584868CCA84221823D53D80DB30FCB"));

            client.Send(message);
            client.Disconnect(true);
        }

        private string StripHtmlTags(string html)
        {
            // Simple HTML to plain text conversion
            return System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
        }
    }
}


