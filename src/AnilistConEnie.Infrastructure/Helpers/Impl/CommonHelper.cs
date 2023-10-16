using AnilistConEnie.Infrastructure.Helpers.Interface;
using Google.Cloud.Firestore.V1;
using Google.Cloud.Firestore;
using SixLabors.ImageSharp.Formats.Png;

namespace AnilistConEnie.Infrastructure.Helpers.Impl
{
    public class CommonHelper : ICommonHelper
    {
        public FirestoreDb GetFirestoreClientYumiko()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"firebase-yumiko.json";
            var jsonString = File.ReadAllText(path);
            var builder = new FirestoreClientBuilder { JsonCredentials = jsonString };
            return FirestoreDb.Create("yumiko-1590195019393", builder.Build());
        }

        public async Task<FirestoreDb> GetFirestoreClientAnilistConEnie()
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"firebase-anilistconenie.json";
            var jsonString = File.ReadAllText(path);
            var builder = new FirestoreClientBuilder { JsonCredentials = jsonString };
            return await FirestoreDb.CreateAsync("anilistconenie-e09cb", builder.Build());
        }

        public async Task<byte[]> MergeImage(string link1, string link2, int x, int y)
        {
            var client = new HttpClient();
            var bytes1 = await client.GetByteArrayAsync(link1);
            var bytes2 = await client.GetByteArrayAsync(link2);

            using var memoryStream = new MemoryStream();
            using Image<Rgba32> img1 = Image.Load<Rgba32>(bytes1); // load up source images
            using Image<Rgba32> img2 = Image.Load<Rgba32>(bytes2);

            using var outputImage = new Image<Rgba32>(x, y); // create output image of the correct dimensions

            img1.Mutate(o => o.Resize(new Size(x / 2, y)));
            img2.Mutate(o => o.Resize(new Size(x / 2, y)));

            // take the 2 source images and draw them onto the image
            outputImage.Mutate(o => o
                .DrawImage(img1, new Point(0, 0), 1f) // draw the first one top left
                .DrawImage(img2, new Point(x / 2, 0), 1f)); // draw the second next to it

            // This saves to the memoryStream with encoder
            outputImage.Save(memoryStream, new PngEncoder());
            memoryStream.Position = 0; // The position needs to be reset.

            // return byte[]
            return memoryStream.ToArray();
        }

        public byte[] OverlapImage(byte[] image1, byte[] image2, int x, int y)
        {
            using var memoryStream = new MemoryStream();
            using var outputImage = new Image<Rgba32>(x, y);
            using Image<Rgba32> img1 = Image.Load<Rgba32>(image1);
            using Image<Rgba32> img2 = Image.Load<Rgba32>(image2);

            outputImage.Mutate(o => o
                .DrawImage(img1, new Point(0, 0), 1f)
                .DrawImage(img2, new Point(0, 0), 1f));

            outputImage.Save(memoryStream, new PngEncoder());
            memoryStream.Position = 0;

            return memoryStream.ToArray();
        }

        public MemoryStream ToMemoryStream(byte[] byteArray)
        {
            return new MemoryStream(byteArray)
            {
                Position = 0,
            };
        }
    }
}
