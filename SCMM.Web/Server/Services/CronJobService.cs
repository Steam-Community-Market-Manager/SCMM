using Cronos;
using Microsoft.Extensions.Hosting;
using SCMM.Web.Server.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SCMM.Web.Server.Services
{
    public abstract class CronJobService : IHostedService, IDisposable
    {
        private System.Timers.Timer _timer;
        private readonly object _timerLock = new object();
        private readonly CronExpression _expression;
        private readonly bool _startImmediately;

        protected CronJobService(JobConfiguration configuration)
        {
            _expression = CronExpression.Parse(configuration.CronExpression);
            _startImmediately = configuration.StartImmediately;
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            await ScheduleJob(cancellationToken, immediately: _startImmediately);
        }

        protected virtual async Task ScheduleJob(CancellationToken cancellationToken, bool immediately = false)
        {
            var next = _expression.GetNextOccurrence(DateTimeOffset.Now, TimeZoneInfo.Local);
            if (next.HasValue || immediately)
            {
                var delay = (immediately ? TimeSpan.FromSeconds(1) : (next.Value - DateTimeOffset.Now));
                _timer = new System.Timers.Timer(delay.TotalMilliseconds);
                _timer.Elapsed += async (sender, args) =>
                {
                    lock(_timerLock)
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
                        await DoWork(cancellationToken);
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
