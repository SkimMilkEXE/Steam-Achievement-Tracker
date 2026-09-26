using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AchievementTracker.ViewModels;

public class LibrarySortModeLabelConverter : IValueConverter
{
    public static readonly LibrarySortModeLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value switch
    {
        LibrarySortMode.Default => "Default",
        LibrarySortMode.NameAZ => "Name (A-Z)",
        LibrarySortMode.NameZA => "Name (Z-A)",
        LibrarySortMode.MostComplete => "Most Complete",
        LibrarySortMode.LeastComplete => "Least Complete",
        _ => value?.ToString() ?? string.Empty
    };

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
