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

    /// <summary>Mailbox folder that receives the portal alerts.</summary>
    public string Folder { get; set; } = "INBOX";

    /// <summary>
    /// Re-read this rolling window on every run. The database-level Message-ID
    /// ledger removes duplicates, while the lookback prevents an alert being
    /// lost if the process stops between IMAP fetch and database commit.
    /// </summary>
    public int LookbackDays { get; set; } = 7;

    /// <summary>Safety cap for one collection cycle.</summary>
    public int MaxMessagesPerRun { get; set; } = 250;

    /// <summary>
    /// Optional optimization for a dedicated mailbox. Disabled by default:
    /// seen/unseen flags are user state and are not a reliable processing
    /// checkpoint; ProcessedEmailMessage is the authoritative checkpoint.
    /// </summary>
    public bool UnseenOnly { get; set; }

    /// <summary>
    /// Enable the EmailImport source at startup when Host, Username and
    /// Password are configured. Set false to keep manual UI control.
    /// </summary>
    public bool AutoEnable { get; set; } = true;
}
