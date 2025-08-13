using System;
using System.Runtime.InteropServices;

namespace QuickSticky
{
    public class NoteModel
    {
        public string Content { get; set; } = "";
        public double Left { get; set; } = 100;
        public double Top { get; set; } = 100;
        public double Width { get; set; } = 300;
        public double Height { get; set; } = 220;

        public static NoteModel NewBlankAtCursor()
        {
            GetCursorPos(out var p);
            return new NoteModel
            {
                Left = p.X - 30,
                Top = p.Y - 30
            };
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }
    }
}