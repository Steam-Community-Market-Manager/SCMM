using Microsoft.AspNetCore.Components;

namespace SCMM.Web.Client.Shared.Components;

public abstract class PersistentComponent : ComponentBase, IDisposable
{
    [Inject]
    private PersistentComponentState ApplicationState { get; set; }

    private PersistingComponentStateSubscription? StateSubscription { get; set; }

    protected override void OnInitialized()
    {
        StateSubscription = ApplicationState?.RegisterOnPersisting(() =>
        {
            OnPersistState();
            return Task.CompletedTask;
        });

        OnLoadState();
    }

    protected virtual void OnLoadState()
    {
    }

    protected virtual void OnPersistState()
    {
    }

    protected T LoadFromState<T>(string name)
    {
        T value = default;
        ApplicationState.TryTakeFromJson(name, out value);
        return value;
    }

    protected void PersistToState<T>(string name, T value)
    {
        ApplicationState.PersistAsJson(name, value);
    }

    public void Dispose()
    {
        StateSubscription?.Dispose();
    }
}
