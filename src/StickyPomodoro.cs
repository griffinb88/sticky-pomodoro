// Sticky Pomodoro: a small always-on-top work/break timer for Windows.
// scripts/start.cmd compiles this with the C# compiler included with Windows (C# 5 syntax only).
using System;
using System.Globalization;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

static class Program
{
    [STAThread]
    static int Main()
    {
        bool isFirst;
        using (var mutex = new Mutex(true, @"Local\StickyPomodoroTimer", out isFirst))
        {
            if (!isFirst) return 0; // already running: one timer is enough

            var app = new Application();
            app.DispatcherUnhandledException += (s, e) =>
            {
                MessageBox.Show(e.Exception.Message, "Sticky Pomodoro error");
                e.Handled = true;
            };
            var widget = new Widget();
            return app.Run(widget.Window);
        }
    }
}

sealed class Widget
{
    const string Xaml = @"
<Window xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
        Title='Sticky Pomodoro' Width='216' SizeToContent='Height'
        WindowStyle='None' AllowsTransparency='True' Background='Transparent'
        ResizeMode='NoResize' Topmost='True' ShowInTaskbar='False' ShowActivated='False'
        UseLayoutRounding='True' FontFamily='Segoe UI' WindowStartupLocation='Manual'>
  <Window.Resources>
    <Style x:Key='Icon' TargetType='Button'>
      <Setter Property='Width' Value='26'/>
      <Setter Property='Height' Value='26'/>
      <Setter Property='FontFamily' Value='Segoe MDL2 Assets'/>
      <Setter Property='FontSize' Value='12'/>
      <Setter Property='Foreground' Value='#D6D6DB'/>
      <Setter Property='Cursor' Value='Hand'/>
      <Setter Property='Template'>
        <Setter.Value>
          <ControlTemplate TargetType='Button'>
            <Border x:Name='B' CornerRadius='5' Background='Transparent'>
              <ContentPresenter HorizontalAlignment='Center' VerticalAlignment='Center'/>
            </Border>
            <ControlTemplate.Triggers>
              <Trigger Property='IsMouseOver' Value='True'>
                <Setter TargetName='B' Property='Background' Value='#26FFFFFF'/>
              </Trigger>
              <Trigger Property='IsPressed' Value='True'>
                <Setter TargetName='B' Property='Background' Value='#4DFFFFFF'/>
              </Trigger>
            </ControlTemplate.Triggers>
          </ControlTemplate>
        </Setter.Value>
      </Setter>
    </Style>
    <Style x:Key='Small' TargetType='TextBlock'>
      <Setter Property='Foreground' Value='#D6D6DB'/>
      <Setter Property='FontSize' Value='12'/>
      <Setter Property='VerticalAlignment' Value='Center'/>
    </Style>
    <Style x:Key='Minutes' TargetType='TextBox'>
      <Setter Property='Width' Value='44'/>
      <Setter Property='Height' Value='24'/>
      <Setter Property='Background' Value='#33FFFFFF'/>
      <Setter Property='Foreground' Value='White'/>
      <Setter Property='CaretBrush' Value='White'/>
      <Setter Property='BorderThickness' Value='0'/>
      <Setter Property='TextAlignment' Value='Center'/>
      <Setter Property='VerticalContentAlignment' Value='Center'/>
    </Style>
    <Style x:Key='Check' TargetType='CheckBox'>
      <Setter Property='Foreground' Value='#D6D6DB'/>
      <Setter Property='FontSize' Value='12'/>
      <Setter Property='Margin' Value='0,6,0,0'/>
    </Style>
  </Window.Resources>

  <Border x:Name='Root' CornerRadius='10' Background='#F21E1E22' BorderBrush='#40FFFFFF'
          BorderThickness='1' Padding='12,6,6,10' Cursor='SizeAll'>
    <StackPanel>
      <DockPanel>
        <Button x:Name='CloseBtn' DockPanel.Dock='Right' Style='{StaticResource Icon}' Content='&#xE8BB;'
                ToolTip='Close timer' AutomationProperties.Name='Close timer' Width='22' Height='22' FontSize='9'/>
        <Button x:Name='GearBtn' DockPanel.Dock='Right' Style='{StaticResource Icon}' Content='&#xE713;'
                ToolTip='Settings' AutomationProperties.Name='Settings' Width='22' Height='22' FontSize='11'/>
        <TextBlock x:Name='ModeText' Text='FOCUS' FontSize='11' FontWeight='SemiBold' VerticalAlignment='Center'/>
      </DockPanel>

      <DockPanel Margin='0,0,0,6'>
        <Button x:Name='ResetBtn' DockPanel.Dock='Right' Style='{StaticResource Icon}' Content='&#xE72C;'
                ToolTip='Restart this timer' AutomationProperties.Name='Restart this timer'/>
        <Button x:Name='SkipBtn' DockPanel.Dock='Right' Style='{StaticResource Icon}' Content='&#xE893;'
                ToolTip='Skip to next timer' AutomationProperties.Name='Skip to next timer'/>
        <Button x:Name='PlayBtn' DockPanel.Dock='Right' Style='{StaticResource Icon}' Content='&#xE768;'
                ToolTip='Start or pause' AutomationProperties.Name='Start or pause'/>
        <TextBlock x:Name='TimeText' Text='25:00' Foreground='White' FontSize='30' FontWeight='SemiBold'
                   Typography.NumeralAlignment='Tabular' VerticalAlignment='Center'/>
      </DockPanel>

      <Border x:Name='Track' Height='3' CornerRadius='1.5' Background='#26FFFFFF' Margin='0,0,6,0'>
        <Border x:Name='Bar' HorizontalAlignment='Left' CornerRadius='1.5'/>
      </Border>

      <StackPanel x:Name='SettingsPanel' Visibility='Collapsed' Margin='0,10,6,0' Cursor='Arrow'>
        <DockPanel Margin='0,0,0,4'>
          <Button x:Name='WorkPlus' DockPanel.Dock='Right' Style='{StaticResource Icon}' Content='&#xE710;'
                  AutomationProperties.Name='More work minutes' Width='24' Height='24' FontSize='10'/>
          <TextBox x:Name='WorkBox' DockPanel.Dock='Right' Style='{StaticResource Minutes}'
                   AutomationProperties.Name='Work minutes'/>
          <Button x:Name='WorkMinus' DockPanel.Dock='Right' Style='{StaticResource Icon}' Content='&#xE738;'
                  AutomationProperties.Name='Fewer work minutes' Width='24' Height='24' FontSize='10'/>
          <TextBlock Style='{StaticResource Small}' Text='Work (min)'/>
        </DockPanel>
        <DockPanel>
          <Button x:Name='BreakPlus' DockPanel.Dock='Right' Style='{StaticResource Icon}' Content='&#xE710;'
                  AutomationProperties.Name='More break minutes' Width='24' Height='24' FontSize='10'/>
          <TextBox x:Name='BreakBox' DockPanel.Dock='Right' Style='{StaticResource Minutes}'
                   AutomationProperties.Name='Break minutes'/>
          <Button x:Name='BreakMinus' DockPanel.Dock='Right' Style='{StaticResource Icon}' Content='&#xE738;'
                  AutomationProperties.Name='Fewer break minutes' Width='24' Height='24' FontSize='10'/>
          <TextBlock Style='{StaticResource Small}' Text='Break (min)'/>
        </DockPanel>
        <CheckBox x:Name='SoundCheck' Style='{StaticResource Check}' Content='Play a chime'/>
        <CheckBox x:Name='AutoCheck' Style='{StaticResource Check}' Content='Start next timer by itself'/>
      </StackPanel>
    </StackPanel>
  </Border>
</Window>";

    const double MinMinutes = 0.05;
    const double MaxMinutes = 600;
    const double DefaultWidth = 216;
    const int AutoStartFlashTicks = 40; // 10 seconds at 4 ticks a second

    static readonly Color FocusColor = Color.FromRgb(0xFF, 0x7A, 0x66);
    static readonly Color BreakColor = Color.FromRgb(0x4C, 0xC3, 0x8A);
    static readonly Brush BaseBackground = new SolidColorBrush(Color.FromArgb(0xF2, 0x1E, 0x1E, 0x22));

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr after, int x, int y, int cx, int cy, uint flags);
    static readonly IntPtr HwndTopmost = new IntPtr(-1);
    const uint NoSizeNoMoveNoActivate = 0x0001 | 0x0002 | 0x0010;

    public readonly Window Window;

    readonly Border root, track, bar;
    readonly TextBlock modeText, timeText;
    readonly Button playBtn;
    readonly StackPanel settingsPanel;
    readonly TextBox workBox, breakBox;
    readonly CheckBox soundCheck, autoCheck;
    readonly string settingsPath;
    readonly DispatcherTimer timer;

    double workMinutes = 25, breakMinutes = 5;
    bool soundOn = true, autoStart = false;

    bool onBreak, running, alerting;
    TimeSpan phaseLength, remaining;
    DateTime endsAtUtc;
    int alertTicks, topmostTicks;
    SoundPlayer player; // kept in a field so it isn't collected mid-chime

    public Widget()
    {
        Window = (Window)XamlReader.Parse(Xaml);
        root = Find<Border>("Root");
        track = Find<Border>("Track");
        bar = Find<Border>("Bar");
        modeText = Find<TextBlock>("ModeText");
        timeText = Find<TextBlock>("TimeText");
        playBtn = Find<Button>("PlayBtn");
        settingsPanel = Find<StackPanel>("SettingsPanel");
        workBox = Find<TextBox>("WorkBox");
        breakBox = Find<TextBox>("BreakBox");
        soundCheck = Find<CheckBox>("SoundCheck");
        autoCheck = Find<CheckBox>("AutoCheck");

        settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pomodoro-settings.txt");
        double savedLeft, savedTop;
        LoadSettings(out savedLeft, out savedTop);
        PlaceWindow(savedLeft, savedTop);

        workBox.Text = Format(workMinutes);
        breakBox.Text = Format(breakMinutes);
        soundCheck.IsChecked = soundOn;
        autoCheck.IsChecked = autoStart;

        phaseLength = TimeSpan.FromMinutes(workMinutes);
        remaining = phaseLength;

        Wire();

        timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        timer.Tick += (s, e) => Tick();
        timer.Start();
        Render();
    }

    T Find<T>(string name) where T : class
    {
        return (T)Window.FindName(name);
    }

    void Wire()
    {
        root.MouseLeftButtonDown += (s, e) =>
        {
            Keyboard.ClearFocus(); // commits any half-typed minutes
            try { Window.DragMove(); } catch (InvalidOperationException) { }
            Save();
        };

        playBtn.Click += (s, e) => TogglePlay();
        Find<Button>("SkipBtn").Click += (s, e) => Skip();
        Find<Button>("ResetBtn").Click += (s, e) => ResetPhase();
        Find<Button>("GearBtn").Click += (s, e) => ToggleSettings();
        Find<Button>("CloseBtn").Click += (s, e) => Window.Close();

        Find<Button>("WorkPlus").Click += (s, e) => Step(workBox, true, 1);
        Find<Button>("WorkMinus").Click += (s, e) => Step(workBox, true, -1);
        Find<Button>("BreakPlus").Click += (s, e) => Step(breakBox, false, 1);
        Find<Button>("BreakMinus").Click += (s, e) => Step(breakBox, false, -1);

        WireBox(workBox, true);
        WireBox(breakBox, false);

        RoutedEventHandler soundChanged = (s, e) => { soundOn = soundCheck.IsChecked == true; Save(); };
        soundCheck.Checked += soundChanged;
        soundCheck.Unchecked += soundChanged;
        RoutedEventHandler autoChanged = (s, e) => { autoStart = autoCheck.IsChecked == true; Save(); };
        autoCheck.Checked += autoChanged;
        autoCheck.Unchecked += autoChanged;

        Window.Closing += (s, e) =>
        {
            Commit(workBox, true);
            Commit(breakBox, false);
            Save();
        };
    }

    void WireBox(TextBox box, bool isWork)
    {
        box.LostKeyboardFocus += (s, e) => Commit(box, isWork);
        box.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter) { Commit(box, isWork); Keyboard.ClearFocus(); }
        };
    }

    // ---- timer ----

    void Tick()
    {
        if (running)
        {
            remaining = endsAtUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) FinishPhase();
        }

        if (alerting)
        {
            alertTicks++;
            // With auto-start the next timer is already going, so the flash stops by itself.
            // Otherwise it keeps flashing until you respond.
            if (running && alertTicks > AutoStartFlashTicks) StopAlert();
        }

        // Other always-on-top windows can land above this one; put it back on top.
        if (++topmostTicks >= 20)
        {
            topmostTicks = 0;
            IntPtr handle = new WindowInteropHelper(Window).Handle;
            if (handle != IntPtr.Zero) SetWindowPos(handle, HwndTopmost, 0, 0, 0, 0, NoSizeNoMoveNoActivate);
        }

        Render();
    }

    void FinishPhase()
    {
        onBreak = !onBreak;
        LoadPhase();
        alerting = true;
        alertTicks = 0;
        running = autoStart;
        if (running) endsAtUtc = DateTime.UtcNow + remaining;
        Chime();
    }

    void LoadPhase()
    {
        phaseLength = TimeSpan.FromMinutes(onBreak ? breakMinutes : workMinutes);
        remaining = phaseLength;
    }

    void TogglePlay()
    {
        StopAlert();
        running = !running;
        if (running) endsAtUtc = DateTime.UtcNow + remaining;
        else remaining = Max(endsAtUtc - DateTime.UtcNow, TimeSpan.Zero);
        Render();
    }

    void Skip()
    {
        StopAlert();
        onBreak = !onBreak;
        LoadPhase();
        if (running) endsAtUtc = DateTime.UtcNow + remaining;
        Render();
    }

    void ResetPhase()
    {
        StopAlert();
        running = false;
        LoadPhase();
        Render();
    }

    void StopAlert()
    {
        alerting = false;
    }

    static TimeSpan Max(TimeSpan a, TimeSpan b)
    {
        return a > b ? a : b;
    }

    void Render()
    {
        Color accent = onBreak ? BreakColor : FocusColor;
        var accentBrush = new SolidColorBrush(accent);

        int seconds = (int)Math.Ceiling(Max(remaining, TimeSpan.Zero).TotalSeconds);
        timeText.Text = string.Format("{0:00}:{1:00}", seconds / 60, seconds % 60);

        modeText.Text = alerting ? (onBreak ? "BREAK TIME" : "BACK TO WORK") : (onBreak ? "BREAK" : "FOCUS");
        modeText.Foreground = accentBrush;
        bar.Background = accentBrush;
        playBtn.Content = running ? "" : "";

        double fraction = phaseLength.TotalSeconds > 0 ? remaining.TotalSeconds / phaseLength.TotalSeconds : 0;
        bar.Width = Math.Max(0, Math.Min(1, fraction)) * track.ActualWidth;

        bool flashOn = alerting && alertTicks % 4 < 2;
        root.Background = flashOn
            ? new SolidColorBrush(Color.FromArgb(0xF2, Dim(accent.R), Dim(accent.G), Dim(accent.B)))
            : BaseBackground;
    }

    static byte Dim(byte channel)
    {
        return (byte)(channel * 0.55);
    }

    void Chime()
    {
        if (!soundOn) return;
        try
        {
            string wav = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                                      "Media", "Windows Notify Calendar.wav");
            if (File.Exists(wav))
            {
                player = new SoundPlayer(wav);
                player.Play();
                return;
            }
        }
        catch (Exception) { }
        SystemSounds.Exclamation.Play();
    }

    // ---- settings ----

    void ToggleSettings()
    {
        bool show = settingsPanel.Visibility != Visibility.Visible;
        if (!show)
        {
            Commit(workBox, true);
            Commit(breakBox, false);
        }
        settingsPanel.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    void Commit(TextBox box, bool isWork)
    {
        double value;
        if (TryParseMinutes(box.Text, out value)) SetMinutes(isWork, value);
        box.Text = Format(isWork ? workMinutes : breakMinutes);
    }

    void Step(TextBox box, bool isWork, int direction)
    {
        Commit(box, isWork);
        double current = isWork ? workMinutes : breakMinutes;
        double next = direction > 0 ? Math.Floor(current) + 1 : Math.Max(1, Math.Ceiling(current) - 1);
        SetMinutes(isWork, next);
        box.Text = Format(next);
    }

    void SetMinutes(bool isWork, double value)
    {
        value = Math.Max(MinMinutes, Math.Min(MaxMinutes, value));
        double old = isWork ? workMinutes : breakMinutes;
        if (value == old) return;

        if (isWork) workMinutes = value; else breakMinutes = value;

        // A timer that hasn't started yet picks up the new length right away.
        // One already in progress keeps going; the new length applies next time.
        if (!running && remaining == phaseLength && isWork != onBreak)
        {
            LoadPhase();
            Render();
        }
        Save();
    }

    static bool TryParseMinutes(string text, out double value)
    {
        bool ok = double.TryParse((text ?? "").Trim().Replace(',', '.'),
                                  NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        return ok && !double.IsNaN(value) && !double.IsInfinity(value);
    }

    static string Format(double minutes)
    {
        return minutes.ToString("0.##", CultureInfo.InvariantCulture);
    }

    void PlaceWindow(double left, double top)
    {
        Rect work = SystemParameters.WorkArea;
        bool onScreen = !double.IsNaN(left) && !double.IsNaN(top)
            && left >= SystemParameters.VirtualScreenLeft - 40
            && top >= SystemParameters.VirtualScreenTop - 10
            && left <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - 40
            && top <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - 40;

        // Default: upper right, low enough to leave a maximized window's close button uncovered.
        Window.Left = onScreen ? left : work.Right - DefaultWidth - 12;
        Window.Top = onScreen ? top : work.Top + 44;
    }

    void LoadSettings(out double left, out double top)
    {
        left = double.NaN;
        top = double.NaN;
        try
        {
            if (!File.Exists(settingsPath)) return;
            foreach (string line in File.ReadAllLines(settingsPath))
            {
                int eq = line.IndexOf('=');
                if (eq < 1) continue;
                string key = line.Substring(0, eq).Trim();
                string text = line.Substring(eq + 1).Trim();
                double number;
                bool isNumber = TryParseMinutes(text, out number);

                if (key == "work" && isNumber) workMinutes = Math.Max(MinMinutes, Math.Min(MaxMinutes, number));
                else if (key == "break" && isNumber) breakMinutes = Math.Max(MinMinutes, Math.Min(MaxMinutes, number));
                else if (key == "left" && isNumber) left = number;
                else if (key == "top" && isNumber) top = number;
                else if (key == "sound") soundOn = text == "1";
                else if (key == "autostart") autoStart = text == "1";
            }
        }
        catch (Exception) { } // a bad settings file just means defaults
    }

    void Save()
    {
        try
        {
            CultureInfo inv = CultureInfo.InvariantCulture;
            File.WriteAllLines(settingsPath, new[]
            {
                "work=" + workMinutes.ToString(inv),
                "break=" + breakMinutes.ToString(inv),
                "left=" + Window.Left.ToString(inv),
                "top=" + Window.Top.ToString(inv),
                "sound=" + (soundOn ? "1" : "0"),
                "autostart=" + (autoStart ? "1" : "0")
            });
        }
        catch (Exception) { } // settings are a convenience; never interrupt the timer
    }
}
