using System;
using System.Globalization;
using InventoryScanner.Models;

namespace InventoryScanner.Converters
{
    /// <summary>
    /// Converts session status string to a corresponding color.
    /// </summary>
    public class StatusToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                SessionStatus.Draft => Color.FromArgb("#78909C"),       // Blue Grey
                SessionStatus.InProgress => Color.FromArgb("#1E88E5"),  // Blue
                SessionStatus.Completed => Color.FromArgb("#43A047"),   // Green
                SessionStatus.Archived => Color.FromArgb("#FB8C00"),    // Orange
                _ => Color.FromArgb("#9E9E9E")                          // Grey
            };
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts boolean to visibility (true = Visible, false = Collapsed).
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b;
            if (value is string s)
                return !string.IsNullOrEmpty(s);
            return value != null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Inverts a boolean value.
    /// </summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b ? !b : value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is bool b ? !b : value;
        }
    }

    /// <summary>
    /// Converts a count to a visibility boolean (count > 0 = true).
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int count)
                return count > 0;
            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts scanning boolean to button text (true = "⏸" pause, false = "▶" play).
    /// </summary>
    public class BoolToScanTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? "⏸" : "▶";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts scanning boolean to button color (true = red/stop, false = green/go).
    /// </summary>
    public class BoolToScanColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? Color.FromArgb("#F44336") : Color.FromArgb("#4CAF50");

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts busy boolean to login button text.
    /// </summary>
    public class BoolToLoginTextConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? "Signing In..." : "Sign In";

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Converts success boolean to color (true = green, false = orange).
    /// </summary>
    public class BoolToSuccessColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? Color.FromArgb("#4CAF50") : Color.FromArgb("#FF9800");

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
