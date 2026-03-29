namespace DeepSigma.DataAccess.WebSearch.Models;

/// <summary>
/// Represents a single normalized search result returned by a SearXNG query.
/// </summary>
/// <param name="Title">The display title of the result page.</param>
/// <param name="Url">The canonical URL of the result page.</param>
/// <param name="Snippet">
/// A short excerpt or description from the result page content, if available.
/// Mapped from the SearXNG <c>content</c> field.
/// </param>
/// <param name="Engine">
/// The name of the search engine that produced this result, if reported by SearXNG.
/// </param>
/// <param name="ParsedUrls">
/// Additional URLs extracted from the result content, if available.
/// </param>
/// <param name="Engines">
/// All search engines that contributed this result when SearXNG de-duplicates across providers.
/// </param>
/// <param name="Score">
/// The aggregated relevance score assigned by SearXNG. Higher values indicate higher relevance.
/// </param>
/// <param name="Category">
/// The SearXNG category this result belongs to, e.g. <c>general</c> or <c>news</c>.
/// </param>
/// <param name="PublishedDate">
/// The publication or last-modified date of the result page, if reported by the engine.
/// </param>
/// <param name="PrettyUrl">
/// A human-readable, shortened form of the URL suitable for display, if provided by SearXNG.
/// </param>
public sealed record SearchResult(
    string Title,
    string Url,
    string? Snippet,
    string? Engine,
    IReadOnlyList<string>? ParsedUrls = null,
    IReadOnlyList<string>? Engines = null,
    double? Score = null,
    string? Category = null,
    DateTimeOffset? PublishedDate = null,
    string? PrettyUrl = null);
