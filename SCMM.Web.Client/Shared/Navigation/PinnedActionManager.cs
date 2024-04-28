using Blazored.LocalStorage;
namespace SCMM.Web.Client.Shared.Navigation;

public class PinnedActionManager
{
    private const string PinnedActionsKey = "PinnedActions";

    private readonly ILocalStorageService _localStorage;

    public PinnedActionManager(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task<IEnumerable<Action>> ListPinnedActions()
    {
        return (await _localStorage.GetItemAsync<Action[]>(PinnedActionsKey) ?? Array.Empty<Action>()).ToArray();
    }

    public async Task PinAction(Action action)
    {
        var newActions = (await ListPinnedActions()).Append(action).ToArray();
        await _localStorage.SetItemAsync(PinnedActionsKey, newActions);
        PinnedActionsChanged?.Invoke(newActions);
    }

    public async Task UnpinAction(Action action)
    {
        var newActions = (await ListPinnedActions()).Where(a => a.Id != action.Id).ToArray();
        await _localStorage.SetItemAsync(PinnedActionsKey, newActions);
        PinnedActionsChanged?.Invoke(newActions);
    }

    public async Task<bool> IsPinned(Action action)
    {
        return (await ListPinnedActions()).Any(a => a.Id == action.Id);
    }

    public delegate void PinnedActionsChangedHandler(IEnumerable<Action> pinnedActions);

    public PinnedActionsChangedHandler PinnedActionsChanged;

    public class Action
    {
        public string Id => $"{Type}:{Url}";

        public string Type { get; set; }

        public string Url { get; set; }

        public string IconUrl { get; set; }

        public string Description { get; set; }
    }
}
