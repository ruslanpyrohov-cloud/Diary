using System;

namespace Diary
{
    public class Event
    {
        public DateTime Start { get; set; }
        public TimeSpan Duration { get; set; }
        public string Place { get; set; }

        public override string ToString()
        {
            return $"{Start:dd.MM.yyyy HH:mm} - {Start.Add(Duration):HH:mm} ({Duration.TotalMinutes} хв) : {Place}";
        }
    }
}
