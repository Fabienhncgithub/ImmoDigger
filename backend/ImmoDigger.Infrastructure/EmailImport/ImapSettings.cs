namespace ImmoDigger.Infrastructure.EmailImport;

/// <summary>
/// Credentials/connection info for the mailbox <see cref="ImapEmailInbox"/>
/// reads alert emails from. Never committed to source control - set via
/// user-secrets (Imap:Host, Imap:Username, Imap:Password, ...) or the
/// standard ASP.NET Core environment-variable equivalent (Imap__Host,
/// ...), same pattern as ConnectionStrings:Postgres. If Host is left
/// blank, <c>DependencyInjection.AddInfrastructure</c> registers
/// <see cref="NullEmailInbox"/> instead - the app runs fine with no
/// mailbox configured, it just imports nothing by that route.
/// </summary>
public class ImapSettings
{
    public const string SectionName = "Imap";

    public string Host { get; set; } = string.Empty;

    public int Port { get; set; } = 993;

    public bool UseSsl { get; set; } = true;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
