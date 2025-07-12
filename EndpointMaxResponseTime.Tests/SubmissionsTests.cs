using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using EndpointMaxResponseTime.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace EndpointMaxResponseTime.Tests;

public class SubmissionsTests
{
    private WebApplicationFactory<Program> _factory;
    private WireMockServer _server;

    [SetUp]
    public void Setup()
    {
        _server = WireMockServer.Start();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration(
                (_, config) =>
                {
                    config.AddInMemoryCollection(
                        new Dictionary<string, string?> { ["BaseAddress"] = _server.Url }
                    );
                }
            );
        });
    }

    [TearDown]
    public void TearDown()
    {
        _factory.Dispose();
        _server.Dispose();
    }

    [Test]
    public async Task ImmediateResponseTest()
    {
        SetUpApiResponses(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1));

        using var httpClient = _factory.CreateClient();

        var stopwatch = Stopwatch.StartNew();
        var response = await httpClient.PostAsync("/submissions", new StringContent(string.Empty));
        stopwatch.Stop();
        response.EnsureSuccessStatusCode();
        var submissionFromCreate = await response.Content.ReadFromJsonAsync<Submission>();
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(500);

        var submissionFromGet = await httpClient.GetFromJsonAsync<Submission>(
            $"/submissions/{submissionFromCreate?.Id}"
        );
        submissionFromGet.ShouldBeEquivalentTo(submissionFromCreate);
    }

    [Test]
    public async Task SlowResponseAfterPointOfNoReturnTest()
    {
        SetUpApiResponses(TimeSpan.FromSeconds(0.6), TimeSpan.FromSeconds(0.6));

        using var httpClient = _factory.CreateClient();

        var stopwatch = Stopwatch.StartNew();
        var response = await httpClient.PostAsync("/submissions", new StringContent(string.Empty));
        stopwatch.Stop();
        response.EnsureSuccessStatusCode();
        var submissionFromCreate = await response.Content.ReadFromJsonAsync<Submission>();
        stopwatch.ElapsedMilliseconds.ShouldBeInRange(1000, 1100);
        submissionFromCreate.ShouldNotBeNull();
        submissionFromCreate.Id.ShouldNotBe(Guid.Empty);
        submissionFromCreate.Phase1CompletedAt.ShouldNotBeNull();
        submissionFromCreate.Phase2CompletedAt.ShouldBeNull();

        await Task.Delay(TimeSpan.FromSeconds(1));

        var submissionFromGet = await httpClient.GetFromJsonAsync<Submission>(
            $"/submissions/{submissionFromCreate.Id}"
        );
        submissionFromGet.ShouldNotBeNull();
        submissionFromGet.Id.ShouldBe(submissionFromCreate.Id);
        submissionFromGet.Phase1CompletedAt.ShouldNotBeNull();
        submissionFromGet.Phase2CompletedAt.ShouldNotBeNull();
    }

    [Test]
    public async Task SlowResponseBeforePointOfNoReturnTest()
    {
        SetUpApiResponses(TimeSpan.FromSeconds(1.1), TimeSpan.FromSeconds(1.1));

        using var httpClient = _factory.CreateClient();

        var stopwatch = Stopwatch.StartNew();
        var response = await httpClient.PostAsync("/submissions", new StringContent(string.Empty));
        stopwatch.Stop();
        response.StatusCode.ShouldBe(HttpStatusCode.GatewayTimeout);
        stopwatch.ElapsedMilliseconds.ShouldBeInRange(1000, 1100);
    }

    private void SetUpApiResponses(TimeSpan phase1Delay, TimeSpan phase2Delay)
    {
        _server
            .Given(Request.Create().WithPath("/phase1"))
            .RespondWith(
                Response
                    .Create()
                    .WithBodyAsJson(_ => new ApiResponse(DateTime.Now))
                    .WithDelay(phase1Delay)
            );

        _server
            .Given(Request.Create().WithPath("/phase2"))
            .RespondWith(
                Response
                    .Create()
                    .WithBodyAsJson(_ => new ApiResponse(DateTime.Now))
                    .WithDelay(phase2Delay)
            );
    }
}
