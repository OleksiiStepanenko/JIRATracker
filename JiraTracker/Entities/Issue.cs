using JiraTracker.Annotations;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace JiraTracker.Entities
{
    public class Issue : INotifyPropertyChanged
    {
        public const string BlankEstimation = "-";
        public const string BlankResolved = "-";

        private readonly DateTime _created;
        private readonly string _latestType;
        private readonly string _latestSummary;
        private readonly string _latestEstimations;
        private readonly string _latestResolved;
        private readonly List<IssueChange> _allChanges;

        public string Key { get; }

        public string Type { get; private set; }

        public string Summary { get; private set; }

        public string Status { get; private set; }

        public string Estimations { get; private set; }

        public string Created { get; }

        public string Resolved { get; private set; }

        public List<Sprint> Sprints { get; }

        public List<IssueChange> Changes { get; private set; }

        public Issue(JObject token)
        {
            Key = token.Property("key").Value.Value<string>();
            Type = token.SelectToken("fields.issuetype.name").Value<string>();
            Summary = token.SelectToken("fields.summary").Value<string>();
            Status = token.SelectToken("fields.status.name").Value<string>();
            Created = token.SelectToken("fields.created").Value<string>();
            Resolved = token.SelectToken("fields.resolutiondate").Value<string>();
            Estimations = token.SelectToken("fields." + JiraConfiguration.StoryPointsField)?.Value<string>() ?? BlankEstimation;

            Sprints = new List<Sprint>();
            foreach (var sprint in token.SelectToken("fields." + JiraConfiguration.SprintsField).AsTokenList().OfType<JValue>())
                Sprints.Add(new Sprint(sprint));

            Changes = new List<IssueChange>();
            Changes.Add(new IssueChange(Created, "Created", "true", "false"));
            foreach (var change in (JArray)token.SelectToken("changelog.histories"))
                Changes.AddRange(IssueChange.Parse((JObject)change));

            _created = Created.ParseDate();
            _allChanges = Changes;
            _latestType = Type;
            _latestSummary = Summary;
            _latestEstimations = Estimations;
            _latestResolved = Resolved;
        }

        public DateTime CreationStamp()
        {
            return _created;
        }

        public bool IsResolved()
        {
            return string.IsNullOrWhiteSpace(Resolved) || Resolved == BlankResolved;
        }

        public void Retro(DateTime dateTime)
        {
            var changesInRange = _allChanges.ToList();
            string typeInRange = _latestType;
            string summaryInRange = _latestSummary;
            string estimationsInRange = _latestEstimations;
            string resolvedInRange = _latestResolved;

            changesInRange.Reverse();

            foreach (var change in changesInRange.ToList())
            {
                if (change.Timestamp.ParseDate() < dateTime) break;

                if (change.Field == "status" && change.OldValue == "Done")
                    resolvedInRange = change.Timestamp;
                else if (change.Field == "status" && change.NewValue == "Done")
                    resolvedInRange = BlankResolved;
                else if (change.Field == "Story points")
                    estimationsInRange = change.OldValue;
                else if (change.Field == "summary")
                    summaryInRange = change.OldValue;
                else if (change.Field == "IssueType")
                    typeInRange = change.OldValue;
                changesInRange.Remove(change);
            }

            changesInRange.Reverse();

            Type = typeInRange;
            Summary = summaryInRange;
            Estimations = estimationsInRange;
            Resolved = resolvedInRange;
            Changes = changesInRange;

            OnPropertyChanged(nameof(Type));
            OnPropertyChanged(nameof(Summary));
            OnPropertyChanged(nameof(Estimations));
            OnPropertyChanged(nameof(Resolved));
            OnPropertyChanged(nameof(Changes));
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}