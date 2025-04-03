using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NCrontab;

namespace Chandiman.CronScheduler;
public class SchedulerHostedService : IHostedService
{
    private Task? _executingTask;

    private CancellationTokenSource? _cts;

    public event EventHandler<UnobservedTaskExceptionEventArgs>? UnobservedTaskException;

    public static readonly List<SchedulerTaskWrapper> _scheduledTasks = [];

    private readonly ILogger _logger;

    public SchedulerHostedService(IEnumerable<IScheduledTask> scheduledTasks, IOptions<CronSchedulerOptions> parseOptions, ILogger<SchedulerHostedService> logger)
    {
        _logger = logger;

        var referenceTime = DateTime.UtcNow;

        foreach (var scheduledTask in scheduledTasks)
        {
            _scheduledTasks.Add(new SchedulerTaskWrapper
            {
                Schedule = CrontabSchedule.Parse(scheduledTask.Schedule, (CrontabSchedule.ParseOptions)parseOptions.Value),
                Task = scheduledTask,
                NextRunTime = referenceTime
            });
        }
    }

    protected async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await ExecuteOnceAsync(cancellationToken);

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }

    private async Task ExecuteOnceAsync(CancellationToken cancellationToken)
    {
        var taskFactory = new TaskFactory(TaskScheduler.Current);
        var referenceTime = DateTime.UtcNow;

        var tasksThatShouldRun = _scheduledTasks.Where(t => t.ShouldRun(referenceTime)).ToList();

        foreach (var taskThatShouldRun in tasksThatShouldRun)
        {
            taskThatShouldRun.Increment();

            await taskFactory.StartNew(
                async () =>
                {
                    try
                    {
                        _logger.LogInformation("Starting " + taskThatShouldRun.Task);
                        //Console.WriteLine("Starting " + taskThatShouldRun.Task);
                        await taskThatShouldRun.Task!.ExecuteAsync(cancellationToken);
                        _logger.LogInformation("Ending " + taskThatShouldRun.Task);
                    }
                    catch (Exception ex)
                    {
                        var args = new UnobservedTaskExceptionEventArgs(
                            ex as AggregateException ?? new AggregateException(ex));

                        UnobservedTaskException?.Invoke(this, args);

                        if (!args.Observed)
                        {
                            throw;
                        }
                    }
                },
                cancellationToken);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Create a linked token so we can trigger cancellation outside of this token's cancellation
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Store the task we're executing
        _executingTask = ExecuteAsync(_cts.Token);

        // If the task is completed then return it, otherwise it's running
        return _executingTask.IsCompleted ? _executingTask : Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop called without start
        if (_executingTask == null)
        {
            return;
        }

        // Signal cancellation to the executing method
        _cts?.Cancel();

        // Wait until the task completes or the stop token triggers
        await Task.WhenAny(_executingTask, Task.Delay(-1, cancellationToken));

        // Throw if cancellation triggered
        cancellationToken.ThrowIfCancellationRequested();
    }

    public class SchedulerTaskWrapper
    {
        public required CrontabSchedule? Schedule { get; set; }
        public required IScheduledTask? Task { get; set; }

        public DateTime LastRunTime { get; set; }
        public DateTime NextRunTime { get; set; }

        public void Increment()
        {
            LastRunTime = NextRunTime;
            if (Schedule is null)
                throw new NullReferenceException("Something went wrong with the Cron Schedule.");
            NextRunTime = Schedule.GetNextOccurrence(NextRunTime);
        }

        public bool ShouldRun(DateTime currentTime)
        {
            return NextRunTime < currentTime && LastRunTime != NextRunTime;
        }
    }
}

public class CronSchedulerOptions
{
    public bool IncludingSeconds { get; set; }

    public static explicit operator CrontabSchedule.ParseOptions(CronSchedulerOptions obj)
    {
        return new CrontabSchedule.ParseOptions { IncludingSeconds = obj.IncludingSeconds };
    }
}

public static class Common_Crons
{
    public static readonly string EVERY_SECOND = "* * * * * *";
}