using Microsoft.WindowsAzure.Storage.Table;

namespace ariabot.Models
{
    public class EventSourceLocation : EventSourceState
    {
        public string Location { get; set; }

        public EventSourceLocation() { }
    }
}