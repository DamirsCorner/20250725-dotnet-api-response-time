using EndpointMaxResponseTime.Models;

namespace EndpointMaxResponseTime.Services;

public class SubmissionsService(ApiClient client, SubmissionsRepository repository)
{
    public async Task<Submission> CreateAsync()
    {
        var submissionId = Guid.NewGuid();
        var phase1CompletedAt = await client.ProcessPhase1Async(submissionId);
        var phase2CompletedAt = await client.ProcessPhase2Async(submissionId);
        var submission = new Submission(submissionId, phase1CompletedAt, phase2CompletedAt);
        repository.Add(submission);
        return submission;
    }
}
