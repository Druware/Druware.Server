using System;
using Microsoft.Extensions.Configuration;

namespace Druware.Server
{
    public enum MailType
    {
        Smtp = 0,
        Pop3 = 1,
        Imap = 2,
        Web = 3
    }

    // A simple helper class to parse and read the known API Settings from the
    // AppSettings.  It exists as a convenience, nothing more.
    public class MailSettings
    {
        public string? HostName { get; private set; }
        public string? UserName { get; private set; }
        public int? Port { get; private set; }
        public string? Password { get; private set; }
        public MailType Type { get; private set; } = MailType.Smtp;

        public MailSettings(IConfigurationSection configuration, MailType type = MailType.Smtp)
        {
            // the passed in ConfigustionSection *should* be the Mail section.
            string keyPath = string.Empty;
            switch (type)
            {
                case MailType.Pop3:
                    keyPath = "POP3";
                    break;
                case MailType.Imap:
                    keyPath = "IMAP";
                    break;
                case MailType.Web:
                    keyPath = "Web";
                    break;
                default: // the default is Smtp
                    keyPath = "SMTP";
                    break;
            }

            IConfigurationSection section = configuration.GetSection(keyPath);
            if (section == null)
                throw new Exception(string.Format("Unable to find a Configuration Section for {0}.", keyPath));

            if (section.GetChildren().Any(item => item.Key == "Server"))
                HostName = section.GetValue<string>("Server");
            if (section.GetChildren().Any(item => item.Key == "Port"))
                Port = section.GetValue<int>("Port");
            if (section.GetChildren().Any(item => item.Key == "User"))
                UserName = section.GetValue<string>("User");
            if (section.GetChildren().Any(item => item.Key == "Pwd"))
                Password = section.GetValue<string>("Pwd");
            Type = type;
        }
    }

    public class NotificationSettings
    {
        public string? From { get; private set; }
        public string? Name { get; private set; }
        public string? To { get; private set; }
        public bool OnStartup { get; private set; } = true;

        public NotificationSettings(IConfigurationSection section)
        {
            if (section.GetChildren().Any(item => item.Key == "From"))
                From = section.GetValue<string>("From");
            if (section.GetChildren().Any(item => item.Key == "Name"))
                Name = section.GetValue<string>("Name");
            if (section.GetChildren().Any(item => item.Key == "To"))
                To = section.GetValue<string>("To");
            if (section.GetChildren().Any(item => item.Key == "OnStartup"))
                OnStartup = section.GetValue<bool>("OnStartup");
        }

    }

    public class ApplicationSettings
    {
        private readonly IConfiguration Configuration;

        public readonly NotificationSettings? Notification = null;
        public readonly MailSettings? Smtp = null;
        public readonly string? ConfirmationUrl = null;

        public ApplicationSettings(IConfiguration configuration)
        {
            Configuration = configuration;

            IConfigurationSection api = Configuration.GetSection("API");

            if (api.GetChildren().Any(item => item.Key == "ConfirmationUrl"))
                ConfirmationUrl = api.GetValue<string>("ConfirmationUrl");

            if (api.GetChildren().Any(item => item.Key == "Notification"))
                Notification = new NotificationSettings(api.GetSection("Notification"));

            if (api.GetChildren().Any(item => item.Key == "Mail"))
            {
                IConfigurationSection mail = api.GetSection("Mail");
                if (mail.GetChildren().Any(item => item.Key == "SMTP"))
                    Smtp = new MailSettings(mail, MailType.Smtp);

            }

        }

    }
}
