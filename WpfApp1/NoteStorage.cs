using System;
using System.IO;
using System.Text.Json;

namespace QuickSticky
{
    public static class NoteStorage
    {
        public static string GenerateNewPath(string notesDir)
        {
            var file = $"Note_{DateTime.Now:yyyyMMdd_HHmmssfff}.qnote";
            return Path.Combine(notesDir, file);
        }

        public static NoteModel Load(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<NoteModel>(json) ?? new NoteModel();
        }

        public static void Save(string path, NoteModel model)
        {
            var json = JsonSerializer.Serialize(model);
            File.WriteAllText(path, json);
        }

        public static void Delete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); } catch { }
        }
    }
}