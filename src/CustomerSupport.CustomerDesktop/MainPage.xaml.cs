using System.IO;
using CustomerSupport.Application.Dtos;
using CustomerSupport_CustomerDesktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace CustomerSupport_CustomerDesktop;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<MainViewModel>();
        InitializeComponent();
        Loaded += MainPage_Loaded;

        ViewModel.ChatMessages.CollectionChanged += (s, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && ViewModel.ChatMessages.Count > 0)
            {
                var item = ViewModel.ChatMessages[^1];
                SafeScrollToBottom(ChatMessagesList, item);
            }
        };

        ViewModel.LiveChatMessages.CollectionChanged += (s, e) =>
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && ViewModel.LiveChatMessages.Count > 0)
            {
                var item = ViewModel.LiveChatMessages[^1];
                SafeScrollToBottom(LiveChatMessagesList, item);
            }
        };
    }

    private void SafeScrollToBottom(ListView? listView, object? item)
    {
        if (listView == null || item == null) return;

        DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                if (!listView.IsLoaded || listView.Visibility != Visibility.Visible)
                    return;

                await System.Threading.Tasks.Task.Delay(50);

                if (listView.IsLoaded && listView.Visibility == Visibility.Visible)
                {
                    listView.UpdateLayout();
                    listView.ScrollIntoView(item);
                }
            }
            catch
            {
                // Defensive: WinUI 3 VirtualizingStackPanel can throw ArgumentException (0x80070057)
                // if layout containers are still generating or view is recycling
            }
        });
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
    }

    private void TicketsListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SupportRequestDto ticket)
        {
            ViewModel.SelectTicket(ticket);
        }
    }

    private async void QuickReplyButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string reply)
        {
            await ViewModel.SelectQuickReplyAsync(reply);
        }
    }

    private async void ChatInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await ViewModel.SendChatMessageAsync();
        }
    }

    private async void LiveChatInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await ViewModel.SendLiveMessageAsync();
        }
    }

    private void ChatInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var hasText = !string.IsNullOrWhiteSpace(ChatInputBox.Text);
        _ = ViewModel.NotifyTypingAsync(hasText);
    }

    private void LiveChatInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var hasText = !string.IsNullOrWhiteSpace(LiveChatInputBox.Text);
        _ = ViewModel.NotifyTypingAsync(hasText);
    }

    private async void AttachTriageScreenshot_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using var stream = await file.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.AsStreamForRead().CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                var base64 = Convert.ToBase64String(bytes);

                await ViewModel.AttachScreenshotAsync(base64, file.Name);
            }
        }
        catch (Exception ex)
        {
            ViewModel.ErrorMessage = $"Failed to pick/attach image: {ex.Message}";
        }
    }

    private async void PasteTriageScreenshot_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var package = Clipboard.GetContent();
            if (package.Contains(StandardDataFormats.Bitmap))
            {
                var streamRef = await package.GetBitmapAsync();
                using var stream = await streamRef.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.AsStreamForRead().CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                var base64 = Convert.ToBase64String(bytes);

                await ViewModel.AttachScreenshotAsync(base64, "clipboard_screenshot.png");
            }
            else
            {
                ViewModel.ErrorMessage = "No image found in Windows clipboard.";
            }
        }
        catch (Exception ex)
        {
            ViewModel.ErrorMessage = $"Failed to paste clipboard screenshot: {ex.Message}";
        }
    }

    private async void AttachLiveScreenshot_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new FileOpenPicker();
            picker.ViewMode = PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".bmp");

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindowInstance);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                using var stream = await file.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.AsStreamForRead().CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                var base64 = Convert.ToBase64String(bytes);

                await ViewModel.SendLiveScreenshotAsync(base64, file.Name);
            }
        }
        catch (Exception ex)
        {
            ViewModel.ErrorMessage = $"Failed to pick/attach image: {ex.Message}";
        }
    }

    private async void PasteLiveScreenshot_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var package = Clipboard.GetContent();
            if (package.Contains(StandardDataFormats.Bitmap))
            {
                var streamRef = await package.GetBitmapAsync();
                using var stream = await streamRef.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.AsStreamForRead().CopyToAsync(memoryStream);
                var bytes = memoryStream.ToArray();
                var base64 = Convert.ToBase64String(bytes);

                await ViewModel.SendLiveScreenshotAsync(base64, "clipboard_screenshot.png");
            }
            else
            {
                ViewModel.ErrorMessage = "No image found in Windows clipboard.";
            }
        }
        catch (Exception ex)
        {
            ViewModel.ErrorMessage = $"Failed to paste clipboard screenshot: {ex.Message}";
        }
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pbox)
        {
            ViewModel.LoginPassword = pbox.Password;
        }
    }

    private async void LoginPasswordBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await ViewModel.SignInAsync();
        }
    }
}

