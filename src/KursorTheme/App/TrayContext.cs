using System.Drawing;
using KursorTheme.Config;
using KursorTheme.Overlay;
using KursorTheme.Support;
using KursorTheme.UI;

namespace KursorTheme.App;

internal sealed class TrayContext : ApplicationContext
{
    private const int HotkeyToggleEffects = 1;
    private const int HotkeySelection = 2;
    private const int HotkeySettings = 3;

    private readonly SettingsHub _hub;
    private readonly OverlayWindow _overlay;
    private readonly NotifyIcon _tray;
    private readonly HotkeyWindow _hotkeys;
    private readonly ToolStripMenuItem _effectsItem;
    private readonly ToolStripMenuItem _selectionItem;
    private readonly System.Windows.Forms.Timer _watchdog;
    private readonly Icon _icon;

    private SettingsForm? _settings;
    private SelectionOverlay? _selection;
    private bool _appliedStartup;
    private bool _hotkeysApplied;
    private bool _disposed;

    public TrayContext(SettingsHub hub, bool openSettingsOnStart = false)
    {
        _hub = hub;
        _icon = IconFactory.LoadAppIcon();

        var menu = BuildMenu(out _effectsItem, out _selectionItem);
        _tray = new NotifyIcon
        {
            Icon = _icon,
            Text = "Kursor Theme",
            Visible = true,
            ContextMenuStrip = menu,
        };
        _tray.DoubleClick += (_, _) => OpenSettings();

        _hotkeys = new HotkeyWindow();
        _hotkeys.Pressed += OnHotkeyPressed;
        if (hub.Current.HotkeysEnabled)
        {
            _hotkeysApplied = true;
            RegisterHotkeys(notify: hub.Current.ShowNotifications);
        }

        _overlay = new OverlayWindow(hub);
        _overlay.Show();

        _appliedStartup = hub.Current.RunAtStartup;
        AutoStart.Set(_appliedStartup);

        _watchdog = new System.Windows.Forms.Timer { Interval = 900 };
        _watchdog.Tick += (_, _) => WatchFullscreen();
        _watchdog.Start();

        _hub.Changed += OnSettingsChanged;
        UpdateTrayState(hub.Current);

        if (hub.Current.ShowNotifications)
        {
            _tray.ShowBalloonTip(4500, "Kursor Theme",
                "Эффекты включены.\nНастройки — в меню трея. Выделение и горячие клавиши по умолчанию выключены.",
                ToolTipIcon.Info);
        }

        if (openSettingsOnStart) OpenSettings();
    }

    private ContextMenuStrip BuildMenu(out ToolStripMenuItem effectsItem, out ToolStripMenuItem selectionItem)
    {
        var menu = new ContextMenuStrip
        {
            Renderer = new ToolStripProfessionalRenderer(new DarkColorTable()),
            ShowImageMargin = false,
            Font = Theme.Body,
            BackColor = Theme.Surface,
            ForeColor = Theme.Text,
            Padding = new Padding(0, 4, 0, 4),
        };

        var settingsItem = new ToolStripMenuItem("Настройки…") { Padding = new Padding(12, 5, 12, 5) };
        settingsItem.Click += (_, _) => OpenSettings();

        effectsItem = new ToolStripMenuItem("Эффекты курсора") { Padding = new Padding(12, 5, 12, 5) };
        effectsItem.Click += (_, _) => ToggleEffects();

        selectionItem = new ToolStripMenuItem("Выделить область   Ctrl+Alt+S") { Padding = new Padding(12, 5, 12, 5) };
        selectionItem.Click += (_, _) => StartSelection();

        var exitItem = new ToolStripMenuItem("Выход") { Padding = new Padding(12, 5, 12, 5) };
        exitItem.Click += (_, _) => Exit();

        menu.Items.Add(settingsItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(effectsItem);
        menu.Items.Add(selectionItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }

    private void RegisterHotkeys(bool notify)
    {
        _hotkeys.UnregisterAll();

        var toggleOk = _hotkeys.Register(HotkeyToggleEffects, Keys.E, control: true, alt: true);
        var selectionOk = _hotkeys.Register(HotkeySelection, Keys.S, control: true, alt: true);
        _hotkeys.Register(HotkeySettings, Keys.O, control: true, alt: true);

        if (notify && (!toggleOk || !selectionOk))
        {
            _tray.ShowBalloonTip(5000, "Kursor Theme",
                "Не удалось назначить Ctrl+Alt+E / Ctrl+Alt+S — клавиши заняты другой программой. " +
                "Используйте меню в системном трее.",
                ToolTipIcon.Warning);
        }

        if (!toggleOk || !selectionOk)
            Log.Info($"Hotkey registration: toggle={toggleOk}, selection={selectionOk}");
    }

    private void OnHotkeyPressed(int id)
    {
        switch (id)
        {
            case HotkeyToggleEffects:
                ToggleEffects();
                break;
            case HotkeySelection:
                StartSelection();
                break;
            case HotkeySettings:
                OpenSettings();
                break;
        }
    }

    private void ToggleEffects()
    {
        _hub.Update(settings => settings.EffectsEnabled = !settings.EffectsEnabled);
    }

    private void StartSelection()
    {
        if (!_hub.Current.SelectionEnabled)
        {
            if (_hub.Current.ShowNotifications)
            {
                _tray.ShowBalloonTip(2500, "Kursor Theme",
                    "Блок выделения отключён в настройках.", ToolTipIcon.Info);
            }

            return;
        }

        if (_selection is { IsDisposed: false })
        {
            _selection.Cancel();
            return;
        }

        var overlay = new SelectionOverlay(_hub, () => _selection = null);
        _selection = overlay;
        overlay.Show();
        overlay.Activate();
        overlay.Focus();
    }

    private void OpenSettings()
    {
        if (_settings is { IsDisposed: false })
        {
            if (_settings.WindowState == FormWindowState.Minimized)
                _settings.WindowState = FormWindowState.Normal;
            _settings.Show();
            _settings.Activate();
            _settings.BringToFront();
            return;
        }

        _settings = new SettingsForm(_hub, StartSelection);
        _settings.FormClosed += (_, _) => _settings = null;
        _settings.Show();
        _settings.Activate();
    }

    private void WatchFullscreen()
    {
        if (_disposed) return;

        if (!_hub.Current.HideInFullscreen)
        {
            _hub.SetSuspend(SuspendReason.Fullscreen, false);
            return;
        }

        _hub.SetSuspend(SuspendReason.Fullscreen, FullscreenDetector.IsForegroundFullscreen());
    }

    private void OnSettingsChanged(object? sender, AppSettings settings)
    {
        UpdateTrayState(settings);

        if (settings.HotkeysEnabled != _hotkeysApplied)
        {
            _hotkeysApplied = settings.HotkeysEnabled;
            if (_hotkeysApplied) RegisterHotkeys(notify: false);
            else _hotkeys.UnregisterAll();
        }

        if (settings.RunAtStartup != _appliedStartup)
        {
            _appliedStartup = settings.RunAtStartup;
            AutoStart.Set(_appliedStartup);
        }
    }

    private void UpdateTrayState(AppSettings settings)
    {
        _effectsItem.Checked = settings.EffectsEnabled;
        _selectionItem.Enabled = settings.SelectionEnabled;
        _tray.Text = settings.EffectsEnabled
            ? "Kursor Theme — эффекты включены"
            : "Kursor Theme — пауза";
    }

    private void Exit()
    {
        if (_disposed) return;
        _disposed = true;

        _watchdog.Stop();
        _watchdog.Dispose();
        _hotkeys.Dispose();
        _hub.Changed -= OnSettingsChanged;

        try
        {
            _settings?.Close();
            _settings?.Dispose();
        }
        catch
        {
            // Ignore.
        }

        try
        {
            _selection?.Close();
            _selection?.Dispose();
        }
        catch
        {
            // Ignore.
        }

        _overlay.Dispose();

        _tray.Visible = false;
        _tray.Dispose();
        IconFactory.Destroy(_icon);

        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _watchdog?.Stop();
            _watchdog?.Dispose();
            _hotkeys?.Dispose();
            _hub.Changed -= OnSettingsChanged;
            _overlay?.Dispose();
            _tray?.Dispose();
            IconFactory.Destroy(_icon);
        }

        base.Dispose(disposing);
    }
}
