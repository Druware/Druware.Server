using System;
using MailKit;
using MailKit.Security;
using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using MimeKit;
using Druware.Extensions;
using MailKit.Net.Smtp;

namespace Druware.Server
{
    public class MailHelper
    {
        private readonly MailSettings? Configuration = null;
        private readonly string AssemblyName = string.Empty;

        private static void OnSendComplete(object sender, AsyncCompletedEventArgs e)
        {
            // Get the unique identifier for this asynchronous operation.
            try
            {
                if (e == null) return;
                if (e.UserState == null) return;
                MailHelper obj = (MailHelper)e!.UserState;

                // write to the error log if there is an issue
                if (e.Error != null)
                    Console.Error.WriteLine("[{0}] {1}", obj, e.Error.ToString());
            }
            finally
            {

            }
        }

        public MailHelper(MailSettings configuration, string assemblyName)
        {
            Configuration = configuration;
            AssemblyName = assemblyName;
        }

        public void Send(string to, string from, string replyTo, string subject, string body)
        {
            try
            {
                if (Configuration == null) return;

                var message = new MimeMessage();
                message.ReplyTo.Add(new MailboxAddress(replyTo, replyTo));
                message.From.Add(new MailboxAddress(from, from));
                message.To.Add(new MailboxAddress(to, to));
                message.Subject = subject;
                message.Body = new TextPart("plain")
                {
                    Text = body
                };

                using (var client = new SmtpClient(new ProtocolLogger(Console.OpenStandardOutput())))
                {
                    client.Connect(Configuration.HostName, Configuration.Port, SecureSocketOptions.SslOnConnect);

                    // Note: only needed if the SMTP server requires authentication
                    client.Authenticate(Configuration.UserName, Configuration.Password.Decrypt(AssemblyName));

                    client.Send(message);
                    client.Disconnect(true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(string.Format(
                    "An Error Occurred while trying to send an email: {0}",
                    e.Message));
            }
        }
    }
}


