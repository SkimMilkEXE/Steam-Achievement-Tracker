using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace AchievementTracker.Services;

public static class IconLoader
{
    private static readonly HttpClient Client = new();

    public static async Task LoadAllAsync<T>(IEnumerable<T> items, Func<T, string> getUrl, Action<T, Bitmap> setIcon, int maxConcurrency = 4)
    {
        using var throttle = new SemaphoreSlim(maxConcurrency);

        var tasks = items.Select(async item =>
        {
            var url = getUrl(item);
            if (string.IsNullOrEmpty(url))
                return;

            await throttle.WaitAsync();
            try
            {
                var bytes = await Client.GetByteArrayAsync(url);
                setIcon(item, new Bitmap(new MemoryStream(bytes)));
            }
            catch
            {
                // ponytail: a missing icon just shows blank, not worth surfacing per-item errors
            }
            finally
            {
                throttle.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}
