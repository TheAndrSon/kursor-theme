namespace KursorTheme.Config;

[Flags]
public enum SuspendReason
{
    None = 0,
    Fullscreen = 1,
    Selection = 2,
}

public sealed class SettingsHub : IDisposable
{
    private readonly object _gate = new();
    private readonly object _saveGate = new();
    private readonly System.Threading.Timer _saveTimer;
    private AppSettings _current;
    private AppSettings? _pendingSave;
    private int _suspend;

    public event EventHandler<AppSettings>? Changed;

    public SettingsHub(AppSettings initial)
    {
        _current = SettingsStore.Normalize(initial);
        _saveTimer = new System.Threading.Timer(_ => FlushSave(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public AppSettings Current
    {
        get
        {
            lock (_gate) return _current;
        }
    }

    public AppSettings Snapshot()
    {
        lock (_gate) return SettingsStore.Clone(_current);
    }

    public bool IsSuspended => Volatile.Read(ref _suspend) != 0;

    public void SetSuspend(SuspendReason reason, bool suspended)
    {
        if (suspended) Interlocked.Or(ref _suspend, (int)reason);
        else Interlocked.And(ref _suspend, ~(int)reason);
    }

    public void Apply(AppSettings settings)
    {
        settings = SettingsStore.Normalize(settings);
        lock (_gate) _current = settings;
        Changed?.Invoke(this, settings);
        ScheduleSave(settings);
    }

    public void Update(Action<AppSettings> mutate)
    {
        var copy = Snapshot();
        mutate(copy);
        Apply(copy);
    }

    private void ScheduleSave(AppSettings settings)
    {
        lock (_saveGate) _pendingSave = settings;
        _saveTimer.Change(450, Timeout.Infinite);
    }

    private void FlushSave()
    {
        AppSettings? pending;
        lock (_saveGate)
        {
            pending = _pendingSave;
            _pendingSave = null;
        }

        if (pending != null) SettingsStore.Save(pending);
    }

    public void Dispose()
    {
        _saveTimer.Change(Timeout.Infinite, Timeout.Infinite);
        FlushSave();
        _saveTimer.Dispose();
    }
}
