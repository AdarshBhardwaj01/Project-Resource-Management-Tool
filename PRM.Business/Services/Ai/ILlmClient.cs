namespace PRM.Business.Services.Ai;

internal interface ILlmClient
{
    Task<string?> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
