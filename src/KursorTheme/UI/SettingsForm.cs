using System.Diagnostics;
using System.Drawing;
using KursorTheme.Config;
using KursorTheme.Interop;
using KursorTheme.UI.Controls;

namespace KursorTheme.UI;

internal sealed class SettingsForm : Form, IMessageFilter
{
    private static readonly (string Title, string Subtitle)[] Pages =
    {
        ("Эффекты курсора", "Живые эффекты вокруг системного курсора — сам курсор и его стиль не меняются"),
        ("Область выделения", "Своя рамка выделения: заливка, косая штриховка, контур и анимация"),
        ("Пресеты", "Готовые палитры и характер эффектов в один клик"),
        ("Система", "Производительность, автозапуск, горячие клавиши и ограничения"),
    };

    private readonly SettingsHub _hub;
    private readonly Action _testSelection;
    private readonly Panel _body;
    private readonly Panel _content;
    private readonly Label _titleLabel;
    private readonly Label _subtitleLabel;
    private readonly Label _statusLabel;
    private readonly Label _statusDot;
    private readonly Label _presetLabel;
    private readonly List<NavButton> _navButtons = new();
    private readonly ThinScrollBar _scrollBar;
    private readonly List<Chip> _trailChips = new();
    private readonly List<Chip> _particleChips = new();
    private readonly List<Chip> _rippleChips = new();
    private readonly List<PresetCard> _presetCards = new();

    private float _scale = 1f;
    private int _page = -1;
    private int _y;
    private int _scrollOffset;
    private bool _syncingScroll;

    public SettingsForm(SettingsHub hub, Action testSelection)
    {
        _hub = hub;
        _testSelection = testSelection;

        Text = "Kursor Theme";
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ClientSize = new Size(1040, 720);
        Location = new Point(
            Math.Max(0, (Screen.PrimaryScreen?.Bounds.Width ?? 1920) - 1040) / 2,
            Math.Max(0, (Screen.PrimaryScreen?.Bounds.Height ?? 1080) - 720) / 2);
        BackColor = Theme.Bg;
        DoubleBuffered = true;
        KeyPreview = true;
        ShowInTaskbar = true;

        // ---------- Sidebar ----------
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 236, BackColor = Theme.BgElevated };
        Controls.Add(sidebar);

        var brand = new BrandPanel { Dock = DockStyle.Top, Height = 108 };
        sidebar.Controls.Add(brand);

        var sidebarBorder = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Theme.BorderSoft };
        sidebar.Controls.Add(sidebarBorder);

        var navHost = new Panel { Location = new Point(16, 128), Size = new Size(204, 4 * 48 + 12), BackColor = Theme.BgElevated };
        sidebar.Controls.Add(navHost);

        var navTitles = new[] { "Эффекты", "Выделение", "Пресеты", "Система" };
        for (var i = 0; i < navTitles.Length; i++)
        {
            var index = i;
            var button = new NavButton
            {
                Text = navTitles[i],
                Location = new Point(0, i * 48),
                Width = navHost.Width,
            };
            button.Click += (_, _) => SetPage(index);
            navHost.Controls.Add(button);
            _navButtons.Add(button);
        }

        var statusPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 92,
            BackColor = Theme.BgElevated,
        };
        sidebar.Controls.Add(statusPanel);

        _statusDot = new Label
        {
            Text = "●",
            Font = Theme.Small,
            ForeColor = Theme.Green,
            BackColor = Theme.BgElevated,
            AutoSize = true,
            Location = new Point(20, 20),
        };
        statusPanel.Controls.Add(_statusDot);

        _statusLabel = new Label
        {
            Text = "Эффекты включены",
            Font = Theme.Body,
            ForeColor = Theme.Text,
            BackColor = Theme.BgElevated,
            AutoSize = true,
            Location = new Point(38, 18),
        };
        statusPanel.Controls.Add(_statusLabel);

        _presetLabel = new Label
        {
            Text = "Пресет: Neon",
            Font = Theme.Small,
            ForeColor = Theme.TextDim,
            BackColor = Theme.BgElevated,
            AutoSize = true,
            Location = new Point(38, 39),
        };
        statusPanel.Controls.Add(_presetLabel);

        var version = new Label
        {
            Text = "Kursor Theme 0.5",
            Font = Theme.Tiny,
            ForeColor = Theme.TextFaint,
            BackColor = Theme.BgElevated,
            AutoSize = true,
            Location = new Point(20, 64),
        };
        statusPanel.Controls.Add(version);

        // ---------- Header ----------
        var header = new Panel
        {
            Location = new Point(236, 0),
            Size = new Size(ClientSize.Width - 236, 104),
            BackColor = Theme.Bg,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        Controls.Add(header);

        _titleLabel = new Label
        {
            Text = Pages[0].Title,
            Font = Theme.H1,
            ForeColor = Theme.Text,
            BackColor = Theme.Bg,
            AutoSize = false,
            Location = new Point(36, 22),
            Size = new Size(520, 34),
        };
        header.Controls.Add(_titleLabel);

        _subtitleLabel = new Label
        {
            Text = Pages[0].Subtitle,
            Font = Theme.Small,
            ForeColor = Theme.TextDim,
            BackColor = Theme.Bg,
            AutoSize = false,
            AutoEllipsis = true,
            Location = new Point(36, 63),
            Size = new Size(header.Width - 72, 20),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        header.Controls.Add(_subtitleLabel);

        var closeButton = new PillButton
        {
            Text = "✕",
            Font = Theme.BodyStrong,
            Size = new Size(34, 34),
            Location = new Point(header.Width - 34 - 34, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        closeButton.Click += (_, _) => Close();
        header.Controls.Add(closeButton);

        var resetButton = new PillButton
        {
            Text = "Сбросить",
            Size = new Size(118, 34),
            Location = new Point(header.Width - 34 - 34 - 12 - 118, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        resetButton.Click += (_, _) => ResetAll();
        header.Controls.Add(resetButton);

        var hairline = new Panel
        {
            Location = new Point(236, 103),
            Size = new Size(ClientSize.Width - 236, 1),
            BackColor = Theme.BorderSoft,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        Controls.Add(hairline);

        // ---------- Body ----------
        _body = new Panel
        {
            Location = new Point(272, 124),
            Size = new Size(ClientSize.Width - 272 - 36, ClientSize.Height - 124 - 36),
            BackColor = Theme.Bg,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
        };
        Controls.Add(_body);

        _content = new Panel
        {
            BackColor = Theme.Bg,
            Location = new Point(0, 0),
            Size = new Size(_body.ClientSize.Width, 0),
            Anchor = AnchorStyles.None,
        };
        _body.Controls.Add(_content);

        _scrollBar = new ThinScrollBar
        {
            Location = new Point(_body.Right - 14, _body.Top + 6),
            Size = new Size(10, _body.Height - 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
            Visible = false,
        };
        Controls.Add(_scrollBar);
        _scrollBar.BringToFront();

        _scrollBar.ValueChanged += (_, _) =>
        {
            if (_syncingScroll) return;
            SetScrollOffset(_scrollBar.Value);
        };

        _body.Resize += (_, _) =>
        {
            _content.Width = _body.ClientSize.Width;
            _content.Top = -_scrollOffset;
            UpdateScrollBar();
        };

        // ---------- Window chrome ----------
        AttachDrag(header);
        AttachDrag(_titleLabel);
        AttachDrag(_subtitleLabel);
        AttachDrag(brand);

        _hub.Changed += OnHubChanged;
        SetPage(0);
        UpdateStatus();
    }

    private static void AttachDrag(Control control)
    {
        control.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Left) return;
            NativeMethods.ReleaseCapture();
            NativeMethods.SendMessage(control.FindForm()?.Handle ?? IntPtr.Zero,
                NativeMethods.WM_NCLBUTTONDOWN, NativeMethods.HTCAPTION, IntPtr.Zero);
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            var preference = NativeMethods.DWMWCP_ROUND;
            NativeMethods.DwmSetWindowAttribute(Handle, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE,
                ref preference, sizeof(int));
            NativeMethods.SetWindowTheme(Handle, "DarkMode_Explorer", null);
            if (_body.IsHandleCreated) NativeMethods.SetWindowTheme(_body.Handle, "DarkMode_Explorer", null);
        }
        catch
        {
            // Windows 10 and older may not support every theme attribute.
        }
    }

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        // Keep the panel above widgets while the user works with it...
        TopMost = true;
        if (IsHandleCreated)
        {
            NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0,
                NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);
        }
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        // ...but let it go behind the browser/other apps as soon as it loses focus.
        TopMost = false;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _scale = DeviceDpi / 96f;

        var workingArea = Screen.PrimaryScreen?.WorkingArea ?? Screen.GetWorkingArea(Point.Empty);
        Location = new Point(
            workingArea.Left + Math.Max(0, (workingArea.Width - Width) / 2),
            workingArea.Top + Math.Max(0, (workingArea.Height - Height) / 2));

        _content.Width = _body.ClientSize.Width;
        _scrollBar.SetBounds(_body.Right - S(14), _body.Top + S(6), S(10), _body.Height - S(12));
        UpdateScrollBar();
        SetScrollOffset(0);

        Application.RemoveMessageFilter(this);
        Application.AddMessageFilter(this);
        UpdateStatus();
    }

    public bool PreFilterMessage(ref Message message)
    {
        const int wmMouseWheel = 0x020A;
        if (message.Msg != wmMouseWheel || IsDisposed || !IsHandleCreated) return false;

        var bodyBounds = _body.RectangleToScreen(_body.ClientRectangle);
        if (!bodyBounds.Contains(Cursor.Position)) return false;

        var delta = (short)((long)message.WParam >> 16);
        if (delta == 0) return false;

        SetScrollOffset(_scrollOffset - Math.Sign(delta) * S(96));
        return true;
    }

    private void UpdateScrollBar()
    {
        var contentHeight = Math.Max(0, _content.Height);
        var viewport = _body.ClientSize.Height;
        var maximum = Math.Max(0, contentHeight - viewport);

        _scrollBar.Maximum = maximum;
        _scrollBar.ViewportRatio = contentHeight <= 0 ? 1f : Math.Clamp(viewport / (float)contentHeight, 0.06f, 1f);

        if (_scrollOffset > maximum) _scrollOffset = maximum;
        _syncingScroll = true;
        _scrollBar.Value = Math.Clamp(_scrollOffset, 0, maximum);
        _syncingScroll = false;
        _scrollBar.Visible = maximum > 0;
        _scrollBar.Invalidate();
    }

    private void SetScrollOffset(int value)
    {
        var viewport = _body.ClientSize.Height;
        var maximum = Math.Max(0, _content.Height - viewport);
        _scrollOffset = Math.Clamp(value, 0, maximum);
        _content.Top = -_scrollOffset;

        _syncingScroll = true;
        _scrollBar.Value = _scrollOffset;
        _syncingScroll = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape) Close();
    }

    private int S(int value) => (int)Math.Round(value * _scale);

    private int ContentWidth => Math.Max(S(360), _body.ClientSize.Width - S(56) - S(14));

    private void SetPage(int index)
    {
        if (_page == index) return;
        _page = index;

        for (var i = 0; i < _navButtons.Count; i++) _navButtons[i].Selected = i == index;

        _titleLabel.Text = Pages[index].Title;
        _subtitleLabel.Text = Pages[index].Subtitle;
        BuildPage();
    }

    private void BuildPage(bool preserveScroll = false)
    {
        var previousOffset = _scrollOffset;

        _content.SuspendLayout();
        foreach (Control control in _content.Controls) control.Dispose();
        _content.Controls.Clear();

        _y = S(4);

        switch (_page)
        {
            case 0:
                BuildEffectsPage();
                break;
            case 1:
                BuildSelectionPage();
                break;
            case 2:
                BuildPresetsPage();
                break;
            default:
                BuildSystemPage();
                break;
        }

        _content.Height = _y + S(28);
        _content.Width = _body.ClientSize.Width;
        _content.ResumeLayout(true);

        UpdateScrollBar();
        SetScrollOffset(preserveScroll ? previousOffset : 0);
    }

    private Panel NewRow(int height, int gapBefore = 0)
    {
        _y += S(gapBefore);
        var row = new Panel
        {
            BackColor = Theme.Bg,
            Height = S(height),
            Width = ContentWidth,
            Location = new Point(S(28), _y),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        _content.Controls.Add(row);
        _y += S(height);
        return row;
    }

    private static Label MakeLabel(string text, Font font, Color color, int x, int y, int width, int height) =>
        new()
        {
            Text = text,
            Font = font,
            ForeColor = color,
            BackColor = Theme.Bg,
            AutoSize = false,
            UseMnemonic = false,
            Bounds = new Rectangle(x, y, width, height),
        };

    private void Section(string title, string? hint = null)
    {
        var row = NewRow(hint == null ? 42 : 58, 16);
        var line = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = Math.Max(1, S(1)),
            BackColor = Theme.BorderSoft,
        };
        row.Controls.Add(line);

        var titleLabel = MakeLabel(title, Theme.H2, Theme.Text, 0, S(hint == null ? 5 : 0), row.Width, S(22));
        titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        row.Controls.Add(titleLabel);

        if (hint == null) return;
        var hintLabel = MakeLabel(hint, Theme.Small, Theme.TextDim, 0, S(24), row.Width, S(20));
        hintLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        row.Controls.Add(hintLabel);
    }

    private void Paragraph(string text, int height = 40)
    {
        var row = NewRow(height);
        var label = MakeLabel(text, Theme.Small, Theme.TextDim, 0, 0, row.Width, row.Height);
        label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        row.Controls.Add(label);
    }

    private void ToggleRow(string title, string description, Func<bool> get, Action<bool> set)
    {
        var row = NewRow(46, 2);
        var titleLabel = MakeLabel(title, Theme.Body, Theme.Text, 0, S(3), row.Width - S(60), S(20));
        titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        var descriptionLabel = MakeLabel(description, Theme.Small, Theme.TextDim, 0, S(22), row.Width - S(60), S(18));
        descriptionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var toggle = new ToggleSwitch
        {
            Checked = get(),
            Location = new Point(row.Width - S(46), S(9)),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(S(44), S(24)),
        };
        toggle.CheckedChanged += (_, _) =>
        {
            set(toggle.Checked);
            UpdateStatus();
        };

        row.Controls.Add(titleLabel);
        row.Controls.Add(descriptionLabel);
        row.Controls.Add(toggle);
    }

    private void SliderRow(
        string title,
        double minimum,
        double maximum,
        double step,
        Func<double> get,
        Action<double> set,
        Func<double, string> format)
    {
        var row = NewRow(58, 2);
        var valueLabel = MakeLabel(format(get()), Theme.BodyStrong, Theme.Accent, row.Width - S(100), S(3), S(100), S(20));
        valueLabel.TextAlign = ContentAlignment.TopRight;
        valueLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        var titleLabel = MakeLabel(title, Theme.Body, Theme.Text, 0, S(3), row.Width - S(110), S(20));
        titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var slider = new Slider
        {
            Minimum = minimum,
            Maximum = maximum,
            Step = step,
            Value = get(),
            Location = new Point(0, S(26)),
            Width = row.Width,
            Height = S(26),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        slider.ValueChanged += (_, _) =>
        {
            set(slider.Value);
            valueLabel.Text = format(slider.Value);
        };

        row.Controls.Add(titleLabel);
        row.Controls.Add(valueLabel);
        row.Controls.Add(slider);
    }

    private void ColorRow(string title, Func<Color> get, Action<Color> set)
    {
        var row = NewRow(40, 2);
        var label = MakeLabel(title, Theme.Body, Theme.Text, 0, S(10), row.Width - S(76), S(20));
        label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var swatch = new ColorSwatch
        {
            Color = get(),
            Location = new Point(row.Width - S(56), S(7)),
            Size = new Size(S(56), S(26)),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        swatch.ColorChanged += (_, _) => set(swatch.Color);

        row.Controls.Add(label);
        row.Controls.Add(swatch);
    }

    private void ButtonRow(string title, string description, string buttonText, Action onClick, bool primary = false)
    {
        var row = NewRow(56, 2);
        var titleLabel = MakeLabel(title, Theme.Body, Theme.Text, 0, S(8), row.Width - S(170), S(20));
        titleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        var descriptionLabel = MakeLabel(description, Theme.Small, Theme.TextDim, 0, S(26), row.Width - S(170), S(18));
        descriptionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        var button = new PillButton
        {
            Text = buttonText,
            Primary = primary,
            Location = new Point(row.Width - S(140), S(12)),
            Size = new Size(S(140), S(32)),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        button.Click += (_, _) => onClick();

        row.Controls.Add(titleLabel);
        row.Controls.Add(descriptionLabel);
        row.Controls.Add(button);
    }

    private void KeyRow(string keys, string description)
    {
        var row = NewRow(36);
        var keyLabel = MakeLabel(keys, Theme.Mono, Theme.Accent, 0, S(7), S(150), S(22));
        var descriptionLabel = MakeLabel(description, Theme.Small, Theme.TextDim, S(160), S(8), row.Width - S(160), S(20));
        descriptionLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        row.Controls.Add(keyLabel);
        row.Controls.Add(descriptionLabel);
    }

    private List<Chip> ChipRow(string title, IReadOnlyList<(string Text, bool Selected, Action OnClick)> items)
    {
        var chips = new List<Chip>(items.Count);
        if (items.Count == 0) return chips;

        var available = Math.Max(S(200), ContentWidth - S(4));
        var widths = new List<int>(items.Count);
        var total = 0;
        foreach (var item in items)
        {
            var width = TextRenderer.MeasureText(item.Text, Theme.Small).Width + S(26);
            widths.Add(width);
            total += width + S(6);
        }

        var lines = Math.Max(1, (int)Math.Ceiling(total / (double)available));
        var height = S(24) + lines * S(32) + S(4);
        var row = NewRow(height, 2);

        var label = MakeLabel(title, Theme.Body, Theme.Text, 0, 0, row.Width, S(20));
        label.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        row.Controls.Add(label);

        var flow = new FlowLayoutPanel
        {
            Location = new Point(0, S(24)),
            Size = new Size(row.Width, height - S(24)),
            BackColor = Theme.Bg,
            WrapContents = true,
            AutoScroll = false,
            Padding = new Padding(0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
        };
        row.Controls.Add(flow);

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var chip = new Chip
            {
                Text = item.Text,
                Selected = item.Selected,
                Size = new Size(widths[i], S(26)),
                Margin = new Padding(0, 0, S(6), S(6)),
            };
            chip.Click += (_, _) => item.OnClick();
            flow.Controls.Add(chip);
            chips.Add(chip);
        }

        return chips;
    }

    private void ApplyTrailStyle(TrailStylePreset preset)
    {
        _hub.Update(settings => settings.TrailStyle = preset.Style);
        UpdateTrailChipSelection(preset.Style);
    }

    private void ApplyParticleStyle(ParticleStylePreset preset)
    {
        _hub.Update(settings => settings.ParticleStyle = preset.Style);
        UpdateParticleChipSelection(preset.Style);
    }

    private void ApplyRippleStyle(RippleStylePreset preset)
    {
        _hub.Update(settings => settings.RippleStyle = preset.Style);
        UpdateRippleChipSelection(preset.Style);
    }

    private void UpdateTrailChipSelection(TrailStyle style)
    {
        for (var i = 0; i < _trailChips.Count && i < TrailStylePresets.All.Count; i++)
        {
            if (_trailChips[i].IsDisposed) continue;
            _trailChips[i].Selected = TrailStylePresets.All[i].Style == style;
        }
    }

    private void UpdateParticleChipSelection(ParticleStyle style)
    {
        for (var i = 0; i < _particleChips.Count && i < ParticleStylePresets.All.Count; i++)
        {
            if (_particleChips[i].IsDisposed) continue;
            _particleChips[i].Selected = ParticleStylePresets.All[i].Style == style;
        }
    }

    private void UpdateRippleChipSelection(RippleStyle style)
    {
        for (var i = 0; i < _rippleChips.Count && i < RippleStylePresets.All.Count; i++)
        {
            if (_rippleChips[i].IsDisposed) continue;
            _rippleChips[i].Selected = RippleStylePresets.All[i].Style == style;
        }
    }

    private void MutateLook(Action<AppSettings> mutate) =>
        _hub.Update(settings =>
        {
            mutate(settings);
            settings.ActivePreset = "custom";
        });

    private void MutateSelection(Action<SelectionSettings> mutate) =>
        _hub.Update(settings =>
        {
            mutate(settings.Selection);
            settings.ActivePreset = "custom";
        });

    // ---------------------------------------------------------------- Effects

    private void BuildEffectsPage()
    {
        Section("Блок эффектов", "Полностью включает или ставит на паузу отрисовку эффектов");
        ToggleRow("Эффекты включены", "Глобальный переключатель, Ctrl+Alt+E — быстрая пауза",
            () => _hub.Current.EffectsEnabled,
            value => _hub.Update(settings => settings.EffectsEnabled = value));
        if (!_hub.Current.EffectsEnabled)
            Paragraph("Блок эффектов отключён: настройки сохранены, но слой не рисуется.", 28);

        Section("Свечение", "Мягкий ореол, который следует за курсором");
        ToggleRow("Включено", "Основной эффект присутствия",
            () => _hub.Current.Glow.Enabled,
            value => MutateLook(settings => settings.Glow.Enabled = value));
        SliderRow("Радиус", 6, 160, 1,
            () => _hub.Current.Glow.Radius,
            value => MutateLook(settings => settings.Glow.Radius = (float)value),
            value => $"{value:0} px");
        SliderRow("Яркость", 0, 1, 0.01,
            () => _hub.Current.Glow.Intensity,
            value => MutateLook(settings => settings.Glow.Intensity = (float)value),
            value => $"{value * 100:0}%");
        SliderRow("Пульсация", 0, 1, 0.01,
            () => _hub.Current.Glow.Pulse,
            value => MutateLook(settings => settings.Glow.Pulse = (float)value),
            value => $"{value * 100:0}%");
        SliderRow("Скорость пульсации", 0.1, 3, 0.05,
            () => _hub.Current.Glow.PulseSpeed,
            value => MutateLook(settings => settings.Glow.PulseSpeed = (float)value),
            value => $"{value:0.00}×");
        ColorRow("Цвет свечения",
            () => _hub.Current.Glow.Color,
            color => MutateLook(settings => settings.Glow.Color = color));

        Section("Шлейф", "Плавный след, который тянется за курсором");
        ToggleRow("Включено", "Работает и в движении, и при остановке",
            () => _hub.Current.Trail.Enabled,
            value => MutateLook(settings => settings.Trail.Enabled = value));
        SliderRow("Длина", 80, 1200, 10,
            () => _hub.Current.Trail.LengthMs,
            value => MutateLook(settings => settings.Trail.LengthMs = (int)value),
            value => $"{value:0} мс");
        SliderRow("Толщина", 1, 40, 0.5,
            () => _hub.Current.Trail.Thickness,
            value => MutateLook(settings => settings.Trail.Thickness = (float)value),
            value => $"{value:0.0} px");
        SliderRow("Прозрачность", 0, 1, 0.01,
            () => _hub.Current.Trail.Opacity,
            value => MutateLook(settings => settings.Trail.Opacity = (float)value),
            value => $"{value * 100:0}%");
        ColorRow("Цвет шлейфа",
            () => _hub.Current.Trail.Color,
            color => MutateLook(settings => settings.Trail.Color = color));

        Section("Стиль шлейфа", "Только форма: длина, толщина, прозрачность и цвет не меняются");
        _trailChips.Clear();
        _trailChips.AddRange(ChipRow("Пресеты шлейфа", TrailStylePresets.All
            .Select(preset => (
                Text: preset.Name,
                Selected: _hub.Current.TrailStyle == preset.Style,
                OnClick: (Action)(() => ApplyTrailStyle(preset))))
            .ToList()));

        Section("Волна по клику", "Расходящееся кольцо в точке нажатия");
        ToggleRow("Включено", "Левая и правая кнопка мыши",
            () => _hub.Current.Ripple.Enabled,
            value => MutateLook(settings => settings.Ripple.Enabled = value));
        SliderRow("Радиус", 20, 260, 5,
            () => _hub.Current.Ripple.MaxRadius,
            value => MutateLook(settings => settings.Ripple.MaxRadius = (float)value),
            value => $"{value:0} px");
        SliderRow("Длительность", 120, 1500, 20,
            () => _hub.Current.Ripple.DurationMs,
            value => MutateLook(settings => settings.Ripple.DurationMs = (int)value),
            value => $"{value:0} мс");
        SliderRow("Толщина", 0.5, 8, 0.1,
            () => _hub.Current.Ripple.Thickness,
            value => MutateLook(settings => settings.Ripple.Thickness = (float)value),
            value => $"{value:0.0} px");
        SliderRow("Колец", 1, 3, 1,
            () => _hub.Current.Ripple.Rings,
            value => MutateLook(settings => settings.Ripple.Rings = (int)value),
            value => $"{value:0}");
        ColorRow("Цвет волны",
            () => _hub.Current.Ripple.Color,
            color => MutateLook(settings => settings.Ripple.Color = color));

        Section("Стиль волны", "Только форма: радиус, длительность, толщина и цвет не меняются");
        _rippleChips.Clear();
        _rippleChips.AddRange(ChipRow("Пресеты волны", RippleStylePresets.All
            .Select(preset => (
                Text: preset.Name,
                Selected: _hub.Current.RippleStyle == preset.Style,
                OnClick: (Action)(() => ApplyRippleStyle(preset))))
            .ToList()));

        Section("Частицы", "Небольшой салют из точки клика");
        ToggleRow("Включено", "Разлетаются и гаснут под действием гравитации",
            () => _hub.Current.Particles.Enabled,
            value => MutateLook(settings => settings.Particles.Enabled = value));
        SliderRow("Количество", 0, 40, 1,
            () => _hub.Current.Particles.Count,
            value => MutateLook(settings => settings.Particles.Count = (int)value),
            value => $"{value:0}");
        SliderRow("Скорость", 40, 900, 10,
            () => _hub.Current.Particles.Speed,
            value => MutateLook(settings => settings.Particles.Speed = (float)value),
            value => $"{value:0} px/с");
        SliderRow("Гравитация", 0, 2000, 20,
            () => _hub.Current.Particles.Gravity,
            value => MutateLook(settings => settings.Particles.Gravity = (float)value),
            value => $"{value:0}");
        SliderRow("Время жизни", 200, 2000, 20,
            () => _hub.Current.Particles.LifetimeMs,
            value => MutateLook(settings => settings.Particles.LifetimeMs = (int)value),
            value => $"{value:0} мс");
        SliderRow("Размер", 1, 12, 0.1,
            () => _hub.Current.Particles.Size,
            value => MutateLook(settings => settings.Particles.Size = (float)value),
            value => $"{value:0.0} px");
        ColorRow("Цвет частиц",
            () => _hub.Current.Particles.Color,
            color => MutateLook(settings => settings.Particles.Color = color));

        Section("Стиль частиц", "Только форма: количество, скорость, размер и цвет не меняются");
        _particleChips.Clear();
        _particleChips.AddRange(ChipRow("Пресеты частиц", ParticleStylePresets.All
            .Select(preset => (
                Text: preset.Name,
                Selected: _hub.Current.ParticleStyle == preset.Style,
                OnClick: (Action)(() => ApplyParticleStyle(preset))))
            .ToList()));
    }

    // -------------------------------------------------------------- Selection

    private void BuildSelectionPage()
    {
        Section("Блок выделения", "Доступность инструмента и горячей клавиши");
        ToggleRow("Выделение включено", "Ctrl+Alt+S и пункт в меню трея",
            () => _hub.Current.SelectionEnabled,
            value => _hub.Update(settings => settings.SelectionEnabled = value));
        if (!_hub.Current.SelectionEnabled)
            Paragraph("Блок выделения отключён: горячая клавиша и запуск из трея не срабатывают.", 28);

        Section("Предпросмотр", "Так выделение выглядит поверх экрана");
        var previewRow = NewRow(200);
        var preview = new SelectionPreview
        {
            Dock = DockStyle.Fill,
            SettingsProvider = () => _hub.Current.Selection,
        };
        previewRow.Controls.Add(preview);

        ButtonRow("Проверить вживую", "Ctrl+Alt+S — глобальная горячая клавиша",
            "Запустить", () => _testSelection(), primary: true);

        Section("Заливка", "Полупрозрачный цвет внутри области");
        ColorRow("Цвет заливки",
            () => _hub.Current.Selection.FillColor,
            color => { MutateSelection(s => s.FillColor = color); preview.Invalidate(); });
        SliderRow("Непрозрачность", 0, 0.6, 0.01,
            () => _hub.Current.Selection.FillOpacity,
            value => { MutateSelection(s => s.FillOpacity = (float)value); preview.Invalidate(); },
            value => $"{value * 100:0}%");

        Section("Штриховка", "Косая линия поверх заливки");
        ColorRow("Цвет линий",
            () => _hub.Current.Selection.HatchColor,
            color => { MutateSelection(s => s.HatchColor = color); preview.Invalidate(); });
        SliderRow("Непрозрачность", 0, 1, 0.01,
            () => _hub.Current.Selection.HatchOpacity,
            value => { MutateSelection(s => s.HatchOpacity = (float)value); preview.Invalidate(); },
            value => $"{value * 100:0}%");
        SliderRow("Угол", 0, 90, 1,
            () => _hub.Current.Selection.HatchAngle,
            value => { MutateSelection(s => s.HatchAngle = (float)value); preview.Invalidate(); },
            value => $"{value:0}°");
        SliderRow("Шаг", 3, 30, 0.5,
            () => _hub.Current.Selection.HatchSpacing,
            value => { MutateSelection(s => s.HatchSpacing = (float)value); preview.Invalidate(); },
            value => $"{value:0.0} px");
        SliderRow("Толщина линий", 0.4, 5, 0.1,
            () => _hub.Current.Selection.HatchWidth,
            value => { MutateSelection(s => s.HatchWidth = (float)value); preview.Invalidate(); },
            value => $"{value:0.0} px");
        ToggleRow("Анимация", "Штриховка медленно движется внутри области",
            () => _hub.Current.Selection.AnimateHatch,
            value => { MutateSelection(s => s.AnimateHatch = value); preview.Invalidate(); });
        SliderRow("Скорость", 0, 80, 1,
            () => _hub.Current.Selection.HatchSpeed,
            value => { MutateSelection(s => s.HatchSpeed = (float)value); preview.Invalidate(); },
            value => $"{value:0} px/с");

        Section("Контур", "Рамка, свечение и уголки");
        ColorRow("Цвет контура",
            () => _hub.Current.Selection.BorderColor,
            color => { MutateSelection(s => s.BorderColor = color); preview.Invalidate(); });
        SliderRow("Непрозрачность", 0, 1, 0.01,
            () => _hub.Current.Selection.BorderOpacity,
            value => { MutateSelection(s => s.BorderOpacity = (float)value); preview.Invalidate(); },
            value => $"{value * 100:0}%");
        SliderRow("Толщина", 0.4, 6, 0.1,
            () => _hub.Current.Selection.BorderWidth,
            value => { MutateSelection(s => s.BorderWidth = (float)value); preview.Invalidate(); },
            value => $"{value:0.0} px");
        ToggleRow("Уголки", "Акценты в четырёх углах области",
            () => _hub.Current.Selection.CornerAccents,
            value => { MutateSelection(s => s.CornerAccents = value); preview.Invalidate(); });
        ToggleRow("Показывать размер", "Бейдж с шириной и высотой",
            () => _hub.Current.Selection.ShowSize,
            value => { MutateSelection(s => s.ShowSize = value); preview.Invalidate(); });

        Section("Поведение", "Затемнение фона и время показа");
        SliderRow("Затемнение фона", 0, 0.9, 0.01,
            () => _hub.Current.Selection.DimOpacity,
            value => { MutateSelection(s => s.DimOpacity = (float)value); preview.Invalidate(); },
            value => $"{value * 100:0}%");
        ToggleRow("Не скрывать автоматически", "Рамка остаётся до нажатия Esc",
            () => _hub.Current.Selection.PersistUntilEsc,
            value => { MutateSelection(s => s.PersistUntilEsc = value); preview.Invalidate(); });
        SliderRow("Скрыть через", 500, 8000, 100,
            () => _hub.Current.Selection.AutoHideMs,
            value => { MutateSelection(s => s.AutoHideMs = (int)value); preview.Invalidate(); },
            value => $"{value / 1000:0.0} с");

        Paragraph("Системную рамку выделения проводника Windows перекрасить нельзя — она рисуется оболочкой. " +
                  "Поэтому стилизованное выделение вызывается отдельно по Ctrl+Alt+S и работает поверх любых окон.", 44);
    }

    // --------------------------------------------------------------- Presets

    private void BuildPresetsPage()
    {
        Section("Блок пресетов", "Готовые наборы можно отключить, не трогая оформление");
        ToggleRow("Пресеты включены", "Когда выключено, карточки не применяются",
            () => _hub.Current.PresetsEnabled,
            value =>
            {
                _hub.Update(settings => settings.PresetsEnabled = value);
                RebuildPageDeferred();
            });
        if (!_hub.Current.PresetsEnabled)
            Paragraph("Блок пресетов отключён. Текущее оформление осталось прежним, карточки неактивны.", 28);

        Section("Готовые наборы", "19 наборов: база, неон, гик, поп, Sims, граффити и тату");

        var gap = S(12);
        var cardWidth = S(300);
        var cardHeight = S(110);
        var columns = Math.Max(1, (ContentWidth + gap) / (cardWidth + gap));
        var rows = (Presets.All.Count + columns - 1) / columns;
        var row = NewRow(rows * (cardHeight + gap) + S(6));

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Bg,
            WrapContents = true,
            AutoScroll = false,
            Padding = new Padding(0, S(4), 0, 0),
        };
        row.Controls.Add(flow);

        var active = _hub.Current.ActivePreset;
        var presetsEnabled = _hub.Current.PresetsEnabled;
        _presetCards.Clear();

        foreach (var preset in Presets.All)
        {
            var card = new PresetCard
            {
                PresetId = preset.Id,
                Title = preset.Name,
                Tagline = preset.Tagline,
                TagText = preset.Tag,
                Dots = new[] { preset.Accent, preset.Secondary },
                AccentColor = preset.Accent,
                Selected = string.Equals(preset.Id, active, StringComparison.OrdinalIgnoreCase),
                Dimmed = !presetsEnabled,
                Size = new Size(cardWidth, cardHeight),
                Margin = new Padding(0, 0, gap, gap),
            };
            card.Activated += (_, _) =>
            {
                if (!_hub.Current.PresetsEnabled) return;
                _hub.Update(settings => Presets.ApplyTo(settings, preset.Id));
                UpdatePresetSelection();
                UpdateStatus();
            };
            flow.Controls.Add(card);
            _presetCards.Add(card);
        }

        Paragraph("Пресет можно донастроить вручную — после первого изменения в статусе появится «Свой набор».", 34);
    }

    private void UpdatePresetSelection()
    {
        var active = _hub.Current.ActivePreset;
        foreach (var card in _presetCards)
        {
            if (card.IsDisposed) continue;
            card.Selected = string.Equals(card.PresetId, active, StringComparison.OrdinalIgnoreCase);
        }
    }

    // ---------------------------------------------------------------- System

    private void BuildSystemPage()
    {
        Section("Блоки функций", "Каждый модуль можно отключить отдельно — настройки сохраняются");
        ToggleRow("Эффекты курсора", "Отрисовка слоя поверх рабочего стола",
            () => _hub.Current.EffectsEnabled,
            value => _hub.Update(settings => settings.EffectsEnabled = value));
        ToggleRow("Пресеты", "Применение готовых наборов оформления",
            () => _hub.Current.PresetsEnabled,
            value => _hub.Update(settings => settings.PresetsEnabled = value));
        ToggleRow("Выделение области", "Инструмент Ctrl+Alt+S и пункт в меню трея",
            () => _hub.Current.SelectionEnabled,
            value => _hub.Update(settings => settings.SelectionEnabled = value));
        ToggleRow("Горячие клавиши", "Ctrl+Alt+E / S / O",
            () => _hub.Current.HotkeysEnabled,
            value => _hub.Update(settings => settings.HotkeysEnabled = value));

        Section("Производительность");
        SliderRow("Частота кадров", 30, 240, 10,
            () => _hub.Current.TargetFps,
            value => _hub.Update(settings => settings.TargetFps = (int)value),
            value => $"{value:0} FPS");
        ToggleRow("Скрывать в полноэкранных приложениях", "Слой скрывается в играх; браузеры и веб-видео остаются",
            () => _hub.Current.HideInFullscreen,
            value => _hub.Update(settings => settings.HideInFullscreen = value));

        Section("Запуск");
        ToggleRow("Запускать вместе с Windows", "Добавляет приложение в автозагрузку текущего пользователя",
            () => _hub.Current.RunAtStartup,
            value => _hub.Update(settings => settings.RunAtStartup = value));
        ToggleRow("Показывать уведомления", "Подсказка при старте и предупреждения о горячих клавишах",
            () => _hub.Current.ShowNotifications,
            value => _hub.Update(settings => settings.ShowNotifications = value));

        Section("Горячие клавиши", "Работают, когда блок горячих клавиш включён");
        KeyRow("Ctrl + Alt + E", "Включить или приостановить эффекты");
        KeyRow("Ctrl + Alt + S", "Выделить область, Esc — отмена");
        KeyRow("Ctrl + Alt + O", "Открыть эту панель настроек");

        Section("Как это работает");
        Paragraph("Эффекты рисуются отдельным прозрачным слоем поверх рабочего стола. " +
                  "Системный курсор не подменяется, поэтому остаются его текущий дизайн, тени и поведение в играх. " +
                  "В окнах с повышенными правами (UAC) и на защищённом рабочем столе слой не виден — это ограничение Windows. " +
                  "Чтобы эффекты работали поверх окон, запущенных от администратора, запустите Kursor Theme от имени администратора.", 72);

        Section("Файлы");
        var pathRow = NewRow(48);
        var pathLabel = MakeLabel(SettingsStore.Folder, Theme.Tiny, Theme.TextFaint, 0, S(6), pathRow.Width - S(170), S(36));
        pathLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        var openFolder = new PillButton
        {
            Text = "Открыть папку",
            Location = new Point(pathRow.Width - S(150), S(8)),
            Size = new Size(S(150), S(32)),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
        };
        openFolder.Click += (_, _) => OpenSettingsFolder();
        pathRow.Controls.Add(pathLabel);
        pathRow.Controls.Add(openFolder);

        var resetRow = NewRow(52, 10);
        var reset = new PillButton
        {
            Text = "Сбросить всё",
            Location = new Point(0, S(8)),
            Size = new Size(S(150), S(34)),
            Anchor = AnchorStyles.Top | AnchorStyles.Left,
        };
        reset.Click += (_, _) => ResetAll();
        resetRow.Controls.Add(reset);
    }

    private static void OpenSettingsFolder()
    {
        try
        {
            Directory.CreateDirectory(SettingsStore.Folder);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{SettingsStore.Folder}\"")
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Support.Log.Error(ex);
        }
    }

    private void ResetAll()
    {
        var answer = MessageBox.Show(this,
            "Сбросить все настройки эффектов и выделения?\nСистемные параметры (FPS, автозапуск) сохранятся.",
            "Kursor Theme",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes) return;

        var current = _hub.Current;
        var fresh = new AppSettings
        {
            TargetFps = current.TargetFps,
            HideInFullscreen = current.HideInFullscreen,
            RunAtStartup = current.RunAtStartup,
            ShowNotifications = current.ShowNotifications,
        };

        Presets.ApplyTo(fresh, Presets.All[0].Id);
        _hub.Apply(fresh);
        RebuildPageDeferred();
    }

    private void RebuildPageDeferred()
    {
        if (IsHandleCreated)
        {
            BeginInvoke(new Action(() =>
            {
                BuildPage(preserveScroll: true);
                UpdateStatus();
            }));
            return;
        }

        BuildPage(preserveScroll: true);
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (IsDisposed) return;
        var settings = _hub.Current;

        _statusLabel.Text = settings.EffectsEnabled ? "Эффекты включены" : "Эффекты на паузе";
        _statusLabel.ForeColor = settings.EffectsEnabled ? Theme.Text : Theme.TextDim;
        _statusDot.ForeColor = settings.EffectsEnabled ? Theme.Green : Theme.Danger;

        var preset = Presets.Find(settings.ActivePreset);
        _presetLabel.Text = "Пресет: " + (preset?.Name ?? "свой набор");
    }

    private void OnHubChanged(object? sender, AppSettings settings)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(UpdateStatus));
            }
            catch
            {
                // Form is closing.
            }

            return;
        }

        UpdateStatus();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        Application.RemoveMessageFilter(this);
        _hub.Changed -= OnHubChanged;
        base.OnFormClosed(e);
    }
}
