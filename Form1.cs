using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TimerOverlay
{
    public partial class Form1 : Form
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE_LL = 14;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")] static extern IntPtr SetWindowsHookEx(int id, LowLevelProc cb, IntPtr hMod, uint tid);
        [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int n, IntPtr wp, IntPtr lp);
        [DllImport("kernel32.dll")] static extern IntPtr GetModuleHandle(string name);

        [StructLayout(LayoutKind.Sequential)]
        struct KBDLLHOOKSTRUCT { public uint vkCode, scanCode, flags, time; public IntPtr extra; }
        [StructLayout(LayoutKind.Sequential)]
        struct MSLLHOOKSTRUCT
        {
            public System.Drawing.Point pt;
            public uint mouseData, flags, time;
            public IntPtr extra;
        }

        private IntPtr _kbHook, _msHook;
        private LowLevelProc _kbProc, _msProc;

        private KeyBinding _bindStart;
        private KeyBinding _bindAdd30;
        private bool _stopwatchMode;

        private enum TimerState { Idle, Running, Stopped, Finished }
        private TimerState _state = TimerState.Idle;

        private readonly Stopwatch _sw = new Stopwatch();
        private double _maxSeconds = 180.0;

        private DateTime _lastPressTime = DateTime.MinValue;
        private const double P_COOLDOWN = 1.0;

        private bool _dragging = false;
        private Point _dragOffset;

        public Form1()
        {
            InitializeComponent();

            var (bs, ba, sw) = SettingsManager.Load();
            _bindStart = bs;
            _bindAdd30 = ba;
            _stopwatchMode = sw;

            this.BackColor = Color.FromArgb(0x11, 0x11, 0x11);
            ResetDisplay();

            SetupHooks();
            SetupContextMenu();

            foreach (Control c in this.Controls)
            {
                c.MouseDown += Drag_MouseDown;
                c.MouseMove += Drag_MouseMove;
                c.MouseUp += Drag_MouseUp;
            }
            this.MouseDown += Drag_MouseDown;
            this.MouseMove += Drag_MouseMove;
            this.MouseUp += Drag_MouseUp;
        }

        private void ResetDisplay()
        {
            _maxSeconds = 180.0;
            _state = TimerState.Idle;
            _sw.Reset();
            lblTimer.ForeColor = Color.FromArgb(0x00, 0xCC, 0xFF);
            lblTimer.Text = _stopwatchMode ? "0.0" : FormatTime(_maxSeconds);
        }

        private void SetupContextMenu()
        {
            var menu = new ContextMenuStrip();
            menu.BackColor = Color.FromArgb(0x1a, 0x1a, 0x1a);
            menu.ForeColor = Color.White;
            menu.RenderMode = ToolStripRenderMode.System;

            var itemSettings = new ToolStripMenuItem("⚙  Settings");
            itemSettings.Click += (s, e) => OpenSettings();

            var itemExit = new ToolStripMenuItem("✕  Exit");
            itemExit.Click += (s, e) => Application.Exit();

            menu.Items.Add(itemSettings);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(itemExit);

            this.ContextMenuStrip = menu;
            foreach (Control c in this.Controls)
                c.ContextMenuStrip = menu;
        }

        private void OpenSettings()
        {
            RemoveHooks();
            using (var sf = new SettingsForm(_bindStart, _bindAdd30, _stopwatchMode))
            {
                sf.ShowDialog(this);
                _bindStart = sf.BindStart;
                _bindAdd30 = sf.BindAdd30;

                if (_stopwatchMode != sf.StopwatchMode)
                {
                    _stopwatchMode = sf.StopwatchMode;
                    ResetDisplay();
                }
            }
            SetupHooks();
        }

        private void SetupHooks()
        {
            _kbProc = KbHookCallback;
            _msProc = MsHookCallback;
            using (var mod = Process.GetCurrentProcess().MainModule)
            {
                var h = GetModuleHandle(mod.ModuleName);
                _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, h, 0);
                _msHook = SetWindowsHookEx(WH_MOUSE_LL, _msProc, h, 0);
            }
        }

        private void RemoveHooks()
        {
            UnhookWindowsHookEx(_kbHook);
            UnhookWindowsHookEx(_msHook);
        }



        private string FormatTime(double seconds)
        {
            if (seconds < 0) seconds = 0;

            if (seconds < 60.0)
            {
                int hundredths = (int)(seconds * 100) % 100;
                return $"{(int)seconds}.{hundredths:D2}";
            }
            else
            {
                int mins = (int)seconds / 60;
                int secs = (int)seconds % 60;
                int tenths = (int)(seconds * 10) % 10;
                return $"{mins}:{secs:D2}.{tenths}";
            }
        }

        private void ToggleStart()
        {
            switch (_state)
            {
                case TimerState.Idle:
                    _sw.Restart();
                    _state = TimerState.Running;
                    lblTimer.ForeColor = Color.FromArgb(0x00, 0xCC, 0xFF);
                    break;

                case TimerState.Running:
                    _sw.Stop();
                    _state = TimerState.Stopped;
                    lblTimer.ForeColor = Color.FromArgb(0xFF, 0x88, 0x00);
                    break;

                case TimerState.Stopped:
                case TimerState.Finished:
                    ResetDisplay();
                    break;
            }
        }

        private void Add30Seconds()
        {
            if (_stopwatchMode) return;
            if ((DateTime.Now - _lastPressTime).TotalSeconds < P_COOLDOWN) return;
            _lastPressTime = DateTime.Now;
            if (_state == TimerState.Idle || _state == TimerState.Finished) return;
            _maxSeconds += 30.0;
        }

        private bool MatchesKey(KeyBinding b, uint vkCode)
            => !b.IsMouseButton && (uint)b.Key == vkCode;
        private bool MatchesMouse(KeyBinding b, MouseButton mb)
            => b.IsMouseButton && b.Mouse == mb;

        private IntPtr KbHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                if (MatchesKey(_bindStart, kb.vkCode)) this.Invoke(new Action(ToggleStart));
                else if (MatchesKey(_bindAdd30, kb.vkCode)) this.Invoke(new Action(Add30Seconds));
                else if (kb.vkCode == (uint)Keys.Escape) this.Invoke(new Action(() => Application.Exit()));
            }
            return CallNextHookEx(_kbHook, nCode, wParam, lParam);
        }

        private IntPtr MsHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var ms = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                MouseButton? mb = null;

                if (wParam == (IntPtr)WM_LBUTTONDOWN) mb = MouseButton.Left;
                else if (wParam == (IntPtr)WM_RBUTTONDOWN) mb = MouseButton.Right;
                else if (wParam == (IntPtr)WM_MBUTTONDOWN) mb = MouseButton.Middle;
                else if (wParam == (IntPtr)WM_XBUTTONDOWN)
                {
                    int btn = (int)(ms.mouseData >> 16) & 0xFFFF;
                    mb = btn == 1 ? MouseButton.X1 : MouseButton.X2;
                }

                if (mb.HasValue)
                {
                    if (MatchesMouse(_bindStart, mb.Value)) this.Invoke(new Action(ToggleStart));
                    else if (MatchesMouse(_bindAdd30, mb.Value)) this.Invoke(new Action(Add30Seconds));
                }
            }
            return CallNextHookEx(_msHook, nCode, wParam, lParam);
        }

        private void Drag_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left) { _dragging = true; _dragOffset = e.Location; }
        }
        private void Drag_MouseMove(object sender, MouseEventArgs e)
        {
            if (_dragging)
            {
                var p = Control.MousePosition;
                this.Location = new Point(p.X - _dragOffset.X, p.Y - _dragOffset.Y);
            }
        }
        private void Drag_MouseUp(object sender, MouseEventArgs e) { _dragging = false; }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            RemoveHooks();
            base.OnFormClosed(e);
        }
    
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void timerTick_Tick_1(object sender, EventArgs e)
        {
            if (_state != TimerState.Running) return;

            if (_stopwatchMode)
            {
                double elapsed = _sw.Elapsed.TotalSeconds;
                lblTimer.Text = FormatTime(elapsed);
                lblTimer.ForeColor = Color.FromArgb(0x00, 0xCC, 0xFF);
            }
            else
            {
                double remaining = _maxSeconds - _sw.Elapsed.TotalSeconds;
                if (remaining <= 0.0)
                {
                    remaining = 0.0;
                    _sw.Stop();
                    _state = TimerState.Finished;
                    lblTimer.Text = FormatTime(0);
                    lblTimer.ForeColor = Color.FromArgb(0x00, 0xFF, 0x88);
                    return;
                }
                lblTimer.Text = FormatTime(remaining);
                lblTimer.ForeColor = Color.FromArgb(0x00, 0xCC, 0xFF);
            }
        }
    }
}