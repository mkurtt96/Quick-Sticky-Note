using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace QuickSticky
{
    public partial class NoteWindow : Window
    {
        private readonly string _path;
        private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromMilliseconds(500) };
        private bool _dirty;
        private NoteModel _model;

        private int _closeClicks = 0;
        private DateTime _firstClickTime;
        private const int RequiredClicks = 3;
        private static readonly TimeSpan ClickWindow = TimeSpan.FromSeconds(1.5);

        public NoteWindow(NoteModel model, string path)
        {
            InitializeComponent();
            _model = model;
            _path = path;

            Left = _model.Left;
            Top = _model.Top;
            Width = _model.Width > 120 ? _model.Width : 300;
            Height = _model.Height > 60 ? _model.Height : 220;

            Editor.Text = _model.Content;
            Title = Path.GetFileNameWithoutExtension(_path);

            _saveTimer.Tick += (_, __) => { if (_dirty) SaveNow(); };
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) => Editor.Focus();

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void Editor_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            _model.Content = Editor.Text;
            _dirty = true;
            if (!_saveTimer.IsEnabled) _saveTimer.Start();
        }

        private void Window_LocationOrSizeChanged(object? sender, EventArgs e)
        {
            _model.Left = Left; _model.Top = Top; _model.Width = Width; _model.Height = Height;
            _dirty = true;
            if (!_saveTimer.IsEnabled) _saveTimer.Start();
        }

        private void SaveNow()
        {
            try
            {
                NoteStorage.Save(_path, _model);
                _dirty = false;
                _saveTimer.Stop();
            }
            catch { }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            var now = DateTime.UtcNow;
            if (_closeClicks == 0) _firstClickTime = now;

            _closeClicks++;
            if (now - _firstClickTime > ClickWindow)
            {
                _closeClicks = 1;
                _firstClickTime = now;
            }

            if (_closeClicks >= RequiredClicks)
            {
                NoteStorage.Delete(_path);
                Close();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                SaveNow(); e.Handled = true;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (_closeClicks < RequiredClicks) SaveNow();
            base.OnClosing(e);
        }
    }
}
