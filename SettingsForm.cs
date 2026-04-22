using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TimerOverlay
{
    public partial class SettingsForm : Form
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
            public Point pt;
            public uint mouseData, flags, time;
            public IntPtr extra;
        }

        private IntPtr _kbHook = IntPtr.Zero;
        private IntPtr _msHook = IntPtr.Zero;
        private LowLevelProc _kbProc, _msProc;

        public KeyBinding BindStart { get; private set; }
        public KeyBinding BindAdd30 { get; private set; }
        public bool StopwatchMode { get; private set; }

        private Button _listeningBtn = null;
        private Button _btnStartKey, _btnAdd30Key;
        private Label _lblStatus;
        private CheckBox _chkStopwatch;
        private Label _lblAdd30;
        private Label _lblAdd30Hint;

        public SettingsForm(KeyBinding bindStart, KeyBinding bindAdd30, bool stopwatchMode)
        {
            BindStart = bindStart;
            BindAdd30 = bindAdd30;
            StopwatchMode = stopwatchMode;
            InitializeSettings();
        }

        private void InitializeSettings()
        {
            this.Text = "Settings";
            this.Size = new Size(380, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(0x1a, 0x1a, 0x1a);
            this.ForeColor = Color.White;

            var lblStart = MakeLabel("Start / Stop:", new Point(20, 30));
            _btnStartKey = MakeBindButton(BindStart.DisplayName, new Point(170, 28));
            _btnStartKey.Click += (s, e) => StartListening(_btnStartKey);

            _lblAdd30 = MakeLabel("+30 Seconds:", new Point(20, 80));
            _btnAdd30Key = MakeBindButton(BindAdd30.DisplayName, new Point(170, 78));
            _btnAdd30Key.Click += (s, e) => StartListening(_btnAdd30Key);

            _lblAdd30Hint = new Label
            {
                Text = "⚠ +30s key is disabled in Stopwatch mode",
                Location = new Point(20, 108),
                Size = new Size(330, 18),
                ForeColor = Color.FromArgb(0xFF, 0x88, 0x00),
                Font = new Font("Consolas", 7.5f),
                Visible = StopwatchMode
            };

            var sep = new Label
            {
                Location = new Point(15, 132),
                Size = new Size(335, 1),
                BackColor = Color.FromArgb(0x44, 0x44, 0x44)
            };

            _chkStopwatch = new CheckBox
            {
                Text = "Timer for 1v1",
                Location = new Point(20, 145),
                Size = new Size(330, 24),
                ForeColor = Color.FromArgb(0x00, 0xCC, 0xFF),
                BackColor = Color.Transparent,
                Font = new Font("Consolas", 9f),
                Checked = StopwatchMode,
                Cursor = Cursors.Hand
            };
            _chkStopwatch.CheckedChanged += (s, e) =>
            {
                StopwatchMode = _chkStopwatch.Checked;
                _btnAdd30Key.Enabled = !StopwatchMode;
                _btnAdd30Key.ForeColor = StopwatchMode
                    ? Color.FromArgb(0x44, 0x44, 0x44)
                    : Color.FromArgb(0x00, 0xCC, 0xFF);
                _lblAdd30Hint.Visible = StopwatchMode;
            };

            _btnAdd30Key.Enabled = !StopwatchMode;
            _btnAdd30Key.ForeColor = StopwatchMode
                ? Color.FromArgb(0x44, 0x44, 0x44)
                : Color.FromArgb(0x00, 0xCC, 0xFF);

            _lblStatus = new Label
            {
                Text = "Click to save",
                Location = new Point(20, 178),
                Size = new Size(330, 18),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(0x66, 0x66, 0x66),
                Font = new Font("Consolas", 8f)
            };

            var btnSave = new Button
            {
                Text = "Save",
                Location = new Point(115, 205),
                Size = new Size(140, 34),
                BackColor = Color.FromArgb(0x00, 0x88, 0x44),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK,
                Font = new Font("Consolas", 10f, System.Drawing.FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                StopListening();
                SettingsManager.Save(BindStart, BindAdd30, StopwatchMode);
                this.Close();
            };

            this.Controls.AddRange(new Control[]
            {
                lblStart,    _btnStartKey,
                _lblAdd30,   _btnAdd30Key,  _lblAdd30Hint,
                sep,
                _chkStopwatch,
                _lblStatus,  btnSave
            });

            this.FormClosing += (s, e) => StopListening();
        }

        private Label MakeLabel(string text, Point loc) => new Label
        {
            Text = text,
            Location = loc,
            Size = new Size(140, 30),
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(0xBB, 0xBB, 0xBB),
            Font = new Font("Consolas", 9f)
        };

        private Button MakeBindButton(string text, Point loc)
        {
            var b = new Button
            {
                Text = text,
                Location = loc,
                Size = new Size(175, 30),
                BackColor = Color.FromArgb(0x2a, 0x2a, 0x2a),
                ForeColor = Color.FromArgb(0x00, 0xCC, 0xFF),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Consolas", 9f)
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(0x00, 0x88, 0xAA);
            return b;
        }

        private void StartListening(Button btn)
        {
            if (_listeningBtn != null)
            {
                _listeningBtn.Text = GetBindingForBtn(_listeningBtn).DisplayName;
                _listeningBtn.ForeColor = Color.FromArgb(0x00, 0xCC, 0xFF);
            }

            _listeningBtn = btn;
            btn.Text = "Press any key...";
            btn.ForeColor = Color.FromArgb(0xFF, 0xAA, 0x00);
            _lblStatus.Text = "Waiting for input — Esc to cancel";
            _lblStatus.ForeColor = Color.FromArgb(0xFF, 0xAA, 0x00);

            StopHooks();
            _kbProc = KbHookCallback;
            _msProc = MsHookCallback;
            using (var mod = System.Diagnostics.Process.GetCurrentProcess().MainModule)
            {
                var h = GetModuleHandle(mod.ModuleName);
                _kbHook = SetWindowsHookEx(WH_KEYBOARD_LL, _kbProc, h, 0);
                _msHook = SetWindowsHookEx(WH_MOUSE_LL, _msProc, h, 0);
            }
        }

        private void StopListening()
        {
            StopHooks();
            if (_listeningBtn != null)
            {
                var b = _listeningBtn;
                _listeningBtn = null;
                b.Text = GetBindingForBtn(b).DisplayName;
                b.ForeColor = Color.FromArgb(0x00, 0xCC, 0xFF);
            }
            _lblStatus.Text = "Click this to save";
            _lblStatus.ForeColor = Color.FromArgb(0x66, 0x66, 0x66);
        }

        private void StopHooks()
        {
            if (_kbHook != IntPtr.Zero) { UnhookWindowsHookEx(_kbHook); _kbHook = IntPtr.Zero; }
            if (_msHook != IntPtr.Zero) { UnhookWindowsHookEx(_msHook); _msHook = IntPtr.Zero; }
        }

        private KeyBinding GetBindingForBtn(Button btn)
            => (btn == _btnStartKey) ? BindStart : BindAdd30;

        private void ApplyBinding(KeyBinding newBinding)
        {
            if (_listeningBtn == null) return;
            if (_listeningBtn == _btnStartKey) BindStart = newBinding;
            else BindAdd30 = newBinding;
            this.Invoke(new Action(() => StopListening()));
        }

        private IntPtr KbHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN && _listeningBtn != null)
            {
                var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                var key = (Keys)kb.vkCode;
                if (key == Keys.Escape) this.Invoke(new Action(() => StopListening()));
                else ApplyBinding(new KeyBinding(key));
                return (IntPtr)1;
            }
            return CallNextHookEx(_kbHook, nCode, wParam, lParam);
        }

        private IntPtr MsHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _listeningBtn != null)
            {
                var ms = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                MouseButton? mb = null;

                if (wParam == (IntPtr)WM_LBUTTONDOWN) mb = MouseButton.Left;
                else if (wParam == (IntPtr)WM_RBUTTONDOWN) mb = MouseButton.Right;
                else if (wParam == (IntPtr)WM_MBUTTONDOWN) mb = MouseButton.Middle;
                else if (wParam == (IntPtr)WM_XBUTTONDOWN)
                {
                    int btn = (int)(ms.mouseData >> 16) & 0xFFFF;
                    mb = (btn == 1) ? MouseButton.X1 : MouseButton.X2;
                }

                if (mb.HasValue)
                {
                    if (mb == MouseButton.Left)
                    {
                        var r = _listeningBtn.RectangleToScreen(_listeningBtn.ClientRectangle);
                        if (r.Contains(ms.pt))
                            return CallNextHookEx(_msHook, nCode, wParam, lParam);
                    }
                    ApplyBinding(new KeyBinding(mb.Value));
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_msHook, nCode, wParam, lParam);
        }
    }

    public enum MouseButton { Left, Right, Middle, X1, X2 }

    public class KeyBinding
    {
        public bool IsMouseButton { get; }
        public Keys Key { get; }
        public MouseButton Mouse { get; }
        public string DisplayName { get; }

        public KeyBinding(Keys key)
        {
            IsMouseButton = false;
            Key = key;
            DisplayName = key.ToString();
        }
        public KeyBinding(MouseButton mb)
        {
            IsMouseButton = true;
            Mouse = mb;
            DisplayName = mb switch
            {
                MouseButton.Left => "Mouse Left",
                MouseButton.Right => "Mouse Right",
                MouseButton.Middle => "Mouse Middle",
                MouseButton.X1 => "Mouse Back",
                MouseButton.X2 => "Mouse Forward",
                _ => "Unknown"
            };
        }
    }
}