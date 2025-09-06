namespace AQ.AI;

public interface IAiAssistantService
{
    IAsyncEnumerable<string> HandleAsync(Guid conversationId, string model, string message, CancellationToken cancellationToken = default);
    Task AddInstruction(Guid conversationId, string instruction, CancellationToken cancellationToken = default);
    IEnumerable<string> GetAvailableModels();
}
