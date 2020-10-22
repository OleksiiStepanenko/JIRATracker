using System;
using System.Collections.Generic;
using System.Linq;

namespace JiraTracker.Entities
{
    public class Statistics
    {
        public string Issues { get; }
        public string StoryPoints { get; }
        public string StoryPointsCompleted { get; }
        public string SpentTime { get; }

        public string IdealStoryPointMax { get; }
        public string IdealStoryPointAvg { get; }
        public string IdealStoryPointMin { get; }

        public string TopIssue1 { get; }
        public string TopIssue1Count { get; }
        public string TopIssue2 { get; }
        public string TopIssue2Count { get; }
        public string TopIssue3 { get; }
        public string TopIssue3Count { get; }
        public string TopIssue4 { get; }
        public string TopIssue4Count { get; }
        public string TopIssue5 { get; }
        public string TopIssue5Count { get; }

        public Statistics(IReadOnlyCollection<Issue> issues, Func<object, bool> filter = null)
        {
            Issues = "-";
            StoryPoints = "-";
            SpentTime = "-";

            IdealStoryPointMax = "-";
            IdealStoryPointAvg = "-";
            IdealStoryPointMin = "-";

            TopIssue1 = "-";
            TopIssue1Count = "-";
            TopIssue2 = "-";
            TopIssue2Count = "-";
            TopIssue3 = "-";
            TopIssue3Count = "-";
            TopIssue4 = "-";
            TopIssue4Count = "-";
            TopIssue5 = "-";
            TopIssue5Count = "-";

            var realIssues = filter == null
                ? issues
                : issues.Where(issue => filter(issue)).ToList();

            if (realIssues.Count == 0) return;

            List<Tuple<TimeSpan, float, float>> spending = new List<Tuple<TimeSpan, float, float>>();
            foreach (var issue in realIssues)
            {
                var firstStatusChange = issue.Changes.FirstOrDefault(ch => ch.Field == "status");
                var lastStatusChange = issue.Changes.LastOrDefault(ch => ch.Field == "status");

                TimeSpan duration = firstStatusChange == null || lastStatusChange == null || (lastStatusChange.NewValue != "Done" && lastStatusChange.NewValue != "Canceled")
                    ? TimeSpan.MinValue
                    : lastStatusChange.Timestamp.ParseDate() -
                      firstStatusChange.Timestamp.ParseDate();
                float storyPoints= string.IsNullOrWhiteSpace(issue.Estimations) || issue.Estimations == Issue.BlankEstimation
                    ? float.NaN
                    : float.Parse(issue.Estimations);
                float storyPointCost = duration == TimeSpan.MinValue || float.IsNaN(storyPoints) || storyPoints <= 0
                    ? float.NaN
                    : (float) duration.TotalDays / storyPoints;

                spending.Add(Tuple.Create(duration, storyPoints, storyPointCost));
            }

            Issues =
                realIssues.Count.ToString();
            StoryPoints =
                spending.Where(tuple => !float.IsNaN(tuple.Item2)).Sum(tuple => tuple.Item2).ToString();
            StoryPointsCompleted =
                spending.Where(tuple => tuple.Item1 != TimeSpan.MinValue && !float.IsNaN(tuple.Item2)).Sum(tuple => tuple.Item2).ToString();
            SpentTime =
                GetReadableTimespan(spending.Where(tuple => tuple.Item1 != TimeSpan.MinValue)
                    .Aggregate(TimeSpan.Zero, (span, tuple) => span.Add(tuple.Item1)));

            var filteredSpending = spending.Where(tuple => !float.IsNaN(tuple.Item3)).ToList();
            IdealStoryPointMax = filteredSpending.Count > 0 ? filteredSpending.Max(tuple => tuple.Item3).ToString("F1") : "-";
            IdealStoryPointAvg = filteredSpending.Count > 0 ? filteredSpending.Average(tuple => tuple.Item3).ToString("F1") : "-";
            IdealStoryPointMin = filteredSpending.Count > 0 ? filteredSpending.Min(tuple => tuple.Item3).ToString("F1") : "-";

            var issueTypes = realIssues
                .Select(i => i.Type)
                .Distinct()
                .Select(p => Tuple.Create(p, realIssues.Count(i => i.Type == p)))
                .OrderByDescending(tuple => tuple.Item2)
                .ToList();

            TopIssue1 = issueTypes.Count > 0 ? issueTypes[0].Item1 : "-";
            TopIssue1Count = issueTypes.Count > 0 ? issueTypes[0].Item2.ToString() : "-";

            TopIssue2 = issueTypes.Count > 1 ? issueTypes[1].Item1 : "-";
            TopIssue2Count = issueTypes.Count > 1 ? issueTypes[1].Item2.ToString() : "-";

            TopIssue3 = issueTypes.Count > 2 ? issueTypes[2].Item1 : "-";
            TopIssue3Count = issueTypes.Count > 2 ? issueTypes[2].Item2.ToString() : "-";

            TopIssue4 = issueTypes.Count > 3 ? issueTypes[3].Item1 : "-";
            TopIssue4Count = issueTypes.Count > 3 ? issueTypes[3].Item2.ToString() : "-";

            TopIssue5 = issueTypes.Count > 4 ? issueTypes[4].Item1 : "-";
            TopIssue5Count = issueTypes.Count > 4 ? issueTypes[4].Item2.ToString() : "-";
        }

        private string GetReadableTimespan(TimeSpan ts)
        {
            var cutoff = new SortedList<long, string> {
                {59, "{4:S}" },
                {60, "{3:M}" },
                {60*60-1, "{3:M}, {4:S}"},
                {60*60, "{2:H}"},
                {24*60*60-1, "{2:H}, {3:M}"},
                {24*60*60, "{1:D}"},
                {365*24*60*60-1, "{1:D}, {2:H}"},
                {365*24*60*60, "{0:Y}, {1:D}"},
                {Int64.MaxValue , "{0:Y}, {1:D}"}
            };

            var find = cutoff.Keys.ToList().BinarySearch((long)ts.TotalSeconds);
            var near = find < 0 ? Math.Abs(find) - 1 : find;

            var years = ts.Days / 365;
            return string.Format(new HMSFormatter(), cutoff[cutoff.Keys[near]],
                years,
                ts.Days - (int) years * 365,
                ts.Hours,
                ts.Minutes,
                ts.Seconds);
        }
    }

    public class HMSFormatter : ICustomFormatter, IFormatProvider
    {
        static readonly Dictionary<string, string> _timeFormats = new Dictionary<string, string>
        {
            {"S", "{0:P:Seconds:Second}"},
            {"M", "{0:P:Minutes:Minute}"},
            {"H","{0:P:Hours:Hour}"},
            {"D", "{0:P:Days:Day}"},
            {"Y", "{0:P:Years:Year}"}
        };

        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            return string.Format(new PluralFormatter(), _timeFormats[format], arg);
        }

        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }
    }

    // formats a numeric value based on a format P:Plural:Singular
    public class PluralFormatter : ICustomFormatter, IFormatProvider
    {
        public string Format(string format, object arg, IFormatProvider formatProvider)
        {
            if (arg != null)
            {
                var parts = format.Split(':');
                if (parts[0] == "P")
                {
                    int partIndex = (arg.ToString() == "1") ? 2 : 1;
                    return $"{arg} {(parts.Length > partIndex ? parts[partIndex] : "")}";
                }
            }
            return string.Format(format, arg);
        }

        public object GetFormat(Type formatType)
        {
            return formatType == typeof(ICustomFormatter) ? this : null;
        }
    }
}
