using System.Text;
using Microsoft.Extensions.Configuration;

namespace Druware.Server;

public enum MailType
{
    Smtp = 0,
    Pop3 = 1,
    Imap = 2,
    Web = 3
}

public enum DbContextType
{
    Microsoft,
    PostgreSql,
    Sqlite
};

// A simple helper class to parse and read the known API Settings from the
// AppSettings.  It exists as a convenience, nothing more.
public class MailSettings
{
    public MailSettings(IConfigurationSection configuration, string? userName = null, string? password = null, MailType type = MailType.Smtp)
    {
        // the passed in ConfigurationSection *should* be the Mail section.
        var keyPath = type switch
        {
            MailType.Pop3 => "POP3",
            MailType.Imap => "IMAP",
            MailType.Web => "Web",
            _ => "SMTP"
        };

        var section = configuration.GetSection(keyPath);
        if (section == null)
            throw new Exception(
                $"Unable to find a Configuration Section for {keyPath}.");

        HostName = section.GetValue<string>("Host");
        Port = section.GetValue<int>("Port");
        UserName = section.GetValue<string>("User");
        Password = section.GetValue<string>("Password");
        Type = type;
    }

    public string? HostName { get; private set; }
    public string? UserName { get; private set; }
    public int Port { get; private set; }
    public string? Password { get; private set; }
    public MailType Type { get; private set; }
}

public class NotificationSettings
{
    public NotificationSettings(IConfigurationSection section, string? from = null, string? name = null, string? to = null)
    {
        From = section.GetValue<string>("From");
        Name = section.GetValue<string>("Name");
        To = section.GetValue<string>("To");
        if (section.GetChildren().Any(item => item.Key == "OnStartup"))
            OnStartup = section.GetValue<bool>("OnStartup");
    }

    public string? From { get; private set; }
    public string? Name { get; private set; }
    public string? To { get; private set; }
    public bool OnStartup { get; private set; } = true;
}

public class AppSettings
{
    public readonly string? ConfirmationUrl;
    public readonly DbContextType DbType;
    public readonly string? ConnectionString;
    public readonly NotificationSettings? Notification;
    public readonly MailSettings? Smtp;

    public AppSettings(IConfiguration configuration)
    {
        var cfg
            = configuration;

        var api = cfg.GetSection("API");

        var db = api.GetSection("Database");
        if (db.GetChildren().Any(item => item.Key == "ConnectionString"))
        {
            var dbTypeString = db.GetValue<string>("ConnectionType");
            if (dbTypeString?.ToUpper() == "MSSQL") DbType = DbContextType.Microsoft;
            if (dbTypeString?.ToUpper() == "POSTGRESQL") DbType = DbContextType.PostgreSql;
            if (dbTypeString?.ToUpper() == "PGSQL") DbType = DbContextType.PostgreSql;
            if (dbTypeString?.ToUpper() == "SQLITE") DbType = DbContextType.Sqlite;
            ConnectionString = db.GetValue<string>("ConnectionString");
        }
        // check the command line, if the connectionString is found there, override 
        // the value found in the appsettings.json file, even if the altAppSettings is
        // provided
        var connectionString = configuration.GetValue<string>("connectionstring");
        ConnectionString =  string.IsNullOrEmpty(connectionString) ? ConnectionString : connectionString;
        
        if (string.IsNullOrEmpty(ConnectionString))
            throw new Exception(
                "Cannot Startup without a ConnectionString");

        if (api.GetChildren().Any(item => item.Key == "ConfirmationUrl"))
            ConfirmationUrl = api.GetValue<string>("ConfirmationUrl");

        if (api.GetChildren().Any(item => item.Key == "Notification"))
            Notification =
                new NotificationSettings(api.GetSection("Notification"));

        if (api.GetChildren().All(item => item.Key != "Mail")) return;
        {
            var mail = api.GetSection("Mail");
            if (mail.GetChildren().Any(item => item.Key == "SMTP"))
                Smtp = new MailSettings(mail);
        }
    }

    private string DecodeString(string base64EncodedData)
    {
        var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
        return Encoding.UTF8.GetString(base64EncodedBytes);
    }
}