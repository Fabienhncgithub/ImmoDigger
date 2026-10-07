namespace ImmoDigger.Application.DTOs;

/// <summary>
/// Non-secret readiness information for the saved-search email pipeline.
/// Credentials and mailbox contents are never exposed by this DTO.
/// </summary>
public sealed record EmailImportStatusDto(
    bool IsConfigured,
    bool AutoEnable,
    string Folder,
    int LookbackDays,
    IReadOnlyCollection<string> SupportedSources,
    IReadOnlyCollection<string> ConfigurationIssues);

public sealed record EmailImportConnectionTestDto(bool Success, int MessagesFound, string Message);
