using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DiaryApp
{
    class Event
    {
        public DateTime Start { get; set; }
        public TimeSpan Duration { get; set; }
        public string Place { get; set; }

        public override string ToString()
        {
            return $"{Start:dd.MM.yyyy HH:mm} - {Start.Add(Duration):HH:mm} ({Duration.TotalMinutes} хв) : {Place}";
        }
    }

    class Program
    {
        static List<Event> events = new List<Event>();
        static string filePath = "diary.json";

        static void Main()
        {
            LoadFromFile();

            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("=== ЩОДЕННИК ===");
                Console.WriteLine("1. Додати захід");
                Console.WriteLine("2. Переглянути всі заходи");
                Console.WriteLine("3. Переглянути заходи на день (завтра, післязавтра...)");
                Console.WriteLine("4. Нагадування про найближчий захід");
                Console.WriteLine("5. Робота з минулими заходами (вже відбулися)");
                Console.WriteLine("6. Аналіз накладок (перетинів)");
                Console.WriteLine("7. Редагувати захід");
                Console.WriteLine("8. Видалити захід");
                Console.WriteLine("9. Пошук заходів (з можливістю збереження)");
                Console.WriteLine("10. Про програму та гарячі клавіші");
                Console.WriteLine("0. Вихід та збереження");
                Console.Write("Виберіть пункт: ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": AddEvent(); break;
                    case "2": ViewAllEvents(); break;
                    case "3": ViewEventsByDayOffset(); break;
                    case "4": ShowNearestReminder(); break;
                    case "5": HandlePastEvents(); break;
                    case "6": ShowOverlaps(); break;
                    case "7": EditEvent(); break;
                    case "8": DeleteEvent(); break;
                    case "9": SearchEvents(); break;
                    case "10": ShowAbout(); break;
                    case "0": SaveToFile(); exit = true; break;
                    default: Console.WriteLine("Невірний вибір. Натисніть будь-яку клавішу..."); Console.ReadKey(); break;
                }
            }
        }

        static void AddEvent()
        {
            Console.Clear();
            Console.WriteLine("=== Додавання заходу (введіть 0 на будь-якому кроці для скасування) ===");

            DateTime start = ReadDate("Введіть дату (дд.мм.рррр) або 0 для виходу: ", allowExit: true);
            if (start == DateTime.MinValue)
            {
                Console.WriteLine("Додавання скасовано.");
                Console.ReadKey();
                return;
            }

            DateTime time = ReadTime("Введіть час (гг:хх): ", allowExit: true);
            if (time == DateTime.MinValue)
            {
                Console.WriteLine("Додавання скасовано.");
                Console.ReadKey();
                return;
            }
            start = start.Date + time.TimeOfDay;

            int minutes = ReadPositiveInt("Введіть тривалість (хвилини): ", allowExit: true);
            if (minutes == 0)
            {
                Console.WriteLine("Додавання скасовано.");
                Console.ReadKey();
                return;
            }

            string place = ReadString("Введіть місце проведення: ", allowExit: true);
            if (place == null)
            {
                Console.WriteLine("Додавання скасовано.");
                Console.ReadKey();
                return;
            }

            events.Add(new Event { Start = start, Duration = TimeSpan.FromMinutes(minutes), Place = place });
            SaveToFile();
            Console.WriteLine("Захід додано. Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ViewAllEvents()
        {
            Console.Clear();
            Console.WriteLine("=== Всі заходи ===");
            if (events.Count == 0) Console.WriteLine("Список заходів порожній.");
            else
            {
                events.Sort((a, b) => a.Start.CompareTo(b.Start));
                for (int i = 0; i < events.Count; i++)
                    Console.WriteLine($"{i + 1}. {events[i]}");
            }
            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ViewEventsByDayOffset()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Перегляд заходів на день (-1 – вихід) ===");
                Console.Write("Введіть зміщення днів (0 – сьогодні, 1 – завтра, ...): ");
                string input = Console.ReadLine();
                if (input == "-1") break;

                if (!int.TryParse(input, out int offset) || offset < 0)
                {
                    Console.WriteLine("Введіть невід'ємне ціле число.");
                    Console.ReadKey();
                    continue;
                }

                DateTime targetDate = DateTime.Today.AddDays(offset);
                var filtered = events.FindAll(e => e.Start.Date == targetDate);
                filtered.Sort((a, b) => a.Start.CompareTo(b.Start));

                Console.WriteLine($"Заходи на {targetDate:dd.MM.yyyy}:");
                if (filtered.Count == 0) Console.WriteLine("Немає заходів.");
                else foreach (var ev in filtered) Console.WriteLine(ev);

                Console.WriteLine("Натисніть будь-яку клавішу для продовження...");
                Console.ReadKey();
            }
        }

        static void ShowNearestReminder()
        {
            Console.Clear();
            Console.WriteLine("=== Нагадування про найближчий захід ===");
            DateTime now = DateTime.Now;
            Event nearest = null;
            foreach (var ev in events)
                if (ev.Start >= now && (nearest == null || ev.Start < nearest.Start))
                    nearest = ev;

            Console.WriteLine(nearest == null ? "Найближчих заходів немає." : $"Найближчий захід: {nearest}");
            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void HandlePastEvents()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Робота з минулими заходами (0 – вихід) ===");
                DateTime now = DateTime.Now;
                var pastEvents = events.FindAll(e => e.Start < now);

                if (pastEvents.Count == 0)
                {
                    Console.WriteLine("Немає заходів, які вже відбулися.");
                    Console.WriteLine("Натисніть будь-яку клавішу...");
                    Console.ReadKey();
                    break;
                }

                Console.WriteLine("Минулі заходи:");
                for (int i = 0; i < pastEvents.Count; i++)
                    Console.WriteLine($"{i + 1}. {pastEvents[i]}");

                Console.WriteLine("\nВиберіть дію:");
                Console.WriteLine("1. Видалити конкретний захід");
                Console.WriteLine("2. Видалити всі минулі заходи");
                Console.WriteLine("3. Перенести конкретний захід на іншу дату");
                Console.WriteLine("4. Перенести всі минулі заходи на іншу дату");
                Console.WriteLine("0. Повернутися до головного меню");
                Console.Write("Ваш вибір: ");
                string action = Console.ReadLine();
                if (action == "0") break;

                if (action == "1" || action == "3")
                {
                    Console.Write("Введіть номер заходу (0 – вихід): ");
                    if (!int.TryParse(Console.ReadLine(), out int idx))
                    {
                        Console.WriteLine("Невірний номер.");
                        Console.ReadKey();
                        continue;
                    }
                    if (idx == 0) break;

                    if (idx < 1 || idx > pastEvents.Count)
                    {
                        Console.WriteLine("Невірний номер.");
                        Console.ReadKey();
                        continue;
                    }
                    Event ev = pastEvents[idx - 1];

                    if (action == "1")
                    {
                        events.Remove(ev);
                        SaveToFile();
                        Console.WriteLine("Захід видалено.");
                    }
                    else
                    {
                        DateTime newDate = ReadDate("Введіть нову дату (дд.мм.рррр) або 0 для виходу: ", allowExit: true);
                        if (newDate == DateTime.MinValue) break;
                        ev.Start = newDate.Date + ev.Start.TimeOfDay;
                        SaveToFile();
                        Console.WriteLine("Захід перенесено.");
                    }
                }
                else if (action == "2")
                {
                    foreach (var ev in pastEvents) events.Remove(ev);
                    SaveToFile();
                    Console.WriteLine("Всі минулі заходи видалено.");
                }
                else if (action == "4")
                {
                    DateTime newDate = ReadDate("Введіть нову дату (дд.мм.рррр) або 0 для виходу: ", allowExit: true);
                    if (newDate == DateTime.MinValue) break;
                    foreach (var ev in pastEvents) ev.Start = newDate.Date + ev.Start.TimeOfDay;
                    SaveToFile();
                    Console.WriteLine("Всі заходи перенесено.");
                }
                else Console.WriteLine("Невірна дія.");

                Console.WriteLine("Натисніть будь-яку клавішу...");
                Console.ReadKey();
            }
        }

        static void ShowOverlaps()
        {
            Console.Clear();
            Console.WriteLine("=== Аналіз накладок (перетинів) ===");
            events.Sort((a, b) => a.Start.CompareTo(b.Start));
            bool overlapFound = false;
            for (int i = 0; i < events.Count; i++)
                for (int j = i + 1; j < events.Count; j++)
                {
                    var a = events[i];
                    var b = events[j];
                    DateTime aEnd = a.Start.Add(a.Duration);
                    DateTime bEnd = b.Start.Add(b.Duration);
                    if (a.Start < bEnd && b.Start < aEnd)
                    {
                        if (!overlapFound)
                        {
                            Console.WriteLine("Знайдені перетини:");
                            overlapFound = true;
                        }
                        Console.WriteLine($"- {a} перетинається з {b}");
                    }
                }
            if (!overlapFound) Console.WriteLine("Накладок не знайдено.");
            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void EditEvent()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Редагування заходу (0 – вихід) ===");
                if (events.Count == 0)
                {
                    Console.WriteLine("Список заходів порожній.");
                    Console.ReadKey();
                    return;
                }

                events.Sort((a, b) => a.Start.CompareTo(b.Start));
                for (int i = 0; i < events.Count; i++)
                    Console.WriteLine($"{i + 1}. {events[i]}");

                Console.Write("Введіть номер заходу для редагування: ");
                string input = Console.ReadLine();
                if (input == "0")
                {
                    Console.WriteLine("Вихід з редагування.");
                    Console.ReadKey();
                    return;
                }

                if (!int.TryParse(input, out int index) || index < 1 || index > events.Count)
                {
                    Console.WriteLine("Неправильний формат або номер поза діапазоном.");
                    Console.ReadKey();
                    continue;
                }

                Event ev = events[index - 1];
                Console.WriteLine($"Редагуємо: {ev}");

                while (true)
                {
                    Console.WriteLine("\nЩо бажаєте змінити?");
                    Console.WriteLine("1. Дату");
                    Console.WriteLine("2. Час");
                    Console.WriteLine("3. Тривалість");
                    Console.WriteLine("4. Місце");
                    Console.WriteLine("5. Вибрати інший захід");
                    Console.Write("Ваш вибір: ");
                    string choice = Console.ReadLine();

                    if (choice == "5")
                    {
                        Console.WriteLine("Зміни скасовано. Повернення до списку.");
                        Console.ReadKey();
                        break;
                    }

                    switch (choice)
                    {
                        case "1":
                            DateTime newDate = ReadDate("Введіть нову дату (дд.мм.рррр) або 0 для виходу без змін: ", allowExit: true);
                            if (newDate == DateTime.MinValue) break;
                            ev.Start = newDate.Date + ev.Start.TimeOfDay;
                            SaveToFile();
                            Console.WriteLine("Дату змінено.");
                            break;
                        case "2":
                            DateTime newTime = ReadTime("Введіть новий час (гг:хх) або 0 для виходу без змін: ", allowExit: true);
                            if (newTime == DateTime.MinValue) break;
                            ev.Start = ev.Start.Date + newTime.TimeOfDay;
                            SaveToFile();
                            Console.WriteLine("Час змінено.");
                            break;
                        case "3":
                            int newDur = ReadPositiveInt("Введіть нову тривалість (хвилини) або 0 для виходу без змін: ", allowExit: true);
                            if (newDur == 0) break;
                            ev.Duration = TimeSpan.FromMinutes(newDur);
                            SaveToFile();
                            Console.WriteLine("Тривалість змінено.");
                            break;
                        case "4":
                            string newPlace = ReadString("Введіть нове місце (або 0 для виходу без змін): ", allowExit: true);
                            if (newPlace == null) break;
                            ev.Place = newPlace;
                            SaveToFile();
                            Console.WriteLine("Місце змінено.");
                            break;
                        default:
                            Console.WriteLine("Невірний вибір.");
                            continue;
                    }
                    Console.WriteLine("Продовжуйте редагування або виберіть 0 або 5 для виходу.");
                }
            }
        }

        static void DeleteEvent()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Видалення заходу (0 – вихід) ===");
                if (events.Count == 0)
                {
                    Console.WriteLine("Список заходів порожній.");
                    Console.ReadKey();
                    break;
                }

                events.Sort((a, b) => a.Start.CompareTo(b.Start));
                for (int i = 0; i < events.Count; i++)
                    Console.WriteLine($"{i + 1}. {events[i]}");

                Console.Write("\nВведіть номер заходу для видалення: ");
                if (!int.TryParse(Console.ReadLine(), out int index) || index == 0) break;
                if (index < 1 || index > events.Count)
                {
                    Console.WriteLine("Невірний номер.");
                    Console.ReadKey();
                    continue;
                }

                events.RemoveAt(index - 1);
                SaveToFile();
                Console.WriteLine("Захід видалено.");
                Console.ReadKey();
            }
        }

        static void SearchEvents()
        {
            Console.Clear();
            Console.WriteLine("=== Пошук заходів ===");
            Console.WriteLine("Введіть критерії пошуку (Enter – пропустити критерій, 0 – вихід).\n");

            DateTime? dateFrom = null;
            bool exit = false;
            while (true)
            {
                Console.Write("Дата (від) [дд.мм.рррр] або Enter: ");
                string input = Console.ReadLine();
                if (input == "0")
                {
                    exit = true;
                    break;
                }
                if (string.IsNullOrWhiteSpace(input))
                {
                    dateFrom = null;
                    break;
                }
                if (DateTime.TryParseExact(input, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime d))
                {
                    dateFrom = d;
                    break;
                }
                Console.WriteLine("Помилка! Введіть дату у форматі дд.мм.рррр або натисніть Enter.");
            }
            if (exit)
            {
                Console.WriteLine("Вихід. Натисніть будь-яку клавішу...");
                Console.ReadKey();
                return;
            }

            DateTime? dateTo = null;
            while (true)
            {
                Console.Write("Дата (до) [дд.мм.рррр] або Enter: ");
                string input = Console.ReadLine();
                if (input == "0")
                {
                    exit = true;
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    dateTo = null;
                    break;
                }
                if (DateTime.TryParseExact(input, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime d))
                {
                    dateTo = d;
                    break;
                }
                Console.WriteLine("Помилка! Введіть дату у форматі дд.мм.рррр або натисніть Enter.");
            }
            if (exit)
            {
                Console.WriteLine("Вихід. Натисніть будь-яку клавішу...");
                Console.ReadKey();
                return;
            }

            TimeSpan? timeFrom = null;
            while (true)
            {
                Console.Write("Час (від) [гг:хх] або Enter: ");
                string input = Console.ReadLine();
                if (input == "0")
                {
                    exit = true;
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    timeFrom = null;
                    break;
                }
                if (DateTime.TryParseExact(input, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime t))
                {
                    timeFrom = t.TimeOfDay;
                    break;
                }
                Console.WriteLine("Помилка! Введіть час у форматі гг:хх або натисніть Enter.");
            }
            if (exit)
            {
                Console.WriteLine("Вихід. Натисніть будь-яку клавішу...");
                Console.ReadKey();
                return;
            }

            TimeSpan? timeTo = null;
            while (true)
            {
                Console.Write("Час (до) [гг:хх] або Enter: ");
                string input = Console.ReadLine();
                if (input == "0")
                {
                    exit = true;
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    timeTo = null;
                    break;
                }
                if (DateTime.TryParseExact(input, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime t))
                {
                    timeTo = t.TimeOfDay;
                    break;
                }
                Console.WriteLine("Помилка! Введіть час у форматі гг:хх або натисніть Enter.");
            }
            if (exit)
            {
                Console.WriteLine("Вихід. Натисніть будь-яку клавішу...");
                Console.ReadKey();
                return;
            }

            int? durFrom = null;
            while (true)
            {
                Console.Write("Тривалість (від) [хвилини] або Enter: ");
                string input = Console.ReadLine();
                if (input == "0")
                {
                    exit = true;
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    durFrom = null;
                    break;
                }
                if (int.TryParse(input, out int d) && d >= 0)
                {
                    durFrom = d;
                    break;
                }
                Console.WriteLine("Помилка! Введіть ціле невід'ємне число або натисніть Enter.");
            }
            if (exit)
            {
                Console.WriteLine("Вихід. Натисніть будь-яку клавішу...");
                Console.ReadKey();
                return;
            }

            int? durTo = null;
            while (true)
            {
                Console.Write("Тривалість (до) [хвилини] або Enter: ");
                string input = Console.ReadLine();
                if (input == "0")
                {
                    exit = true;
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    durTo = null;
                    break;
                }
                if (int.TryParse(input, out int d) && d >= 0)
                {
                    durTo = d;
                    break;
                }
                Console.WriteLine("Помилка! Введіть ціле невід'ємне число або натисніть Enter.");
            }
            if (exit)
            {
                Console.WriteLine("Вихід. Натисніть будь-яку клавішу...");
                Console.ReadKey();
                return;
            }

            string placeSubstring = null;
            while (true)
            {
                Console.Write("Місце (частина назви) або Enter: ");
                string input = Console.ReadLine();
                if (input == "0")
                {
                    exit = true;
                    break;
                }

                if (string.IsNullOrWhiteSpace(input))
                {
                    placeSubstring = null;
                    break;
                }
                placeSubstring = input.Trim();
                break;
            }
            if (exit)
            {
                Console.WriteLine("Вихід. Натисніть будь-яку клавішу...");
                Console.ReadKey();
                return;
            }

            var results = new List<Event>();
            foreach (var ev in events)
            {
                bool match = true;
                if (dateFrom.HasValue && ev.Start.Date < dateFrom.Value) match = false;
                if (dateTo.HasValue && ev.Start.Date > dateTo.Value) match = false;
                if (timeFrom.HasValue && ev.Start.TimeOfDay < timeFrom.Value) match = false;
                if (timeTo.HasValue && ev.Start.TimeOfDay > timeTo.Value) match = false;
                if (durFrom.HasValue && ev.Duration.TotalMinutes < durFrom.Value) match = false;
                if (durTo.HasValue && ev.Duration.TotalMinutes > durTo.Value) match = false;
                if (!string.IsNullOrEmpty(placeSubstring) && !ev.Place.Contains(placeSubstring, StringComparison.OrdinalIgnoreCase))
                    match = false;
                if (match) results.Add(ev);
            }

            Console.Clear();
            Console.WriteLine("=== Результати пошуку ===");
            if (results.Count == 0)
            {
                Console.WriteLine("Заходів, що відповідають критеріям, не знайдено.");
            }
            else
            {
                results.Sort((a, b) => a.Start.CompareTo(b.Start));
                for (int i = 0; i < results.Count; i++)
                    Console.WriteLine($"{i + 1}. {results[i]}");
            }

            Console.Write("\nЗберегти результати у файл? (т/н): ");
            while (true)
            {
                string answer = Console.ReadLine()?.Trim().ToLower();
                if (answer != "н" && answer != "т")
                {
                    Console.Write("Уведіть \"т\" або \"н\" ");
                    continue;
                }

                if (answer == "т")
                {
                    Console.Write("Введіть повний шлях до файлу (напр., C:\\Diary): ");
                    string path = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        try
                        {
                            using (StreamWriter sw = new StreamWriter(path + "\\result.txt", false))
                            {
                                sw.WriteLine("=== Результати пошуку ===");
                                sw.WriteLine($"Дата пошуку: {DateTime.Now:dd.MM.yyyy HH:mm}");
                                sw.WriteLine();
                                if (results.Count == 0)
                                    sw.WriteLine("Заходів не знайдено.");
                                else
                                    foreach (var ev in results)
                                        sw.WriteLine(ev);
                            }
                            Console.WriteLine($"Результати збережено у файл: {path}");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Помилка при збереженні: {ex.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Шлях не вказано – збереження скасовано.");
                    }
                }
                else
                {
                    Console.WriteLine("Збереження скасовано.");
                    break;
                }
            }

            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ShowAbout()
        {
            Console.Clear();
            Console.WriteLine("=== Про програму ===");
            Console.WriteLine("Щоденник - програма для планування заходів.");
            Console.WriteLine("Версія 1.0");
            Console.WriteLine("Автор: Пирогов Руслан - студент групи ПЗПІ-25-4");
            Console.WriteLine("\nГарячі клавіші в головному меню:");
            Console.WriteLine("1 - Додати захід");
            Console.WriteLine("2 - Переглянути всі заходи");
            Console.WriteLine("3 - Переглянути заходи на день");
            Console.WriteLine("4 - Нагадування про найближчий захід");
            Console.WriteLine("5 - Робота з минулими заходами");
            Console.WriteLine("6 - Аналіз накладок");
            Console.WriteLine("7 - Редагувати захід");
            Console.WriteLine("8 - Видалити захід");
            Console.WriteLine("9 - Пошук заходів (з можливістю збереження)");
            Console.WriteLine("10 - Ця довідка");
            Console.WriteLine("0 - Вихід та збереження");
            Console.WriteLine("\nНатисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static DateTime ReadDate(string prompt, bool allowExit = false)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine();
                if (allowExit && (input == "0")) return DateTime.MinValue;
                if (DateTime.TryParseExact(input, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime date))
                    return date;
                Console.WriteLine("Помилка! Введіть дату у форматі дд.мм.рррр (наприклад, 31.12.2025).");
            }
        }

        static DateTime ReadTime(string prompt, bool allowExit = false)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine();
                if (allowExit && (input == "0")) return DateTime.MinValue;
                if (DateTime.TryParseExact(input, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime time))
                    return time;
                Console.WriteLine("Помилка! Введіть час у форматі гг:хх (наприклад, 14:30).");
            }
        }

        static int ReadPositiveInt(string prompt, bool allowExit = false)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine();
                if (allowExit && (input == "0")) return 0;
                if (int.TryParse(input, out int value) && value > 0) return value;
                Console.WriteLine("Помилка! Введіть ціле додатнє число.");
            }
        }

        static string ReadString(string prompt, bool allowExit = false)
        {
            while (true)
            {
                Console.Write(prompt);
                string input = Console.ReadLine();
                if (allowExit && (input == "0")) return null;
                if (!string.IsNullOrWhiteSpace(input)) return input.Trim();
                Console.WriteLine("Поле не може бути порожнім.");
            }
        }

        static void SaveToFile()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(events, options);
                File.WriteAllText(filePath, json);
            }
            catch { }
        }

        static void LoadFromFile()
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    events = JsonSerializer.Deserialize<List<Event>>(json) ?? new List<Event>();
                }
                catch { events = new List<Event>(); }
            }
            else events = new List<Event>();
        }
    }
}