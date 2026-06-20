using System;
using System.Collections.Generic;
using System.Linq;

namespace Diary
{
    public class DiaryManager
    {
        private List<Event> _events;

        public DiaryManager()
        {
            _events = new List<Event>();
        }

        public DiaryManager(List<Event> events)
        {
            _events = events ?? new List<Event>();
        }

        public IReadOnlyList<Event> Events => _events.AsReadOnly();

        public void AddEvent(Event ev) => _events.Add(ev);

        public void RemoveEvent(Event ev) => _events.Remove(ev);

        public void RemoveAt(int index) => _events.RemoveAt(index);

        public void SortEvents() => _events.Sort((a, b) => a.Start.CompareTo(b.Start));

        public Event GetNearest()
        {
            DateTime now = DateTime.Now;
            Event nearest = null;
            foreach (var ev in _events)
                if (ev.Start >= now && (nearest == null || ev.Start < nearest.Start))
                    nearest = ev;
            return nearest;
        }

        public List<Event> GetPastEvents()
        {
            DateTime now = DateTime.Now;
            return _events.FindAll(e => e.Start < now);
        }

        public List<Event> GetEventsByDate(DateTime date)
        {
            return _events.FindAll(e => e.Start.Date == date);
        }

        public List<(Event First, Event Second)> FindOverlaps()
        {
            var pairs = new List<(Event, Event)>();
            _events.Sort((a, b) => a.Start.CompareTo(b.Start));
            for (int i = 0; i < _events.Count; i++)
                for (int j = i + 1; j < _events.Count; j++)
                {
                    var a = _events[i];
                    var b = _events[j];
                    DateTime aEnd = a.Start.Add(a.Duration);
                    DateTime bEnd = b.Start.Add(b.Duration);
                    if (a.Start < bEnd && b.Start < aEnd)
                    {
                        pairs.Add((a, b));
                    }
                }
            return pairs;
        }

        public List<Event> Search(DateTime? dateFrom, DateTime? dateTo,
                                  TimeSpan? timeFrom, TimeSpan? timeTo,
                                  int? durFrom, int? durTo,
                                  string placeSubstring)
        {
            var results = new List<Event>();
            foreach (var ev in _events)
            {
                bool match = true;
                if (dateFrom.HasValue && ev.Start.Date < dateFrom.Value) match = false;
                if (dateTo.HasValue && ev.Start.Date > dateTo.Value) match = false;
                if (timeFrom.HasValue && ev.Start.TimeOfDay < timeFrom.Value) match = false;
                if (timeTo.HasValue && ev.Start.TimeOfDay > timeTo.Value) match = false;
                if (durFrom.HasValue && ev.Duration.TotalMinutes < durFrom.Value) match = false;
                if (durTo.HasValue && ev.Duration.TotalMinutes > durTo.Value) match = false;
                if (!string.IsNullOrEmpty(placeSubstring) &&
                    !ev.Place.Contains(placeSubstring, StringComparison.OrdinalIgnoreCase))
                    match = false;
                if (match) results.Add(ev);
            }
            results.Sort((a, b) => a.Start.CompareTo(b.Start));
            return results;
        }

        public void MovePastEvents(DateTime newDate)
        {
            var past = GetPastEvents();
            foreach (var ev in past)
                ev.Start = newDate.Date + ev.Start.TimeOfDay;
        }

        public void DeletePastEvents()
        {
            var past = GetPastEvents();
            foreach (var ev in past)
                _events.Remove(ev);
        }
    }
}
