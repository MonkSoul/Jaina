using System;

namespace Jaina.Benchmarks
{
    public sealed class Wrapper
    {
        public Wrapper(string eventId)
        {
            EventId = eventId;
        }

        public string EventId { get; set; }

        public DateTime CreatedTime { get; } = DateTime.UtcNow;

        public bool ShouldRun(string eventId)
        {
            return EventId == eventId;
        }
    }
}