using Microsoft.WindowsAzure.Storage.Table;

namespace ariabot.Models
{
    public class Review : EventSourceState
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public string Rating { get; set; }

        public Review()
        {
            SourceType = "review";
        }
    }
}