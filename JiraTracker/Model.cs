using JiraTracker.Annotations;
using JiraTracker.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Input;
using ThreeShape.Wpf.Commands;

namespace JiraTracker
{
    public class Model : INotifyPropertyChanged
    {
        private readonly Func<string> _userProvider;
        private readonly Func<string> _passwordProvider;

        private IReadOnlyList<Sprint> _sprints;
        private IReadOnlyList<Issue> _issues;
        private IReadOnlyList<CheckableItem> _issueTypes;
        private IReadOnlyList<CheckableItem> _issueChangeFields;
        private IReadOnlyList<SprintCheckableItem> _issueSprints;

        private string _issueFilter;
        private bool? _issueCompletionFilter;
        private HashSet<string> _issueSprintFilter;
        private HashSet<string> _issueTypeFilter;
        private HashSet<string> _issueChangeFieldFilter;

        private Statistics _statistics;
        private DateTime _start;
        private DateTime _now;
        private DateTime _end;
        private readonly HTTPRequests _httpRequests;

        public string ProjectKey { get; set; } = "SD";

        public IReadOnlyList<Issue> Issues
        {
            get => _issues;
            private set
            {
                _issues = value;
                if (_issues != null)
                {
                    CollectionViewSource.GetDefaultView(Issues).Filter = FilterIssues;
                    foreach (var issue in _issues)
                        CollectionViewSource.GetDefaultView(issue.Changes).Filter = FilterChanges;
                }
                OnPropertyChanged(nameof(Issues));
            }
        }
        public IReadOnlyList<SprintCheckableItem> IssueSprints
        {
            get => _issueSprints;
            private set
            {
                if (_issueSprints != null)
                    foreach (var sprint in _issueSprints)
                        sprint.PropertyChanged -= HandleIssueSprintChanged;

                _issueSprints = value;

                if (_issueSprints != null)
                    foreach (var sprint in _issueSprints)
                        sprint.PropertyChanged += HandleIssueSprintChanged;

                HandleIssueSprintChanged(null, null);
                OnPropertyChanged();
            }
        }
        public IReadOnlyList<CheckableItem> IssueTypes
        {
            get => _issueTypes;
            private set
            {
                if (_issueTypes != null)
                    foreach (var type in _issueTypes)
                        type.PropertyChanged -= HandleIssueFilterChanged;

                _issueTypes = value;

                if (_issueTypes != null)
                    foreach (var type in _issueTypes)
                        type.PropertyChanged += HandleIssueFilterChanged;

                HandleIssueFilterChanged(null, null);
                OnPropertyChanged();
            }
        }
        public IReadOnlyList<CheckableItem> ChangeFields
        {
            get => _issueChangeFields;
            private set
            {
                if (_issueChangeFields != null)
                    foreach (var field in _issueChangeFields)
                        field.PropertyChanged -= HandleChangeFieldsChanged;

                _issueChangeFields = value;

                if (_issueChangeFields != null)
                    foreach (var field in _issueChangeFields)
                        field.PropertyChanged += HandleChangeFieldsChanged;

                HandleChangeFieldsChanged(null, null);
                OnPropertyChanged();
            }
        }

        public string IssueFilter
        {
            get => _issueFilter;
            set
            {
                var newValue = value.Trim();

                if (_issueFilter == newValue) return;

                _issueFilter = newValue;
                OnPropertyChanged();

                if (_issues != null)
                    foreach (var issue in _issues)
                        CollectionViewSource.GetDefaultView(issue.Changes).Filter = FilterChanges;
                HandleIssueFilterChanged(null, new PropertyChangedEventArgs(string.Empty));
                HandleChangeFieldsChanged(null, new PropertyChangedEventArgs(string.Empty));
            }
        }

        public bool? IssueCompletionFilter

        {
            get => _issueCompletionFilter;
            set
            {
                if (_issueCompletionFilter == value) return;

                _issueCompletionFilter = value;
                OnPropertyChanged();

                HandleIssueFilterChanged(null, new PropertyChangedEventArgs(string.Empty));
            }
        }

        public DateTime Start
        {
            get => _start;
            private set
            {
                _start = value;
                OnPropertyChanged();
            }
        }

        public DateTime Now
        {
            get => _now;
            set
            {
                _now = value;
                OnPropertyChanged();

                foreach (var issue in _issues)
                    issue.Retro(_now);

                HandleIssueFilterChanged(null, new PropertyChangedEventArgs(string.Empty));
            }
        }

        public DateTime End
        {
            get => _end;
            private set
            {
                _end = value;
                OnPropertyChanged();
            }
        }

        public Statistics Statistics
        {
            get => _statistics;
            set
            {
                _statistics = value;
                OnPropertyChanged();
            }
        }

        public ICommand RequestCommand { get; }


        /// <summary>
        /// Constructor
        /// </summary>
        public Model(HTTPRequests http)
        {
            Issues = new ObservableCollection<Issue>();
            IssueTypes = new List<CheckableItem>();
            ChangeFields = new List<CheckableItem>();

            RequestCommand = new RelayCommand(ProcessIssues);
            _httpRequests = http;
        }


        private void ProcessIssues()
        {
            const int pageSize = 50;
            const string request = "search?jql=project={0}+order+by+key&fields={1}&expand=changelog&startAt={2}&maxResults={3}";

            if (!_httpRequests.Request(string.Format(request, ProjectKey, GetRequestedFields(), 0, pageSize), out string data)) return;

            int totalCount = 0;
            List<Issue> issues = new List<Issue>();
            do
            {
                var json = JObject.Parse(data);

                totalCount = json.Property("total").Value.Value<int>();

                foreach (var issue in (JArray)json.SelectToken("issues"))
                {
                    issues.Add(new Issue((JObject)issue));
                }

                if (!_httpRequests.Request(string.Format(request, ProjectKey, GetRequestedFields(), issues.Count, pageSize), out data)) return;
            }
            while (issues.Count < totalCount);

            // unify sprints
            var allSprints = issues.SelectMany(i => i.Sprints).ToList();
            _sprints = allSprints.Select(sprint => sprint.Name).Distinct()
                .Select(sprint => allSprints.FirstOrDefault(sp => sp.Name == sprint)).ToList();
            foreach (var issue in issues)
            {
                var newSprints = issue.Sprints.Select(sprint => _sprints.FirstOrDefault(sp => sp.Name == sprint.Name)).ToList();
                issue.Sprints.Clear();
                issue.Sprints.AddRange(newSprints);
            }

            IssueSprints = _sprints.OrderByDescending(p => p.Started.ParseDate())
                .Select(p => new SprintCheckableItem(p.Name, p.Started, p.Completed) { IsChecked = false }).ToList();
            IssueTypes = issues.Select(i => i.Type).Distinct().OrderBy(p => p)
                .Select(p => new CheckableItem(p)).ToList();
            ChangeFields = issues.SelectMany(i => i.Changes).Select(c => c.Field).Distinct().OrderBy(p => p)
                .Select(p => new CheckableItem(p)).ToList();
            Issues = new ObservableCollection<Issue>(issues);

            Statistics = new Statistics(Issues, FilterIssues);

            Start = Issues.Min(issue => issue.Created.ParseDate()).Subtract(TimeSpan.FromDays(30));
            End = Issues.Max(issue => issue.Created.ParseDate()).Add(TimeSpan.FromDays(30));
            Now = DateTime.Now > End ? End : DateTime.Now;
        }

        private bool FilterIssues(object target)
        {
            Issue issue = target as Issue;
            if (issue == null) return false;

            if (!_issueTypeFilter.Contains(issue.Type)) return false;

            if (_issueCompletionFilter.HasValue && _issueCompletionFilter.Value && issue.IsResolved()) return false;

            if (_issueCompletionFilter.HasValue && !_issueCompletionFilter.Value && !issue.IsResolved()) return false;

            if (!string.IsNullOrWhiteSpace(_issueFilter)
                && !issue.Key.Contains(_issueFilter)
                && !issue.Summary.Contains(_issueFilter)) return false;

            if (_issueSprintFilter.Count > 0 && !issue.Sprints.Any(sprint => _issueSprintFilter.Contains(sprint.Name))) return false;

            if (issue.CreationStamp() > Now) return false;

            return true;
        }
        private bool FilterChanges(object target)
        {
            IssueChange change = target as IssueChange;
            if (change == null) return false;

            if (!_issueChangeFieldFilter.Contains(change.Field)) return false;

            return true;
        }

        private void HandleIssueSprintChanged(object sender, PropertyChangedEventArgs e)
        {
            _issueSprintFilter = new HashSet<string>(IssueSprints.Where(t => t.IsChecked).Select(t => t.Title));
            CollectionViewSource.GetDefaultView(Issues).Refresh();

            Statistics = new Statistics(Issues, FilterIssues);
        }

        private void HandleIssueFilterChanged(object sender, PropertyChangedEventArgs e)
        {
            _issueTypeFilter = new HashSet<string>(IssueTypes.Where(t => t.IsChecked).Select(t => t.Title));
            CollectionViewSource.GetDefaultView(Issues).Refresh();

            Statistics = new Statistics(Issues, FilterIssues);
        }

        private void HandleChangeFieldsChanged(object sender, PropertyChangedEventArgs e)
        {
            _issueChangeFieldFilter = new HashSet<string>(ChangeFields.Where(t => t.IsChecked).Select(t => t.Title));
            foreach (var issue in Issues)
                CollectionViewSource.GetDefaultView(issue.Changes).Refresh();
        }

        private string GetRequestedFields()
        {
            string[] requestFields =
            {
                "id",
                "key",
                "issuetype",
                "summary",
                "created",
                "resolutiondate",
                "status",
                JiraConfiguration.StoryPointsField,
                JiraConfiguration.SprintsField
            };

            return string.Join(",", requestFields);
        }


        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CheckableItem : INotifyPropertyChanged
    {
        private bool _isChecked;

        public string Title { get; }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }

        public CheckableItem(string title)
        {
            Title = title;
            IsChecked = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class SprintCheckableItem : CheckableItem
    {
        public DateTime Started { get; }
        public DateTime Completed { get; }

        public SprintCheckableItem(string title, string started, string completed) : base(title)
        {
            Started = started.ParseDate();
            Completed = completed.ParseDate();
        }
    }
}
