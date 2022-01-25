using Microsoft.AspNetCore.Components;

namespace SCMM.Web.Client.Shared.Components;

public abstract class PersistentComponent : ComponentBase, IDisposable
{
    [Inject]
    private ILogger<PersistentComponent> Logger { get; set; }

    [Inject]
    private PersistentComponentState ComponentState { get; set; }

    [Inject]
    private AppState ApplicationState { get; set; }

    private PersistingComponentStateSubscription? _stateSubscription;
    private bool _disposedValue;

    protected override Task OnInitializedAsync()
    {
        _stateSubscription = ComponentState?.RegisterOnPersisting(OnPersistStateAsync);
        if (ApplicationState != null)
        {
            ApplicationState.PropertyChanged += OnUIStateChanged;
        }
        return Task.WhenAll(
            OnLoadStateAsync(), base.OnInitializedAsync()
        );
    }

    private void OnUIStateChanged(object sender, EventArgs e)
    {
        StateHasChanged();
    }

    protected virtual Task OnLoadStateAsync()
    {
        return Task.CompletedTask;
    }

    protected virtual Task OnPersistStateAsync()
    {
        return Task.CompletedTask;
    }

    protected T RestoreFromStateOrDefault<T>(string name, T defaultValue)
    {
        return ComponentState.TryTakeFromJson(name, out T value)
            ? value
            : defaultValue;
    }

    protected async Task<T> RestoreFromStateOrLoad<T>(string name, Func<Task<T>> loader)
    {
        if (!ComponentState.TryTakeFromJson(name, out T value))
        {
            try
            {
                value = await loader();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Error loading state data for {name}");
            }
        }
        return value;
    }

    protected void PersistToState<T>(string name, T value)
    {
        ComponentState.PersistAsJson(name, value);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _stateSubscription?.Dispose();
                if (ApplicationState != null)
                {
                    ApplicationState.PropertyChanged -= OnUIStateChanged;
                }
            }

            _stateSubscription = null;
            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
