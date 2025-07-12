using EndpointMaxResponseTime.Models;

namespace EndpointMaxResponseTime.Services;

public class ApiClient(HttpClient httpClient)
{
    public Task<DateTime> ProcessPhase1Async(Guid submissionId)
    {
        return ProcessAsync("/phase1", submissionId);
    }

    public Task<DateTime> ProcessPhase2Async(Guid submissionId)
    {
        return ProcessAsync("/phase2", submissionId);
    }

    private async Task<DateTime> ProcessAsync(string requestUri, Guid submissionId)
    {
        var response = await httpClient.PostAsJsonAsync(requestUri, new ApiRequest(submissionId));
        response.EnsureSuccessStatusCode();
        var parsedResponse = await response.Content.ReadFromJsonAsync<ApiResponse>();
        return parsedResponse!.CompletedAt;
    }
}
