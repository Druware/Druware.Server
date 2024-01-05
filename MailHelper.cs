using MailKit;
using MailKit.Security;
using System.ComponentModel;
using MimeKit;
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

        public void Send(string to, string from, string replyTo, string subject, string body)
        {
            try
            {
                if (_configuration == null) return;
                
                //var s = $"PWD: {Configuration.Password!.Decrypt(AssemblyName)}";

                //Console.WriteLine(
                //    $"PWD: {s}");
                
                var message = new MimeMessage();
                message.ReplyTo.Add(new MailboxAddress(replyTo, replyTo));
                message.From.Add(new MailboxAddress(from, from));
                message.To.Add(new MailboxAddress(to, to));
                message.Subject = subject;
                message.Body = new TextPart("plain")
                {
                    Text = body
                };

                using var client = new SmtpClient(new ProtocolLogger(Console.OpenStandardOutput()));
                client.Connect(_configuration.HostName, (int)_configuration.Port!, SecureSocketOptions.SslOnConnect);
                client.Authenticate(_configuration.UserName, _configuration.Password!.Decrypt(_assemblyName));

                client.Send(message);
                client.Disconnect(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"An Error Occurred while trying to send an email: {e.Message}");
            }
        }
    }
}


