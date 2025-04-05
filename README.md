# Chandiman.CronScheduler

Simple asp.net scheduler using cron syntax to schedule tasks without the need for a database.

## Usage
##### IScheduledTask
CronScheduler only accepts 5 part and 6 part cron schedules. It works by creating a class that implements the IScheduledTask interface. IScheduledTask requires implementing the property Schedule and the method ExecuteAsync. Assign the Schedule property as your cron schedule in a string value. Example:

    public class Task1 : IScheduledTask {
	    public string Schedule => "* * * * *"; // Every Minute
	    public ILogger<Task1> Logger { get; set; }

	    public Task1(ILogger<Task1> logger)
	    {
	        Logger = logger;
	    }

	    public async Task ExecuteAsync(CancellationToken cancellationToken)
	    {
	        await Task.Run(() => Logger.LogInformation(DateTime.Now + " - Task1 Execute"), cancellationToken);
	    }
    }
##### Configuration
By default the scheduler only accepts 5 part crons. You can enable 6 part crons by adding the following to your appsettings.json:

	"CronSchedulerOptions": {
	  "IncludingSeconds": true
	}
and the following your Program.cs:

    .Configure<CronSchedulerOptions>(builder.Configuration.GetSection("CronSchedulerOptions"))

##### Add the Service and Tasks
	.AddSingleton<IScheduledTask, Task1>()
	.AddCronScheduler();

## Notes
Currently, tasks do not have parameters and schedules are hard coded to the task. I did it this way because I wanted something simple for micro services that require running one or two tasks on a schedule. I would like to add these things, but I want to avoid adding any kind of database as that adds complexity. I might add these in the future if I can.

I made this library because I wanted a scheduler for my asp.net micro-service without having to use Quartz.net or Hangfire. If you require a more feature-full library I would suggest those two libraries.

Logs start and end times of tasks using ILogger. So if you use serilog or other loggers you will have your task logs available there if needed.

    info: Chandiman.CronScheduler.SchedulerHostedService[0]
      Starting Task1
    info: Task1[0]
      2025-04-05 2:50:59 AM - Task1 Execute
    info: Chandiman.CronScheduler.SchedulerHostedService[0]
      Ending Task1
