namespace AQ.AI;

public class AiConversation(Guid id)
{
    public Guid Id { get; private set; } = id;
    public List<Message> Messages { get; private set; } = [];
    public float TokenEstimate => Messages.Sum(m => m.TokenEstimate);

    public IEnumerable<OllamaChatMessage> OllamaChatMessages => Messages.Select(m => m.ToOllamaChatMessage);

    public string GetMessagesToSummarize(int take)
    {
        return string.Join("\n", Messages.Take(take).Select(m => m.Role switch
        {
            Role.User => $"User: {m.Content}",
            Role.Assistant => $"Assistant: {m.Content}",
            Role.System => $"System: {m.Content}",
            _ => m.Content
        }));
    }

    public void ClearMessages() => Messages.Clear();

    public void AddSummary(string summary, int messageCount)
    {
        var recent = Messages.Skip(messageCount).ToList();
        ClearMessages();
        Messages.Add(new Message(summary, Role.System));
        Messages.AddRange(recent);
    }

    public void AddUserMessage(string content) => Messages.Add(new Message(content, Role.User));
    public void AddAssistantMessage(string content) => Messages.Add(new Message(content, Role.Assistant));
    public void AddSystemMessage(string instruction) => Messages.Add(new Message(instruction, Role.System));

    public class Message(string content, Role role)
    {
        public string Content { get; private set; } = content;
        public Role Role { get; private set; } = role;

        public float TokenEstimate => Content.Length / 4;

        public OllamaChatMessage ToOllamaChatMessage
        {
            get
            {
                return new OllamaChatMessage
                {
                    Role = Role.ToString().ToLower(),
                    Content = Content
                };
            }
        }
    }

    public enum Role
    {
        User,
        Assistant,
        System
    }
}




