using Coravel.Invocable;

namespace SCMM.Steam.Job.Server.Jobs;

public abstract class InvocableJob : IInvocable, IJob
{
    public async Task Invoke()
    {
        try
        {
            await Run(CancellationToken.None);
        }
        catch (Exception e)
        {
            throw;
        }
    }

    public abstract Task Run(CancellationToken cancellationToken);
}
