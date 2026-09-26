using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace AchievementTracker.Services;

public enum TrophyTier { None, Bronze, Silver, Gold, Platinum }

// Loads/caches the four trophy badge bitmaps once and maps a completion fraction to a tier.
public static class TrophyIcons
{
    private static readonly Lazy<IImage> BronzeIcon = new(() => Load("Bronze"));
    private static readonly Lazy<IImage> SilverIcon = new(() => Load("Silver"));
    private static readonly Lazy<IImage> GoldIcon = new(() => Load("Gold"));
    private static readonly Lazy<IImage> PlatinumIcon = new(() => Load("Platinum"));

    public static TrophyTier TierFor(double fraction) => fraction switch
    {
        >= 1.0 => TrophyTier.Platinum,
        >= 0.75 => TrophyTier.Gold,
        >= 0.5 => TrophyTier.Silver,
        >= 0.25 => TrophyTier.Bronze,
        _ => TrophyTier.None
    };

    public static IImage? Get(TrophyTier tier) => tier switch
    {
        TrophyTier.Bronze => BronzeIcon.Value,
        TrophyTier.Silver => SilverIcon.Value,
        TrophyTier.Gold => GoldIcon.Value,
        TrophyTier.Platinum => PlatinumIcon.Value,
        _ => null
    };

    public static IImage? ForFraction(double fraction) => Get(TierFor(fraction));

    // The four source PNGs carry wildly different amounts of transparent padding around the
    // actual badge art, so scaling them into the same box makes them look inconsistently sized.
    // Trimming to the opaque bounding box before display normalizes that regardless of how
    // future replacement assets are exported.
    private static IImage Load(string name)
    {
        using var stream = AssetLoader.Open(new Uri($"avares://AchievementTracker/Assets/{name}.png"));
        var bitmap = new Bitmap(stream);
        return Trim(bitmap);
    }

    private static IImage Trim(Bitmap bitmap)
    {
        var size = bitmap.PixelSize;
        var stride = size.Width * 4;
        var buffer = new byte[stride * size.Height];
        var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        try
        {
            bitmap.CopyPixels(new PixelRect(0, 0, size.Width, size.Height), handle.AddrOfPinnedObject(), buffer.Length, stride);
        }
        finally
        {
            handle.Free();
        }

        int minX = size.Width, minY = size.Height, maxX = -1, maxY = -1;
        for (var y = 0; y < size.Height; y++)
        {
            for (var x = 0; x < size.Width; x++)
            {
                var alpha = buffer[y * stride + x * 4 + 3];
                if (alpha <= 10) continue;

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY)
            return bitmap;

        var trimmed = new PixelRect(minX, minY, maxX - minX + 1, maxY - minY + 1);
        return new CroppedBitmap(bitmap, trimmed);
    }
}
