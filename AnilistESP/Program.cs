namespace AnilistESP
{
    class Program
    {
        static void Main()
        {
            var anilistEspBot = new AniListConEnie_Bot();
            anilistEspBot.RunAsync().GetAwaiter().GetResult();
        }
    }
}
