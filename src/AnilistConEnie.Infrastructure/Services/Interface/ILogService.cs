using Discord;

namespace AnilistConEnie.Infrastructure.Services.Interface
{
    public interface ILogService
    {
        void ConfigureLogging();
        Task LogAsync(LogMessage message);
    }
}
