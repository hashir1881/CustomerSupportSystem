using System.IO;
using CustomerSupport_CsDesktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;

namespace CustomerSupport_CsDesktop;

public sealed partial class MainPage : Page
{
    public CsMainViewModel ViewModel { get; }

    public MainPage()
    {
        ViewModel = App.Current.Services.GetRequiredService<CsMainViewModel>();
        InitializeComponent();
        Loaded += MainPage_Loaded;

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

    private async void AgentSwitcher_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox comboBox && comboBox.SelectedItem is string agentDomain)
        {
            if (ViewModel.CurrentAgentDomain != agentDomain)
            {
                await ViewModel.SwitchAgentAsync(agentDomain);
            }
        }
    }

    private void QueueTab0_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedQueueIndex = 0;
    }

    private void QueueTab1_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedQueueIndex = 1;
    }

    private void QueueTab2_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedQueueIndex = 2;
    }

    private async void LiveChatInputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await ViewModel.SendLiveMessageAsync();
        }
    }

    private void LiveChatInputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var hasText = !string.IsNullOrWhiteSpace(LiveChatInputBox.Text);
        _ = ViewModel.NotifyTypingAsync(hasText);
    }

    private async void AttachScreenshotButton_Click(object sender, RoutedEventArgs e)
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

                await ViewModel.SendScreenshotAsync(base64, file.Name);
            }
        }
        catch (Exception ex)
        {
            ViewModel.ErrorMessage = $"Failed to pick/attach image: {ex.Message}";
        }
    }

    private async void PasteScreenshotButton_Click(object sender, RoutedEventArgs e)
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

                await ViewModel.SendScreenshotAsync(base64, "clipboard_screenshot.png");
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

