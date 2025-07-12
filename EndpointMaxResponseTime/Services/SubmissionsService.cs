using EndpointMaxResponseTime.Models;

namespace EndpointMaxResponseTime.Services;

public class SubmissionsService(ApiClient client, SubmissionsRepository repository)
{
    public async Task<Guid> CreateAsync()
    {
        var submission = new Submission(Guid.NewGuid(), null, null);

        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(1));
        Task[] tasks =
        [
            ProcessAsync(submission, cancellationTokenSource.Token),
            Task.Delay(TimeSpan.FromSeconds(1), CancellationToken.None),
        ];
        await Task.WhenAny(tasks);

        return submission.Id;
    }

    private async Task ProcessAsync(Submission submission, CancellationToken cancellationToken)
    {
        var phase1CompletedAt = await client.ProcessPhase1Async(submission.Id, cancellationToken);
        submission = submission with { Phase1CompletedAt = phase1CompletedAt };
        repository.Add(submission);

        var phase2CompletedAt = await client.ProcessPhase2Async(
            submission.Id,
            CancellationToken.None
        );
        submission = submission with { Phase2CompletedAt = phase2CompletedAt };
        repository.Add(submission);
    }
}
