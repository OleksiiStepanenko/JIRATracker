using System.Text;
using Newtonsoft.Json.Linq;

namespace JiraTracker.Entities
{
    public class Sprint
    {
        public const string BlankDate = "-";

        public string Name { get; }

        public string Started { get; }

        public string Completed { get; }

        public Sprint(JValue token)
        {
            string property = string.Empty;
            StringBuilder currentContainer = new StringBuilder();
            foreach (var ch in token.Value<string>())
            {
                if (ch == '[' || ch == '=')
                {
                    property = currentContainer.ToString();
                    currentContainer.Clear();
                }
                else if (ch == ']' || ch == ',')
                {
                    if (property == "name")
                        Name = currentContainer.ToString();
                    else if (property == "startDate")
                        Started = JsonExtensions.ConvertDate(currentContainer.ToString());
                    else if (property == "completeDate")
                        Completed = JsonExtensions.ConvertDate(currentContainer.ToString());
                    currentContainer.Clear();
                }
                else
                {
                    currentContainer.Append(ch);
                }
            }
        }
    }
}
