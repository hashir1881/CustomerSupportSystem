using CustomerSupport.Application.Services;
using CustomerSupport_CustomerDesktop.Services;
using CustomerSupport_CustomerDesktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace CustomerSupport_CustomerDesktop;

/// <summary>
/// Provides application-specific behavior and hosts the Dependency Injection IoC container.
/// </summary>
public partial class App : Application
{
    private Window? _window;
    public static Window? MainWindowInstance { get; private set; }

    public IServiceProvider Services { get; }

    public static new App Current => (App)Application.Current;

    public App()
    {
        UnhandledException += (sender, e) =>
        {
            try
            {
                var msg = $"Message: {e.Message}\r\nException: {e.Exception}\r\nType: {e.Exception?.GetType().FullName}\r\nStackTrace: {e.Exception?.StackTrace}\r\nInner: {e.Exception?.InnerException}";
                System.IO.File.WriteAllText(@"C:\Users\hashim49494\.gemini\antigravity\scratch\CustomerSupportSystem\crash_log.txt", msg);
            }
            catch { }
            e.Handled = true;
        };

        InitializeComponent();
        Services = ConfigureServices();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // 1. Thread-safe authentication state
        services.AddSingleton<IAuthenticationState, AuthenticationState>();

        // 2. HTTP Message Handlers
        services.AddTransient<AuthHeaderHandler>();

        var apiBaseUri = new Uri("http://localhost:5239/");

        // 3. Typed API Clients
        services.AddHttpClient<IAuthApiClient, AuthApiClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            UseDefaultCredentials = true
        });

        services.AddHttpClient<ISupportRequestApiClient, SupportRequestApiClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
        })
        .AddHttpMessageHandler<AuthHeaderHandler>();

        services.AddHttpClient<IAttachmentApiClient, AttachmentApiClient>(client =>
        {
            client.BaseAddress = apiBaseUri;
        })
        .AddHttpMessageHandler<AuthHeaderHandler>();

        // 4. Conversational Triage Engine
        services.AddTransient<ISupportTriageEngine, SupportTriageEngine>();

        // 5. SignalR Real-Time Chat Service
        services.AddLogging();
        services.AddSingleton<IChatHubService, ChatHubService>();

        // 6. ViewModels
        services.AddSingleton<MainViewModel>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        MainWindowInstance = _window;
        _window.Activate();
    }
}
