namespace DeepSigma.DataAccess.WebSearch.WebSearchClient.Model;

/// <summary>
/// Represents the response from a URL retrieval operation, containing an array of URLs and the timestamp of when the URLs were retrieved.
/// </summary>
/// <param name="Urls">An array of URLs retrieved from the search query.</param>
/// <param name="RetrievedAt">The timestamp indicating when the URLs were retrieved.</param>
public record ResponseUrlRetrival(
    string[] Urls,
    DateTimeOffset RetrievedAt
    );
