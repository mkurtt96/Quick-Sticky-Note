using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace QuickSticky
{
    public partial class NoteWindow : Window
    {
        private const string DefaultTitlePlaceholder = "Quick Note";

        private readonly string _path;

        private readonly DispatcherTimer _saveTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        
        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;

        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_APPWINDOW = 0x00040000;

        private NoteModel _model;
        private bool _dirty;
        private bool _isLoading;
        private bool _isTitleEditing;

        private int _closeClicks;
        private DateTime _firstClickTime;

        public NoteWindow(NoteModel model, string path)
        {
            InitializeComponent();

            _isLoading = true;

            _model = model;
            _path = path;

            Left = _model.Left;
            Top = _model.Top;

            Width = _model.Width > NoteWindowSettings.MinValidWidth
                ? _model.Width
                : NoteWindowSettings.DefaultWidth;

            Height = _model.Height > NoteWindowSettings.MinValidHeight
                ? _model.Height
                : NoteWindowSettings.DefaultHeight;

            TitleEditor.Text = _model.Title ?? "";
            Editor.Text = _model.Content ?? "";

            UpdateWindowTitle();
            UpdateTitlePlaceholder();

            _isLoading = false;

            _saveTimer.Tick += (_, _) =>
            {
                if (_dirty)
                    SaveNow();
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            WindowEffects.Apply(this);

            var hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd)?.AddHook(WndProc);

            int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            exStyle |= WS_EX_TOOLWINDOW;
            exStyle &= ~WS_EX_APPWINDOW;

            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WindowEffects.Apply(this);
            Editor.Focus();
        }

        private const int WM_NCHITTEST = 0x0084;

        private const int HTLEFT = 10;
        private const int HTRIGHT = 11;
        private const int HTTOP = 12;
        private const int HTTOPLEFT = 13;
        private const int HTTOPRIGHT = 14;
        private const int HTBOTTOM = 15;
        private const int HTBOTTOMLEFT = 16;
        private const int HTBOTTOMRIGHT = 17;

        private IntPtr WndProc(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled)
        {
            if (msg != WM_NCHITTEST)
                return IntPtr.Zero;

            int x = unchecked((short)(long)lParam);
            int y = unchecked((short)((long)lParam >> 16));

            Point pos = PointFromScreen(new Point(x, y));

            double border = NoteWindowSettings.ResizeBorderThickness;

            bool left = pos.X <= border;
            bool right = pos.X >= ActualWidth - border;
            bool top = pos.Y <= border;
            bool bottom = pos.Y >= ActualHeight - border;

            handled = true;

            if (top && left) return new IntPtr(HTTOPLEFT);
            if (top && right) return new IntPtr(HTTOPRIGHT);
            if (bottom && left) return new IntPtr(HTBOTTOMLEFT);
            if (bottom && right) return new IntPtr(HTBOTTOMRIGHT);
            if (left) return new IntPtr(HTLEFT);
            if (right) return new IntPtr(HTRIGHT);
            if (top) return new IntPtr(HTTOP);
            if (bottom) return new IntPtr(HTBOTTOM);

            handled = false;
            return IntPtr.Zero;
        }

        private void TitleEditor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount < 2)
            {
                Window_MouseLeftButtonDown(sender, e);
                return;
            }

            _isTitleEditing = true;

            TitleEditor.IsReadOnly = false;
            TitleEditor.Focus();
            TitleEditor.SelectAll();

            e.Handled = true;
        }
        
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isTitleEditing)
                return;

            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void TitleEditor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isLoading)
                return;

            _model.Title = TitleEditor.Text.Trim();
            UpdateWindowTitle();
            UpdateTitlePlaceholder();
            MarkDirty();
        }
        
        private void TitleEditor_LostFocus(object sender, RoutedEventArgs e)
        {
            EndTitleEditing();
        }

        private void EndTitleEditing()
        {
            _isTitleEditing = false;
            TitleEditor.IsReadOnly = true;
        }

        private void Editor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isLoading)
                return;

            _model.Content = Editor.Text;
            MarkDirty();
        }

        private void UpdateWindowTitle()
        {
            Title = string.IsNullOrWhiteSpace(_model.Title)
                ? DefaultTitlePlaceholder
                : _model.Title;
        }

        private void UpdateTitlePlaceholder()
        {
            TitlePlaceholder.Visibility = string.IsNullOrWhiteSpace(TitleEditor.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void Window_LocationOrSizeChanged(object? sender, EventArgs e)
        {
            if (_isLoading)
                return;

            _model.Left = Left;
            _model.Top = Top;
            _model.Width = Width;
            _model.Height = Height;

            MarkDirty();
        }

        private void MarkDirty()
        {
            _dirty = true;
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private void SaveNow()
        {
            try
            {
                NoteStorage.Save(_path, _model);
                _dirty = false;
                _saveTimer.Stop();
            }
            catch
            {
            }
        }

        private void SettingsMenuItem_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                this,
                "Settings will be added here later.",
                "Note Settings",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.UtcNow;

            if (_closeClicks == 0)
                _firstClickTime = now;

            _closeClicks++;

            if (now - _firstClickTime > NoteWindowSettings.CloseClickWindow)
            {
                _closeClicks = 1;
                _firstClickTime = now;
            }

            if (_closeClicks >= NoteWindowSettings.RequiredCloseClicks)
            {
                NoteStorage.Delete(_path);
                Close();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S &&
                Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                SaveNow();
                e.Handled = true;
            }
        }

        protected override void OnClosing( System.ComponentModel.CancelEventArgs e)
        {
            if (_closeClicks < NoteWindowSettings.RequiredCloseClicks)
                SaveNow();

            base.OnClosing(e);
        }
    }
}