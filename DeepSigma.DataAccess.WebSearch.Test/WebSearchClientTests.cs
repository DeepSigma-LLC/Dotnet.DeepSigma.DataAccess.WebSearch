using System.Net;
using DeepSigma.DataAccess.WebSearch.Abstraction;
using DeepSigma.DataAccess.WebSearch.Abstraction.Model;
using DeepSigma.DataAccess.WebSearch.WebSearchClient;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DeepSigma.DataAccess.WebSearch.Test;

public class WebSearchClientTests
{
    sealed class TestOptions;

    // ── Fakes ────────────────────────────────────────────────────────────────

    sealed class FakeUrlRetriver(List<ResponseUrlRetrival> results) : IUrlRetriver<TestOptions>
    {
        public Task<List<ResponseUrlRetrival>> SearchAsync(
            string query, TestOptions? searchOption = null, CancellationToken? cancellationToken = null)
            => Task.FromResult(results);
    }

    sealed class ThrowingUrlRetriver : IUrlRetriver<TestOptions>
    {
        public Task<List<ResponseUrlRetrival>> SearchAsync(
            string query, TestOptions? searchOption = null, CancellationToken? cancellationToken = null)
            => throw new HttpRequestException("Backend unreachable");
    }

    sealed class FakeHtmlRetriver(Func<string, ResponseHtmlContent> factory) : IHtmlRetriver
    {
        public Task<ResponseHtmlContent> FetchContentAsync(
            string URL, CancellationToken? cancellationToken = null)
            => Task.FromResult(factory(URL));

        public Task<ResponseHtmlContent> FetchContentAsync(
            ResponseUrlRetrival responseUrl, CancellationToken? cancellationToken = null)
            => FetchContentAsync(responseUrl.Url, cancellationToken);
    }

    sealed class FakeContentExtractor(
        Func<ResponseHtmlContent, ResponseExtractedContent> factory) : IContentExtractor
    {
        public Task<ResponseExtractedContent> ExtractedContentAsync(
            ResponseHtmlContent htmlContent, CancellationToken? cancellationToken = null)
            => Task.FromResult(factory(htmlContent));

        public Task<ResponseExtractedContent> ExtractedContentAsync(
            string html, string? url = null, CancellationToken? cancellationToken = null)
            => throw new NotSupportedException();
    }

    // Throws on the Nth call; all others succeed. Use maxConcurrency=1 for determinism.
    sealed class ThrowingOnNthCallContentExtractor(int failOnCall) : IContentExtractor
    {
        int _calls;

        public Task<ResponseExtractedContent> ExtractedContentAsync(
            ResponseHtmlContent htmlContent, CancellationToken? cancellationToken = null)
        {
            if (Interlocked.Increment(ref _calls) == failOnCall)
                throw new InvalidOperationException("Extraction failed");
            return Task.FromResult(GoodContent());
        }

        public Task<ResponseExtractedContent> ExtractedContentAsync(
            string html, string? url = null, CancellationToken? cancellationToken = null)
            => throw new NotSupportedException();
    }

    // ── Model helpers ─────────────────────────────────────────────────────────

    static ResponseUrlRetrival UrlResult(string url) => new(
        Url: url, Title: "", Snippet: "", SearchEngine: "",
        RetrievedAt: DateTimeOffset.UtcNow,
        ParsedUrls: null, Engines: null, EngineRelevanceScore: null,
        Category: null, PrettyUrl: null, Template: null, Thumbnail: null,
        ImageUrl: null, Author: null, IframeSrc: null, PublishedDate: null);

    static ResponseHtmlContent HtmlContent(string url) => new(
        URL: url, HTML: "<html/>",
        FetchedAt: DateTimeOffset.UtcNow,
        StatusCode: HttpStatusCode.OK,
        Title: "", Byline: "", Excerpt: "", Language: "", ContentType: "",
        SourceUrlRetrival: null!, Error: false, ErrorMessage: []);

    static ResponseExtractedContent GoodContent() => new(
        MainText: "content", Title: "title", Error: false, ErrorMessage: []);

    static WebSearchClient<TestOptions> CreateClient(
        IUrlRetriver<TestOptions> urlRetriver,
        IHtmlRetriver htmlRetriver,
        IContentExtractor contentExtractor)
        => new(urlRetriver, htmlRetriver, contentExtractor,
               NullLogger<WebSearchClient<TestOptions>>.Instance);

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchAndExtract_HappyPath_ReturnsExtractedContentForEachUrl()
    {
        var client = CreateClient(
            new FakeUrlRetriver([UrlResult("https://a.com"), UrlResult("https://b.com")]),
            new FakeHtmlRetriver(HtmlContent),
            new FakeContentExtractor(_ => GoodContent()));

        var result = await client.SearchAndExtract("query", new TestOptions());

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.False(r.Error));
    }

    [Fact]
    public async Task SearchAndExtract_UrlRetrieverThrows_ReturnsNull()
    {
        var client = CreateClient(
            new ThrowingUrlRetriver(),
            new FakeHtmlRetriver(HtmlContent),
            new FakeContentExtractor(_ => GoodContent()));

        var result = await client.SearchAndExtract("query", new TestOptions());

        Assert.Null(result);
    }

    [Fact]
    public async Task SearchAndExtract_OneExtractionFails_FailedEntryHasErrorAndRemainingSucceed()
    {
        // maxConcurrency=1 makes calls sequential so failOnCall=2 reliably targets the second URL
        var client = CreateClient(
            new FakeUrlRetriver([
                UrlResult("https://a.com"),
                UrlResult("https://b.com"),
                UrlResult("https://c.com")]),
            new FakeHtmlRetriver(HtmlContent),
            new ThrowingOnNthCallContentExtractor(failOnCall: 2));

        var result = await client.SearchAndExtract("query", new TestOptions(), maxConcurrency: 1);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal(1, result.Count(r => r.Error));
        Assert.Equal(2, result.Count(r => !r.Error));
        Assert.NotEmpty(result.Single(r => r.Error).ErrorMessage);
    }

    [Fact]
    public async Task SearchAndExtract_EmptyUrlList_ReturnsEmptyList()
    {
        var client = CreateClient(
            new FakeUrlRetriver([]),
            new FakeHtmlRetriver(HtmlContent),
            new FakeContentExtractor(_ => GoodContent()));

        var result = await client.SearchAndExtract("query", new TestOptions());

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SearchAndExtract_CancelledToken_ThrowsOperationCanceledException()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var client = CreateClient(
            new FakeUrlRetriver([UrlResult("https://a.com")]),
            new FakeHtmlRetriver(HtmlContent),
            new FakeContentExtractor(_ => GoodContent()));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => client.SearchAndExtract("query", new TestOptions(), cancellationToken: cts.Token));
    }

    [Fact]
    public async Task SearchAndExtract_InvalidMaxConcurrency_ThrowsArgumentOutOfRangeException()
    {
        var client = CreateClient(
            new FakeUrlRetriver([UrlResult("https://a.com")]),
            new FakeHtmlRetriver(HtmlContent),
            new FakeContentExtractor(_ => GoodContent()));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.SearchAndExtract("query", new TestOptions(), maxConcurrency: 0));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => client.SearchAndExtract("query", new TestOptions(), maxConcurrency: -1));
    }
}
