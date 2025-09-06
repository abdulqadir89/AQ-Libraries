namespace AQ.AI;

public interface IConversationStore
{
    AiConversation GetOrCreate(Guid id);
    void Clear(Guid id);
}
