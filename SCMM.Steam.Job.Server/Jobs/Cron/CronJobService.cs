using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using SCMM.Steam.Job.Server.Jobs.Cron;

namespace SCMM.Steam.Job.Server.Jobs.Cron
{
    public abstract class CronJobService : CronJobService<CronJobConfiguration>
    {
        protected CronJobService(ILogger<CronJobService> logger, CronJobConfiguration configuration)
            : base(logger, configuration) { }
    }

    public abstract class CronJobService<T> : IHostedService, IDisposable
        where T : CronJobConfiguration
    {
        private readonly ILogger<CronJobService<T>> _logger;
        private readonly T _configuration;
        private readonly bool _startImmediately;
        private readonly CronExpression _expression;
        private System.Timers.Timer _timer;
        private readonly object _timerLock = new object();

        protected CronJobService(ILogger<CronJobService<T>> logger, T configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _startImmediately = (_configuration?.StartImmediately ?? false);
            _expression = !String.IsNullOrEmpty(_configuration?.CronExpression)
                ? CronExpression.Parse(_configuration?.CronExpression)
                : null;
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            await ScheduleJob(cancellationToken, immediately: _startImmediately);
        }

        protected virtual async Task ScheduleJob(CancellationToken cancellationToken, bool immediately = false)
        {
            var next = _expression?.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
            if ((next.HasValue == true && next > DateTimeOffset.Now) || immediately)
            {
                var delay = (immediately ? TimeSpan.FromSeconds(1) : (next.Value - DateTimeOffset.Now));
                _timer = new System.Timers.Timer(delay.TotalMilliseconds);
                _timer.Elapsed += async (sender, args) =>
                {
                    lock (_timerLock)
                    {
                        if (_timer != null)
                        {
                            _timer.Dispose();
                            _timer = null;
                        }
                        else
                        {
                            return;
                        }
                    }
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            await DoWork(cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"{this.GetType().Name} work failed");
                        }
                    }
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await ScheduleJob(cancellationToken);
                    }
                };
                _timer.Start();
            }
            await Task.CompletedTask;
        }

        public abstract Task DoWork(CancellationToken cancellationToken);

        public virtual async Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Stop();
            await Task.CompletedTask;
        }

        public virtual void Dispose()
        {
            _timer?.Dispose();
        }

        protected T Configuration => _configuration;
    }
}
