using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Diary
{
    public class FileStorage
    {
        private readonly string _filePath;

        public FileStorage(string filePath)
        {
            _filePath = filePath;
        }

        public void Save(IEnumerable<Event> events)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(events, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка збереження: {ex.Message}");
            }
        }

        public List<Event> Load()
        {
            if (!File.Exists(_filePath))
                return new List<Event>();

            try
            {
                string json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<Event>>(json) ?? new List<Event>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка завантаження: {ex.Message}");
                return new List<Event>();
            }
        }
    }
}
