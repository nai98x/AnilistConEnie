namespace AnilistESP
{
    class Program
    {
        static void Main()
        {
            var anilistEspBot = new AniListESP_Bot();
            anilistEspBot.RunAsync().GetAwaiter().GetResult();
        }
    }
}
