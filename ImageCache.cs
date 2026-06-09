using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace osu_fetcher.Services
{
    public static class ImageCache
    {
        private static readonly HttpClient Http = new();
        private static readonly ConcurrentDictionary<string, BitmapImage?> Cache = new();
        private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "imagecache.log");

        public static async Task<BitmapImage?> LoadAsync(string? url)
        {
            if (string.IsNullOrEmpty(url)) return null;
            if (Cache.TryGetValue(url, out var cached)) return cached;

            try
            {
                if (!Http.DefaultRequestHeaders.Contains("User-Agent"))
                {
                    Http.DefaultRequestHeaders.Add("User-Agent", "osu-fetcher-desktop-app");
                }
                if (Http.DefaultRequestHeaders.Accept.Count == 0)
                {
                    Http.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("image/*"));
                }
                if (!Http.DefaultRequestHeaders.Contains("Referer"))
                {
                    Http.DefaultRequestHeaders.Add("Referer", "https://osu.ppy.sh/");
                }

                using var resp = await Http.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    var msg = $"ImageCache: failed to load '{url}': {(int)resp.StatusCode} {resp.ReasonPhrase}";
                    System.Diagnostics.Debug.WriteLine(msg);
                    try { File.AppendAllText(LogPath, DateTime.Now.ToString("o") + " " + msg + Environment.NewLine); } catch { }
                    return null;
                }
                var bytes = await resp.Content.ReadAsByteArrayAsync();
                var bmp   = new BitmapImage();
                bmp.BeginInit();
                bmp.StreamSource  = new MemoryStream(bytes);
                bmp.CacheOption   = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                Cache[url] = bmp;
                return bmp;
            }
            catch (Exception ex)
            {
                var err = $"ImageCache: failed to load '{url}': {ex.Message}";
                System.Diagnostics.Debug.WriteLine(err);
                try { File.AppendAllText(LogPath, DateTime.Now.ToString("o") + " " + err + Environment.NewLine); } catch { }
                return null;
            }
        }
    }
}
