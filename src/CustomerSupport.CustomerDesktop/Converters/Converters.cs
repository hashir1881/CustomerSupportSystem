using CustomerSupport_CustomerDesktop.Models;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace CustomerSupport_CustomerDesktop;

public class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool b = value is bool flag && flag;
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        bool isNotNull = value != null;
        if (Invert) isNotNull = !isNotNull;
        return isNotNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class StringNotNullOrEmptyToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return !string.IsNullOrWhiteSpace(value as string);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class ChatSenderToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ChatSender sender && sender == ChatSender.User)
        {
            return HorizontalAlignment.Right;
        }
        return HorizontalAlignment.Left;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class ChatSenderToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ChatSender sender && sender == ChatSender.User)
        {
            // Accent color for user messages
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 103, 192));
        }

        // Neutral card background for assistant messages
        return Application.Current.Resources.TryGetValue("CardBackgroundFillColorDefaultBrush", out var brush)
            ? brush
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 243, 243, 243));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class ChatSenderToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ChatSender sender && sender == ChatSender.User)
        {
            return new SolidColorBrush(Colors.White);
        }

        return Application.Current.Resources.TryGetValue("TextFillColorPrimaryBrush", out var brush)
            ? brush
            : new SolidColorBrush(Colors.Black);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class ChatSenderToCornerRadiusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is ChatSender sender && sender == ChatSender.User)
        {
            return new CornerRadius(16, 16, 2, 16);
        }
        return new CornerRadius(16, 16, 16, 2);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class IsMineToAlignmentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (value is bool isMine && isMine) ? HorizontalAlignment.Right : HorizontalAlignment.Left;
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class IsMineToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isMine && isMine)
        {
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 103, 192));
        }

        return Application.Current.Resources.TryGetValue("CardBackgroundFillColorDefaultBrush", out var brush)
            ? brush
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 243, 243, 243));
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class IsMineToForegroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isMine && isMine)
        {
            return new SolidColorBrush(Colors.White);
        }

        return Application.Current.Resources.TryGetValue("TextFillColorPrimaryBrush", out var brush)
            ? brush
            : new SolidColorBrush(Colors.Black);
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class IsMineToCornerRadiusConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isMine && isMine)
        {
            return new CornerRadius(16, 16, 2, 16);
        }
        return new CornerRadius(16, 16, 16, 2);
    }
    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

public class StringToImageSourceConverter : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is string url && !string.IsNullOrWhiteSpace(url))
        {
            try
            {
                return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(url));
            }
            catch
            {
                return null;
            }
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotImplementedException();
}

