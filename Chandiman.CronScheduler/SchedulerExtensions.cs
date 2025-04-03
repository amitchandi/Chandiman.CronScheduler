using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Chandiman.CronScheduler;
public static class SchedulerExtensions
{
    public static IServiceCollection AddCronScheduler(this IServiceCollection services)
    {
        return services.AddSingleton<IHostedService, SchedulerHostedService>();
    }
}
