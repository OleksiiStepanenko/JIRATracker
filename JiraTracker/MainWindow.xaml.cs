using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace JiraTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var http = new HTTPRequests
            {
                UserProvider = () => tbUser.Text,
                PasswordProvider = () => tbPassword.Password
            };

            DataContext = new Model(http);
        }

        private void UncheckAllIssueSprints(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Model context)) return;
            foreach (var checkableItem in context.IssueSprints)
                checkableItem.IsChecked = false;
        }

        private void UncheckAllIssueTypes(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Model context)) return;
            foreach (var checkableItem in context.IssueTypes)
                checkableItem.IsChecked = false;
        }

        private void UncheckAllChangeFields(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Model context)) return;
            foreach (var checkableItem in context.ChangeFields)
                checkableItem.IsChecked = false;
        }

        private void CompletionClicked(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is Model context)) return;
            if (Equals(sender, CompletionAll))
                context.IssueCompletionFilter = null;
            if (Equals(sender, CompletionDone))
                context.IssueCompletionFilter = true;
            if (Equals(sender, CompletionNotDone))
                context.IssueCompletionFilter = false;
        }

        private void DragCompleted(object sender, DragCompletedEventArgs e)
        {
            slTime.GetBindingExpression(RangeBase.ValueProperty)?.UpdateSource();
        }
    }

    public class TicksConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((DateTime) value).Ticks;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new DateTime((long) (double) value);
        }
    }

    public class DateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return new DateTime((long)(double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((DateTime)value).Ticks;
        }
    }
}
