
namespace DeepSigma.DataAccess.WebSearch.WebSearchClient.Model;

/// <summary>
/// Represents the response from a content extraction operation, containing the URL of the extracted content, the extracted content itself, and the timestamp of when the content was extracted.
/// </summary>
/// <param name="URL">The URL of the extracted content.</param>
/// <param name="ExtractedContent">The extracted content from the URL.</param>
/// <param name="ExtractedAt">The timestamp indicating when the content was extracted.</param>
public record ResponseExtractedContent(
    string URL,
    string ExtractedContent,
    DateTimeOffset ExtractedAt
    );
