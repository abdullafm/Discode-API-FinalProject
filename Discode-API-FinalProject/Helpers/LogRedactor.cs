using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace Discode_API_FinalProject.Helpers
{
    public static class LogRedactor
    {
        private static readonly Regex Secret = new("pass|token|secret|code|signature|key|card|authorization", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string Describe(object? value, int depth = 0)
        {
            if (value is null)
            {
                return "null";
            }

            if (value is string text)
            {
                return text.Length > 120 ? "\"" + text[..120] + "...\"" : "\"" + text + "\"";
            }

            var type = value.GetType();
            if (type.IsPrimitive || value is decimal || value is DateTime || value is DateTimeOffset || value is Guid || type.IsEnum)
            {
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            }

            if (value is Microsoft.AspNetCore.Http.IFormFile file)
            {
                return "file(" + file.Length + " bytes)";
            }

            if (value is IEnumerable list && value is not IDictionary)
            {
                var count = 0;
                foreach (var _ in list)
                {
                    count++;
                }

                return "list(" + count + ")";
            }

            if (depth > 1)
            {
                return type.Name;
            }

            var builder = new StringBuilder("{");
            var first = true;
            foreach (var property in type.GetProperties())
            {
                if (property.GetIndexParameters().Length > 0 || !property.CanRead)
                {
                    continue;
                }

                if (!first)
                {
                    builder.Append(", ");
                }

                first = false;
                builder.Append(property.Name).Append('=');
                builder.Append(Secret.IsMatch(property.Name) ? "***" : Describe(property.GetValue(value), depth + 1));
            }

            return builder.Append('}').ToString();
        }
    }
}
