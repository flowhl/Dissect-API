using System.Text;
using System.Text.RegularExpressions;

namespace DissectAPI
{
    public static class StringExtensions
    {
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }
        public static bool IsNotNullOrEmpty(this string str)
        {
            return !string.IsNullOrEmpty(str);
        }
        public static bool IsNullOrWhiteSpace(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }
        public static string ToValidWindowsFileName(this string str)
        {
            string invalidChars = Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidReStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return Regex.Replace(str, invalidReStr, "_").Replace(" ", "-");
        }
        public static string RemoveNonNumeric(this string input)
        {
            // Use regular expression to remove non-numeric characters
            string pattern = "[^0-9]";
            string replacement = "";
            Regex regex = new Regex(pattern);
            string result = regex.Replace(input, replacement);

            return result;
        }
        public static string RemoveLeadingAndTrailingCommasAndDots(this string input)
        {
            char[] charsToTrim = { ',', '.' };
            return input.Trim().Trim(charsToTrim).Trim();
        }
        public static string RemoveAlternativeFromIconName(this string input)
        {
            input = input.Trim();
            while (input.Contains("_alternative"))
            {
                input = input.Replace("_alternative", "");
            }
            return input.Trim();
        }
    }
    public static class ExceptionExtensions
    {
        public static string GetFullMessage(this Exception exception)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("Exception and Inner Exceptions:");

            while (exception != null)
            {
                stringBuilder.AppendLine($"Message: {exception.Message}");
                stringBuilder.AppendLine($"StackTrace: {exception.StackTrace}");
                exception = exception.InnerException;
                if (exception != null)
                {
                    stringBuilder.AppendLine("--- Inner Exception ---");
                }
            }

            return stringBuilder.ToString();
        }
    }
}


