using DeepSigma.DataAccess.WebSearch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using DeepSigma.DataAccess.WebSearch.WebSearchClient;
using DeepSigma.DataAccess.WebSearch.UrlRetriever;
using DeepSigma.DataAccess.WebSearch.Abstraction;
using DeepSigma.DataAccess.WebSearch.UrlRetriever.Models;
using DeepSigma.DataAccess.WebSearch.Abstraction.Model;

var services = new ServiceCollection();

Console.WriteLine("Enter search query:");
string? search = Console.ReadLine();

services.AddLogging(b => b.AddConsole());

services.AddSearxngClient(new SearxngOptions
{
    BaseUri = new Uri("http://localhost:8080"),
    Timeout = TimeSpan.FromSeconds(10),
    UserAgent = "MyApp/1.0"
});

await using var provider = services.BuildServiceProvider();

IHtmlRetriver searxng = provider.GetRequiredService<IHtmlRetriver>();
IContentExtractor contentExtractor = provider.GetRequiredService<IContentExtractor>();
IUrlRetriver<SearchRequestOptions> urlRetriver = provider.GetRequiredService<IUrlRetriver<SearchRequestOptions>>();
ILogger logger = provider.GetRequiredService<ILogger<Program>>();
WebSearchClient<SearchRequestOptions> webSearchClient = provider.GetRequiredService<WebSearchClient<SearchRequestOptions>>();

using CancellationTokenSource cts = new();
CancellationToken ct = cts.Token;

SearchRequestOptions searchRequestOptions = new()
{
    Engines = ["google"],
    Language = "en",
    TimeRange = "week"
};

List<ResponseExtractedContent>? extractedContents = await webSearchClient.SearchAndExtract(search ?? "unknown", searchRequestOptions, cancellationToken: ct);

if(extractedContents == null)
{
    logger.LogWarning("No content was extracted for the query: {Query}", search);
}

foreach (var content in extractedContents ?? [])
{
    Console.WriteLine("__________________________");
    Console.WriteLine($"Title   : {content.Title}");
    Console.WriteLine($"Byline  : {content.Byline}");
    Console.WriteLine($"Language: {content.Language}");
    Console.WriteLine($"Date    : {content.PublishedAt}");
    Console.WriteLine(content.MainText);
    Console.WriteLine("__________________________");
    Console.WriteLine();
}