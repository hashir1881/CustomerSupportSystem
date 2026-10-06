using CustomerSupport.Application.Common.Interfaces;
using CustomerSupport.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerSupport.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ISupportRequestService, SupportRequestService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddTransient<ISupportTriageEngine, SupportTriageEngine>();

        return services;
    }
}
