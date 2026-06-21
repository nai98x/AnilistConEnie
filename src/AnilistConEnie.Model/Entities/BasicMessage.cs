namespace AnilistConEnie.Model.Entities;

public class BasicMessage(string content, ulong channelId, DateTime createdAt)
{
    public string Content { get; set; } = content;
    public ulong ChannelId { get; set; } = channelId;
    public DateTime CreatedAt { get; set; } = createdAt;
}
