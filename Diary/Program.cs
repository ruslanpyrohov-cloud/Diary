using Diary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Diary
{
    class Program
    {
        private static DiaryManager _manager;
        private static FileStorage _storage;
        private const string FilePath = "diary.json";

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            _storage = new FileStorage(FilePath);
            var events = _storage.Load();
            _manager = new DiaryManager(events);

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
                    case "0": _storage.Save(_manager.Events); exit = true; break;
                    default: Console.WriteLine("Невірний вибір. Натисніть будь-яку клавішу..."); Console.ReadKey(); break;
                }
            }
        }

        static void AddEvent()
        {
            Console.Clear();
            Console.WriteLine("=== Додавання заходу (введіть 0 на будь-якому кроці для скасування) ===");

            DateTime start = ReadDate("Введіть дату (дд.мм.рррр): ", allowExit: true);
            if (start == DateTime.MinValue)
            {
                Console.WriteLine("Додавання скасовано. Повернення до головного меню...");
                Console.ReadKey();
                return;
            }

            DateTime time = ReadTime("Введіть час (гг:хх): ", allowExit: true);
            if (time == DateTime.MinValue)
            {
                Console.WriteLine("Додавання скасовано. Повернення до головного меню...");
                Console.ReadKey();
                return;
            }
            start = start.Date + time.TimeOfDay;

            int minutes = ReadPositiveInt("Введіть тривалість (хвилини): ", allowExit: true);
            if (minutes == 0)
            {
                Console.WriteLine("Додавання скасовано. Повернення до головного меню...");
                Console.ReadKey();
                return;
            }

            string place = ReadString("Введіть місце проведення: ", allowExit: true);
            if (place == null)
            {
                Console.WriteLine("Додавання скасовано. Повернення до головного меню...");
                Console.ReadKey();
                return;
            }

            _manager.AddEvent(new Event { Start = start, Duration = TimeSpan.FromMinutes(minutes), Place = place });
            _storage.Save(_manager.Events);
            Console.WriteLine("Захід додано. Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void ViewAllEvents()
        {
            Console.Clear();
            Console.WriteLine("=== Всі заходи ===");
            var events = _manager.Events;
            if (events.Count == 0) Console.WriteLine("Список заходів порожній.");
            else
            {
                _manager.SortEvents();
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
                if (input == "-1")
                {
                    Console.WriteLine("Повернення до головного меню...");
                    Console.ReadKey();
                    break;
                }

                if (!int.TryParse(input, out int offset) || offset < 0)
                {
                    Console.WriteLine("Введіть невід'ємне ціле число.");
                    Console.ReadKey();
                    continue;
                }

                DateTime targetDate = DateTime.Today.AddDays(offset);
                var filtered = _manager.GetEventsByDate(targetDate);
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
            var nearest = _manager.GetNearest();
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
                var pastEvents = _manager.GetPastEvents();

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
                if (action == "0")
                {
                    Console.WriteLine("Повернення до головного меню...");
                    Console.ReadKey();
                    break;
                }

                if (action == "1" || action == "3")
                {
                    while (true)
                    {
                        Console.Write("Введіть номер заходу (0 для виходу до попереднього меню): ");
                        if (!int.TryParse(Console.ReadLine(), out int idx))
                        {
                            Console.WriteLine("Помилка! Введіть ціле число.");
                            continue;
                        }
                        if (idx == 0)
                        {
                            Console.WriteLine("Повернення до списку дій...");
                            Console.ReadKey();
                            break;
                        }
                        if (idx < 1 || idx > pastEvents.Count)
                        {
                            Console.WriteLine($"Помилка! Введіть число від 1 до {pastEvents.Count}.");
                            continue;
                        }
                        Event ev = pastEvents[idx - 1];

                        if (action == "1")
                        {
                            _manager.RemoveEvent(ev);
                            _storage.Save(_manager.Events);
                            Console.WriteLine("Захід видалено.");
                        }
                        else
                        {
                            DateTime newDate = ReadDate("Введіть нову дату (дд.мм.рррр): ", allowExit: true);
                            if (newDate == DateTime.MinValue)
                            {
                                Console.WriteLine("Операцію скасовано.");
                                Console.ReadKey();
                                break;
                            }
                            ev.Start = newDate.Date + ev.Start.TimeOfDay;
                            _storage.Save(_manager.Events);
                            Console.WriteLine("Захід перенесено.");
                        }
                        Console.WriteLine("Натисніть будь-яку клавішу...");
                        Console.ReadKey();
                        break;
                    }
                }
                else if (action == "2")
                {
                    _manager.DeletePastEvents();
                    _storage.Save(_manager.Events);
                    Console.WriteLine("Всі минулі заходи видалено.");
                    Console.WriteLine("Натисніть будь-яку клавішу...");
                    Console.ReadKey();
                }
                else if (action == "4")
                {
                    DateTime newDate = ReadDate("Введіть нову дату (дд.мм.рррр): ", allowExit: true);
                    if (newDate == DateTime.MinValue)
                    {
                        Console.WriteLine("Операцію скасовано.");
                        Console.ReadKey();
                        continue;
                    }
                    _manager.MovePastEvents(newDate);
                    _storage.Save(_manager.Events);
                    Console.WriteLine("Всі заходи перенесено.");
                    Console.WriteLine("Натисніть будь-яку клавішу...");
                    Console.ReadKey();
                }
                else
                {
                    Console.WriteLine("Невірна дія.");
                    Console.ReadKey();
                }
            }
        }

        static void ShowOverlaps()
        {
            Console.Clear();
            Console.WriteLine("=== Аналіз накладок (перетинів) ===");
            var overlaps = _manager.FindOverlaps();
            if (overlaps.Count == 0)
                Console.WriteLine("Накладок не знайдено.");
            else
            {
                Console.WriteLine("Знайдені перетини:");
                foreach (var ev in overlaps)
                    Console.WriteLine($"- {ev}");
            }
            Console.WriteLine("Натисніть будь-яку клавішу...");
            Console.ReadKey();
        }

        static void EditEvent()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Редагування заходу (0 – вихід) ===");
                var events = _manager.Events;
                if (events.Count == 0)
                {
                    Console.WriteLine("Список заходів порожній.");
                    Console.ReadKey();
                    return;
                }

                _manager.SortEvents();
                for (int i = 0; i < events.Count; i++)
                    Console.WriteLine($"{i + 1}. {events[i]}");

                Console.Write("Введіть номер заходу для редагування: ");
                string input = Console.ReadLine();
                if (input == "0")
                {
                    Console.WriteLine("Вихід з редагування...");
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
                    Console.WriteLine("0. Вийти");
                    Console.Write("Ваш вибір: ");
                    string choice = Console.ReadLine();

                    if (choice == "5")
                    {
                        Console.WriteLine("Зміни скасовано. Повернення до списку.");
                        Console.ReadKey();
                        break;
                    }

                    if (choice == "0")
                    {
                        Console.WriteLine("Повернення до головного меню...");
                        Console.ReadKey();
                        return;
                    }

                    switch (choice)
                    {
                        case "1":
                            DateTime newDate = ReadDate("Введіть нову дату (дд.мм.рррр) або 0 для виходу без змін: ", allowExit: true);
                            if (newDate == DateTime.MinValue) break;
                            ev.Start = newDate.Date + ev.Start.TimeOfDay;
                            _storage.Save(_manager.Events);
                            Console.WriteLine("Дату змінено.");
                            break;
                        case "2":
                            DateTime newTime = ReadTime("Введіть новий час (гг:хх) або 0 для виходу без змін: ", allowExit: true);
                            if (newTime == DateTime.MinValue) break;
                            ev.Start = ev.Start.Date + newTime.TimeOfDay;
                            _storage.Save(_manager.Events);
                            Console.WriteLine("Час змінено.");
                            break;
                        case "3":
                            int newDur = ReadPositiveInt("Введіть нову тривалість (хвилини) або 0 для виходу без змін: ", allowExit: true);
                            if (newDur == 0) break;
                            ev.Duration = TimeSpan.FromMinutes(newDur);
                            _storage.Save(_manager.Events);
                            Console.WriteLine("Тривалість змінено.");
                            break;
                        case "4":
                            string newPlace = ReadString("Введіть нове місце (або 0 для виходу без змін): ", allowExit: true);
                            if (newPlace == null) break;
                            ev.Place = newPlace;
                            _storage.Save(_manager.Events);
                            Console.WriteLine("Місце змінено.");
                            break;
                        default:
                            Console.WriteLine("Невірний вибір.");
                            continue;
                    }
                    Console.WriteLine("Продовжуйте редагування або натисніть 5 для вибору іншої події чи 0 для виходу.");
                }
            }
        }

        static void DeleteEvent()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== Видалення заходу (0 – вихід) ===");

                var events = _manager.Events;
                if (events.Count == 0)
                {
                    Console.WriteLine("Список заходів порожній.");
                    Console.ReadKey();
                    return;
                }

                _manager.SortEvents();
                for (int i = 0; i < events.Count; i++)
                    Console.WriteLine($"{i + 1}. {events[i]}");

                while (true)
                {
                    Console.Write("\nВведіть номер заходу для видалення (або 0 для виходу): ");
                    string input = Console.ReadLine();

                    if (!int.TryParse(input, out int index))
                    {
                        Console.WriteLine("Помилка! Введіть число.");
                        continue;
                    }

                    if (index == 0)
                    {
                        Console.WriteLine("Повернення до головного меню...");
                        Console.ReadKey();
                        return;
                    }

                    if (index < 1 || index > events.Count)
                    {
                        Console.WriteLine($"Невірний номер. Введіть число від 1 до {events.Count}.");
                        continue;
                    }

                    _manager.RemoveAt(index - 1);
                    _storage.Save(_manager.Events);
                    Console.WriteLine("Захід видалено.");
                    Console.ReadKey();
                    break;
                }
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
                if (input == "0") { exit = true; break; }
                if (string.IsNullOrWhiteSpace(input)) { dateFrom = null; break; }
                if (DateTime.TryParseExact(input, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime d))
                { dateFrom = d; break; }
                Console.WriteLine("Помилка! Введіть дату у форматі дд.мм.рррр або натисніть Enter.");
            }
            if (exit) { Console.WriteLine("Вихід. Натисніть будь-яку клавішу..."); Console.ReadKey(); return; }

            DateTime? dateTo = null;
            while (true)
            {
                Console.Write("Дата (до) [дд.мм.рррр] або Enter: ");
                string input = Console.ReadLine();
                if (input == "0") { exit = true; break; }
                if (string.IsNullOrWhiteSpace(input)) { dateTo = null; break; }
                if (DateTime.TryParseExact(input, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out DateTime d))
                { dateTo = d; break; }
                Console.WriteLine("Помилка! Введіть дату у форматі дд.мм.рррр або натисніть Enter.");
            }
            if (exit) { Console.WriteLine("Вихід. Натисніть будь-яку клавішу..."); Console.ReadKey(); return; }

            TimeSpan? timeFrom = null;
            while (true)
            {
                Console.Write("Час (від) [гг:хх] або Enter: ");
                string input = Console.ReadLine();
                if (input == "0") { exit = true; break; }
                if (string.IsNullOrWhiteSpace(input)) { timeFrom = null; break; }
                if (DateTime.TryParseExact(input, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime t))
                { timeFrom = t.TimeOfDay; break; }
                Console.WriteLine("Помилка! Введіть час у форматі гг:хх або натисніть Enter.");
            }
            if (exit) { Console.WriteLine("Вихід. Натисніть будь-яку клавішу..."); Console.ReadKey(); return; }

            TimeSpan? timeTo = null;
            while (true)
            {
                Console.Write("Час (до) [гг:хх] або Enter: ");
                string input = Console.ReadLine();
                if (input == "0") { exit = true; break; }
                if (string.IsNullOrWhiteSpace(input)) { timeTo = null; break; }
                if (DateTime.TryParseExact(input, "HH:mm", null, System.Globalization.DateTimeStyles.None, out DateTime t))
                { timeTo = t.TimeOfDay; break; }
                Console.WriteLine("Помилка! Введіть час у форматі гг:хх або натисніть Enter.");
            }
            if (exit) { Console.WriteLine("Вихід. Натисніть будь-яку клавішу..."); Console.ReadKey(); return; }

            int? durFrom = null;
            while (true)
            {
                Console.Write("Тривалість (від) [хвилини] або Enter: ");
                string input = Console.ReadLine();
                if (input == "0") { exit = true; break; }
                if (string.IsNullOrWhiteSpace(input)) { durFrom = null; break; }
                if (int.TryParse(input, out int d) && d >= 0) { durFrom = d; break; }
                Console.WriteLine("Помилка! Введіть ціле невід'ємне число або натисніть Enter.");
            }
            if (exit) { Console.WriteLine("Вихід. Натисніть будь-яку клавішу..."); Console.ReadKey(); return; }

            int? durTo = null;
            while (true)
            {
                Console.Write("Тривалість (до) [хвилини] або Enter: ");
                string input = Console.ReadLine();
                if (input == "0") { exit = true; break; }
                if (string.IsNullOrWhiteSpace(input)) { durTo = null; break; }
                if (int.TryParse(input, out int d) && d >= 0) { durTo = d; break; }
                Console.WriteLine("Помилка! Введіть ціле невід'ємне число або натисніть Enter.");
            }
            if (exit) { Console.WriteLine("Вихід. Натисніть будь-яку клавішу..."); Console.ReadKey(); return; }

            string placeSubstring = null;
            while (true)
            {
                Console.Write("Місце (частина назви) або Enter: ");
                string input = Console.ReadLine();
                if (input == "0") { exit = true; break; }
                if (string.IsNullOrWhiteSpace(input)) { placeSubstring = null; break; }
                placeSubstring = input.Trim(); break;
            }
            if (exit) { Console.WriteLine("Вихід. Натисніть будь-яку клавішу..."); Console.ReadKey(); return; }

            var results = _manager.Search(dateFrom, dateTo, timeFrom, timeTo, durFrom, durTo, placeSubstring);

            Console.Clear();
            Console.WriteLine("=== Результати пошуку ===");
            if (results.Count == 0)
                Console.WriteLine("Заходів, що відповідають критеріям, не знайдено.");
            else
                for (int i = 0; i < results.Count; i++)
                    Console.WriteLine($"{i + 1}. {results[i]}");

            Console.Write("\nЗберегти результати у файл? (т/н): ");
            while (true)
            {
                string answer = Console.ReadLine()?.Trim().ToLower();
                if (answer != "н" && answer != "т")
                { Console.Write("Уведіть \"т\" або \"н\" "); continue; }

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
                    else Console.WriteLine("Шлях не вказано – збереження скасовано.");
                }
                else Console.WriteLine("Збереження скасовано.");
                break;
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
                if (allowExit && input == "0") return DateTime.MinValue;
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
                if (allowExit && input == "0") return DateTime.MinValue;
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
                if (allowExit && input == "0") return 0;
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
                if (allowExit && input == "0") return null;
                if (!string.IsNullOrWhiteSpace(input)) return input.Trim();
                Console.WriteLine("Поле не може бути порожнім.");
            }
        }
    }
}