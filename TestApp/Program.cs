using Chandiman.CronScheduler;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Add scheduled tasks & scheduler
builder.Services
    .AddSingleton<IScheduledTask, QuoteOfTheDayTask>()
    .AddSingleton<IScheduledTask, Task1>()
    .Configure<CronSchedulerOptions>(builder.Configuration.GetSection("CronSchedulerOptions"))
    .AddCronScheduler();
//.AddScheduler((sender, args) =>
//{
//    Console.Write(args.Exception.Message);
//    args.SetObserved();
//});

var app = builder.Build();



// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/schedule", () =>
{
    return SchedulerHostedService._scheduledTasks.Count;
});

app.Run();

public class QuoteOfTheDayTask : IScheduledTask
{
    public string Schedule => "*/10 * * * * *";

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await Task.Run(() => Console.WriteLine(DateTime.Now + " - Test Execute"), cancellationToken);
    }
}

public class Task1 : IScheduledTask
{
    public string Schedule => Common_Crons.EVERY_SECOND;

    public ILogger<Task1> Logger { get; set; }

    public Task1(ILogger<Task1> logger)
    {
        Logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        //await Task.Run(() => Console.WriteLine(DateTime.Now + " - Task1 Execute"), cancellationToken);
        await Task.Run(() => Logger.LogInformation(DateTime.Now + " - Task1 Execute"), cancellationToken);
    }
}