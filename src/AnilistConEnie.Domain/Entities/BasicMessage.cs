namespace AnilistConEnie.Domain.Entities;

public record BasicMessage(string Content, ulong ChannelId, DateTime CreatedAt);
