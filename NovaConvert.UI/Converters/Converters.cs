using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using NovaConvert.Core.Models;

namespace NovaConvert.UI.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool b = value is bool val && val;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isNull = value == null;
        if (value is string s) isNull = string.IsNullOrWhiteSpace(s);
        if (Invert) isNull = !isNull;
        return isNull ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class CountToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        int count = value is int c ? c : 0;
        bool hasItems = count > 0;
        if (Invert) hasItems = !hasItems;
        return hasItems ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class JobStatusToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is JobStatus status)
        {
            return status switch
            {
                JobStatus.Completed => new SolidColorBrush(Color.FromRgb(34, 197, 94)),   // Green #22C55E
                JobStatus.Converting => new SolidColorBrush(Color.FromRgb(99, 102, 241)), // Indigo #6366F1
                JobStatus.Failed => new SolidColorBrush(Color.FromRgb(239, 68, 68)),      // Red #EF4444
                JobStatus.Cancelled => new SolidColorBrush(Color.FromRgb(156, 163, 175)), // Gray
                JobStatus.Paused => new SolidColorBrush(Color.FromRgb(234, 179, 8)),      // Yellow #EAB308
                _ => new SolidColorBrush(Color.FromRgb(148, 163, 184))                    // Slate
            };
        }
        return new SolidColorBrush(Color.FromRgb(148, 163, 184));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public class FormatCategoryToBadgeBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is FileCategory cat)
        {
            return cat switch
            {
                FileCategory.Image => new SolidColorBrush(Color.FromArgb(40, 168, 85, 247)),      // Purple tint
                FileCategory.Audio => new SolidColorBrush(Color.FromArgb(40, 236, 72, 153)),      // Pink tint
                FileCategory.Video => new SolidColorBrush(Color.FromArgb(40, 59, 130, 246)),      // Blue tint
                FileCategory.Document => new SolidColorBrush(Color.FromArgb(40, 34, 197, 94)),    // Green tint
                FileCategory.Spreadsheet => new SolidColorBrush(Color.FromArgb(40, 16, 185, 129)),// Emerald tint
                FileCategory.Pdf => new SolidColorBrush(Color.FromArgb(40, 239, 68, 68)),         // Red tint
                FileCategory.Archive => new SolidColorBrush(Color.FromArgb(40, 245, 158, 11)),    // Amber tint
                FileCategory.Ebook => new SolidColorBrush(Color.FromArgb(40, 139, 92, 246)),      // Violet tint
                FileCategory.Presentation => new SolidColorBrush(Color.FromArgb(40, 249, 115, 22)),// Orange tint
                _ => new SolidColorBrush(Color.FromArgb(40, 100, 116, 139))
            };
        }
        return new SolidColorBrush(Color.FromArgb(40, 100, 116, 139));
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
