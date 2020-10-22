using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace JiraTracker.Entities
{
    public class IssueChange
    {
        public string Timestamp { get; }
        public string Field { get; }
        public string NewValue { get; }
        public string OldValue { get; }

        public IssueChange(string timestamp, string field, string newValue, string oldValue)
        {
            Timestamp = timestamp;
            Field = field;
            NewValue = newValue;
            OldValue = oldValue;
        }

        public IssueChange(string timestamp, JObject token)
        {
            Timestamp = timestamp;
            Field = token.Property("field").Value.Value<string>();
            NewValue = token.Property("toString").Value.Value<string>();
            OldValue = token.Property("fromString").Value.Value<string>();
        }

        public static List<IssueChange> Parse(JObject token)
        {
            string timestamp = token.Property("created").Value.Value<string>();

            List<IssueChange> changes = new List<IssueChange>();

            foreach (var change in (JArray)token.SelectToken("items"))
                changes.Add(new IssueChange(timestamp, (JObject)change));

            return changes;
        }
    }
}