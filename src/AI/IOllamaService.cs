namespace AQ.AI;

public interface IOllamaService
{
    IEnumerable<string> GetAvailableModels();

    // Generate
    Task<string> GenerateAsync(string model, string prompt, CancellationToken ct = default);
    IAsyncEnumerable<string> GenerateStreamAsync(string model, string prompt, CancellationToken ct = default);
    Task<string> GenerateAsync(string prompt, CancellationToken ct = default);
    IAsyncEnumerable<string> GenerateStreamAsync(string prompt, CancellationToken ct = default);

    // Chat
    Task<OllamaChatMessage> ChatAsync(string model, IEnumerable<OllamaChatMessage> messages, CancellationToken ct = default);
    IAsyncEnumerable<OllamaChatMessage> ChatStreamAsync(string model, IEnumerable<OllamaChatMessage> messages, CancellationToken ct = default);
    Task<OllamaChatMessage> ChatAsync(IEnumerable<OllamaChatMessage> messages, CancellationToken ct = default);
    IAsyncEnumerable<OllamaChatMessage> ChatStreamAsync(IEnumerable<OllamaChatMessage> messages, CancellationToken ct = default);

    // Embeddings (always non-stream)
    Task<float[]> EmbedAsync(string prompt, CancellationToken ct = default);
    Task<float[]> EmbedAsync(string model, string prompt, CancellationToken ct = default);

    // Helpers
    Task<string> GenerateAsync(string model, string prompt, string context, CancellationToken ct) => GenerateAsync(model, $"{context}\n\n{prompt}", ct);
    Task<string> SummarizeAsync(string model, string content, CancellationToken ct) => GenerateAsync(model, $"Summarize the following content in a concise manner:\n\n{content}", ct);
    Task<string> ParaphraseAsync(string model, string content, CancellationToken ct) => GenerateAsync(model, $"Paraphrase the following content:\n\n{content}", ct);
    Task<string> ImproveAsync(string model, string content, CancellationToken ct = default) => GenerateAsync(model, $"Improve the following content:\n\n{content}", ct);

    // Stream Helpers
    IAsyncEnumerable<string> GenerateStreamAsync(string model, string prompt, string context, CancellationToken ct) => GenerateStreamAsync(model, $"{context}\n\n{prompt}", ct);
    IAsyncEnumerable<string> SummarizeStreamAsync(string model, string content, CancellationToken ct = default) => GenerateStreamAsync(model, $"Summarize the following content in a concise manner:\n\n{content}", ct);
    IAsyncEnumerable<string> ParaphraseStreamAsync(string model, string content, CancellationToken ct = default) => GenerateStreamAsync(model, $"Paraphrase the following content:\n\n{content}", ct);
    IAsyncEnumerable<string> ImproveStreamAsync(string model, string content, CancellationToken ct = default) => GenerateStreamAsync(model, $"Improve the following content:\n\n{content}", ct);
}


public record OllamaChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}
