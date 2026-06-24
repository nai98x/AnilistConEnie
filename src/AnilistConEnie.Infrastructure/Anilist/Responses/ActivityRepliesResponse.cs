namespace AnilistConEnie.Infrastructure.Anilist.Responses;

/// <summary>Respuesta de la query de replies de una actividad: <c>{ "Page": { "activityReplies": [ ... ] } }</c>.</summary>
internal sealed class ActivityRepliesResponse
{
    public ActivityRepliesPageDto? Page { get; set; }
}

internal sealed class ActivityRepliesPageDto
{
    public List<ActivityReplyDto>? ActivityReplies { get; set; }
}

internal sealed class ActivityReplyDto
{
    public int UserId { get; set; }
}
