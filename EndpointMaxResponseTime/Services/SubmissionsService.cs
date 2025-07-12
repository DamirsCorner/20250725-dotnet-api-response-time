using EndpointMaxResponseTime.Models;

namespace EndpointMaxResponseTime.Services;

public class SubmissionsService(ApiClient client, SubmissionsRepository repository)
{
    public async Task<Guid> CreateAsync()
    {
        var submission = new Submission(Guid.NewGuid(), null, null);
        repository.Add(submission);

        Task[] tasks = [ProcessAsync(submission), Task.Delay(TimeSpan.FromSeconds(1))];
        await Task.WhenAny(tasks);

        return submission.Id;
    }

    private async Task ProcessAsync(Submission submission)
    {
        var phase1CompletedAt = await client.ProcessPhase1Async(submission.Id);
        submission = submission with { Phase1CompletedAt = phase1CompletedAt };
        repository.Add(submission);

        var phase2CompletedAt = await client.ProcessPhase2Async(submission.Id);
        submission = submission with { Phase2CompletedAt = phase2CompletedAt };
        repository.Add(submission);
    }
}
