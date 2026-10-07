namespace ImmoDigger.Application.DTOs;

/// <summary>A document explicitly published by the listing source for a property.</summary>
public sealed record ListingDocumentDto(string Type, string Name, string Url);
