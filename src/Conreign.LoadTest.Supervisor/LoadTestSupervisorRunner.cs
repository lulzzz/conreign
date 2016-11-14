using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Conreign.LoadTest.Supervisor.Utility;
using Humanizer;
using Microsoft.Azure.Batch;
using Microsoft.Azure.Batch.Auth;
using Microsoft.Azure.Batch.Common;
using Serilog;
using Serilog.Formatting.Json;

namespace Conreign.LoadTest.Supervisor
{
    public static class LoadTestSupervisorRunner
    {
        public static Task Run(string[] args)
        {
            var options = LoadTestSupervisorOptions.Parse(args);
            return Run(options);
        }

        public static async Task Run(LoadTestSupervisorOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            var jobOutputDirectory = Path.Combine(options.OutputDirectory, $"loadtest-{options.Name}");
            var logPath = Path.Combine(jobOutputDirectory, "supervisor-log.json");
            ServicePointManager.DefaultConnectionLimit = options.Instances * 4;
            var formatter = new JsonFormatter(renderMessage: true, closingDelimiter: $",{Environment.NewLine}");
            Log.Logger = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .WriteTo.File(formatter, logPath)
                .Enrich.FromLogContext()
                .CreateLogger();
            var logger = Log.Logger;
            var timeoutCts = new CancellationTokenSource();
            try
            {
                logger.Information("Initialized with the following configuration: {@Configuration}", options);
                var creds = new BatchSharedKeyCredentials(options.BatchUrl, options.BatchAccountName,
                    options.BatchAccountKey);
                using (var client = await BatchClient.OpenAsync(creds))
                {
                    logger.Information("Connected to Azure Batch service.");
                    if (string.IsNullOrEmpty(options.ApplicationVersion))
                    {
                        var latestVersion = await client.GetLatestApplicationVersion();
                        if (string.IsNullOrEmpty(latestVersion))
                        {
                            throw new InvalidOperationException("Cannot determine load test application version.");
                        }
                        options.ApplicationVersion = latestVersion;
                    }
                    logger.Information("Will use load test v{Version}.", options.ApplicationVersion);
                    if (!options.UseAutoPool)
                    {
                        var pool = await client.GetOrCreateLoadTestPool(options);
                        await WaitWhileNotSteady(pool);
                    }
                    var job = client.CreateLoadTestJob(options);
                    await job.CommitAsync(cancellationToken: timeoutCts.Token);
                    logger.Information("Created job: {JobId}.", job.Id);
                    var tasks = Enumerable.Range(0, options.Instances)
                        .Select(options.CreateLoadTestTask)
                        .ToList();
                    await client.JobOperations.AddTaskAsync(job.Id, tasks);
                    logger.Information("Added {TasksCount} tasks.", tasks.Count);
                    job = client.JobOperations.GetJob(job.Id, new ODATADetailLevel(selectClause: "id"));
                    job.OnAllTasksComplete = OnAllTasksComplete.TerminateJob;
                    await job.CommitChangesAsync(cancellationToken: timeoutCts.Token);
                    // Calculate run time from this moment
                    timeoutCts.CancelAfter(options.JobTimeout.Add(10.Minutes()));
                    var monitor = client.Utilities.CreateTaskStateMonitor();
                    tasks = await client
                        .JobOperations
                        .ListTasks(job.Id, new ODATADetailLevel(selectClause: "id"))
                        .ToListAsync(cancellationToken: timeoutCts.Token);
                    await monitor.WhenAll(tasks, TaskState.Running, timeoutCts.Token);
                    logger.Information("All tasks are {TaskState}.", TaskState.Running);
                    var watchCts = new CancellationTokenSource();
                    WatchStatistics(logger, tasks, options.TaskStatisticsFetchInterval, watchCts.Token);
                    await monitor.WhenAll(tasks, TaskState.Completed, timeoutCts.Token);
                    watchCts.Cancel();
                    logger.Information("All tasks are {TaskState}.", TaskState.Completed);
                    // ReSharper disable MethodSupportsCancellation
                    foreach (var task in tasks)
                    {
                        await task.RefreshAsync(new ODATADetailLevel(selectClause: "id,executionInfo"));
                        if (task.ExecutionInformation.SchedulingError != null)
                        {
                            logger.Error("Task {TaskId} completed with scheduling error: {@SchedulingError}.",
                                task.Id,
                                task.ExecutionInformation.SchedulingError);
                            continue;
                        }
                        if (task.ExecutionInformation.ExitCode != 0)
                        {
                            logger.Error("Task {TaskId} exited with non-zero code.", task.Id);
                        }
                    }
                    if (options.DownloadLogFiles)
                    {
                        await DownloadLogs(jobOutputDirectory, tasks, options.FileDownloadDegreeOfParallelism);
                    }
                    logger.Information("Load test complete.");
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken == timeoutCts.Token)
                {
                    logger.Error(ex, "Load test timeout.");
                }
                else
                {
                    logger.Fatal(ex, "Load test failed: {ErrorMessage}", ex.Message);
                }
                throw;
            }
            catch (Exception ex)
            {
                logger.Fatal(ex, "Load test failed: {ErrorMessage}", ex.Message);
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        private static async Task DownloadLogs(string jobOutputDirectory, IEnumerable<CloudTask> tasks, int? maxDegreeOfParallelism)
        {
            var minProgressDelta = 500.Kilobytes().Bytes;
            var logger = Log.Logger;
            var files = tasks
                .Select(t => new
                {
                    TaskId = t.Id,
                    LogFile = t.ListNodeFiles().FirstOrDefault(f => f.Name == "log.json")
                })
                .Where(t => t.LogFile != null)
                .ToList();
            await files.SelectAsync(async f =>
            {
                var filePath = Path.Combine(jobOutputDirectory, $"{f.TaskId}-log.json");
                try
                {
                    var totalBytes = f.LogFile.Properties.ContentLength;
                    logger.Information("Task {TaskId} log file size is {LogFileSizeMB:f2} MB.",
                        f.TaskId,
                        totalBytes.Bytes().Megabytes);
                    using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Write, 4096, true))
                    {
                        double? lastProgress = null;
                        var progressTracker = new Progress<ProgressStreamEventArgs>(progressInfo =>
                        {
                            var progress = progressInfo.WrittenBytes;
                            if (lastProgress != null && progress - lastProgress.Value < minProgressDelta)
                            {
                                return;
                            }
                            lastProgress = progress;
                            var percents = (double)progressInfo.WrittenBytes / totalBytes;
                            logger.Information("Downloading log file from task [{TaskId}]: {DownloadedMB:f2} / {TotalMB:f2} MB ({ProgressPercents:P}).",
                                f.TaskId,
                                progress.Bytes().Megabytes,
                                totalBytes.Bytes().Megabytes,
                                percents);
                        });
                        var adapter = new ProgressStreamAdapter(fs, progressTracker);
                        await f.LogFile.CopyToStreamAsync(adapter);
                    }
                    logger.Information("Downloaded log file from task [{TaskId}] to {TaskLogFilePath}.", f.TaskId, filePath);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Failed to download log file from task [{TaskId}] to {TaskLogFilePath}.", f.TaskId, filePath);
                }
                return filePath;
            }, maxDegreeOfParallelism);
        }

        private static async Task WaitWhileNotSteady(CloudPool pool)
        {
            while (true)
            {
                await pool.RefreshAsync();
                Log.Logger.Information("Pool {PoolId} is {AllocationState}.", pool.Id, pool.AllocationState);
                if (pool.AllocationState == AllocationState.Steady)
                {
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        private static async void WatchStatistics(ILogger logger, List<CloudTask> tasks, TimeSpan interval, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));
            var completedTaskIds = new HashSet<string>();
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var task in tasks.Where(x => !completedTaskIds.Contains(x.Id)))
                    {
                        await task.RefreshAsync(new ODATADetailLevel(selectClause: "id,stats"), null, cancellationToken);
                        logger.Information("Task {TaskId} statistics: {@Statistics}", task.Id, task.Statistics);
                        if (task.State == TaskState.Completed)
                        {
                            completedTaskIds.Add(task.Id);
                            logger.Information("Task {TaskId} is {TaskState}.", task.Id, task.State);
                        }
                    }
                    await Task.Delay(interval, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    // Do nothing
                }
            }
        }
    }
}