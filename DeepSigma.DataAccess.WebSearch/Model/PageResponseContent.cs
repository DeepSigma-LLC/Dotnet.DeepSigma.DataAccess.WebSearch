
namespace DeepSigma.DataAccess.WebSearch.WebSearchClient.Model;

/// <summary>
/// Represents the content of a web page as returned by a fetch operation, including its URL, HTML markup, and the
/// timestamp when it was retrieved.
/// </summary>
/// <param name="URL">The absolute URL of the web page from which the content was fetched. Cannot be null or empty.</param>
/// <param name="HTML">The raw HTML markup of the fetched web page. May be empty if the page has no content.</param>
/// <param name="FetchedAt">The date and time, in UTC, when the page content was retrieved.</param>
public record PageResponseContent(
    string URL,
    string HTML,
    DateTimeOffset FetchedAt
    );
