using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Windows;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Application = System.Windows.Application;

namespace QuickSticky
{
    public partial class App : Application
    {
        public static string NotesDir { get; private set; } = null!;
        private static Mutex? _singleInstanceMutex;
        private const string MutexName = @"Global\QuickSticky_SingleInstance_v1";
        private static string PipeName => @"QuickStickyPipe_v1"; 

        private void Application_Startup(object? sender, StartupEventArgs e)
        {
            NotesDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "QuickSticky", "Notes");
            Directory.CreateDirectory(NotesDir);

            bool createdNew;
            try
            {
                _singleInstanceMutex = new Mutex(true, @"Global\QuickSticky_SingleInstance_v1", out createdNew);
            }
            catch (UnauthorizedAccessException)
            {
                _singleInstanceMutex = new Mutex(true, @"Local\QuickSticky_SingleInstance_v1", out createdNew);
            }
            catch (AbandonedMutexException) { createdNew = true; }

            var args = (e.Args ?? Array.Empty<string>()).ToArray();

            if (!createdNew)
            {
                if (args.Length >= 2 && args[0].Equals("/new-from-shell", StringComparison.OrdinalIgnoreCase))
                {
                    var shellPath = args[1].Trim('"');
                    try { Directory.CreateDirectory(Path.GetDirectoryName(shellPath)!); using (File.Create(shellPath)) { } } catch { }
                }
                SendToPrimary(args);
                Shutdown();
                return;
            }

            StartPipeServer();

            bool opened = HandleArgs(args);

            if (args.Length == 0 && !opened)
                Shutdown();
        }
        
        private bool HandleArgs(string[] args)
        {
            bool opened = false;

            if (args.Length >= 2 && args[0].Equals("/open", StringComparison.OrdinalIgnoreCase))
            {
                var file = args[1].Trim('"');
                if (File.Exists(file)) { OpenNote(NoteStorage.Load(file), file); opened = true; }
                return opened;
            }

            if (args.Length >= 2 && args[0].Equals("/new-from-shell", StringComparison.OrdinalIgnoreCase))
            {
                var shellPath = args[1].Trim('"');
                var finalPath = NoteStorage.GenerateNewPath(NotesDir);
                try { File.Move(shellPath, finalPath, true); } catch { /* ignore */ }
                CreateNote(finalPath, spawnAtCursor: true);
                return true;
            }

            if (args.Length >= 1 && args[0].Equals("/new", StringComparison.OrdinalIgnoreCase))
            {
                var path = NoteStorage.GenerateNewPath(NotesDir);
                CreateNote(path, spawnAtCursor: true);
                return true;
            }

            opened = LoadAllNotes() > 0;
            return opened;
        }

        private int LoadAllNotes()
        {
            int count = 0;
            foreach (var f in Directory.EnumerateFiles(NotesDir, "*.qnote"))
            {
                try { OpenNote(NoteStorage.Load(f), f); count++; } catch { }
            }
            return count;
        }

        private void CreateNote(string path, bool spawnAtCursor)
        {
            var model = spawnAtCursor ? NoteModel.NewBlankAtCursor() : new NoteModel();
            OpenNote(model, path);
        }

        private void OpenNote(NoteModel model, string path)
        {
            var w = new NoteWindow(model, path) { ShowInTaskbar = false };
            w.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _singleInstanceMutex?.ReleaseMutex();
            _singleInstanceMutex?.Dispose();
            _singleInstanceMutex = null;
            base.OnExit(e);
        }

        private static void SendToPrimary(string[] args)
        {
            try
            {
                using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                client.Connect(1500);
                using var w = new StreamWriter(client) { AutoFlush = true };
                string cmd = args.Length > 0 ? args[0] : "";
                string arg = args.Length > 1 ? args[1] : "";
                w.WriteLine($"{cmd}|{arg}");
            }
            catch
            {
                
            }
        }

        private void StartPipeServer()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                        await server.WaitForConnectionAsync().ConfigureAwait(false);
                        using var r = new StreamReader(server);
                        var line = await r.ReadLineAsync().ConfigureAwait(false);
                        if (string.IsNullOrEmpty(line)) continue;

                        var parts = line.Split(new[] { '|' }, 2);
                        var cmd = parts[0] ?? "";
                        var arg = parts.Length > 1 ? parts[1] : "";

                        Dispatcher.Invoke(() => HandleArgs(new[] { cmd, arg }));
                    }
                    catch
                    {
                        // keep serving
                    }
                }
            });
        }
    }
}
