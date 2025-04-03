namespace Chandiman.CronScheduler;
public interface IScheduledTask
{
    string Schedule { get; }
    Task ExecuteAsync(CancellationToken cancellationToken);
}
