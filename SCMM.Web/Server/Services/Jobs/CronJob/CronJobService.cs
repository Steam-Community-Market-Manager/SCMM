using Cronos;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services.Jobs.CronJob
{
    public abstract class CronJobService : IHostedService, IDisposable
    {
        private readonly ILogger<CronJobService> _logger;
        private readonly bool _startImmediately;
        private readonly CronExpression _expression;
        private System.Timers.Timer _timer;
        private readonly object _timerLock = new object();

        protected CronJobService(ILogger<CronJobService> logger, CronJobConfiguration configuration)
        {
            _logger = logger;
            _startImmediately = (configuration?.StartImmediately ?? false);
            _expression = !String.IsNullOrEmpty(configuration?.CronExpression)
                ? CronExpression.Parse(configuration?.CronExpression)
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
    }
}
