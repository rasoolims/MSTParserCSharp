using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MSTParser.Extensions
{
    public static class SequenceExtensions
    {
        public static List<T> SubList<T>(this List<T> lst, int fromIndex, int toIndex)
        {
            return lst.GetRange(fromIndex, toIndex - fromIndex + 1);
        }

        public static bool IsEmpty<TSource>(this IEnumerable<TSource> source)
        {
            return source.Count() <= 0;
        }

        public static string SubstringWithIndex(this string str, int startIndex, int endIndex)
        {
            if (String.IsNullOrEmpty(str))
                return str;

            //if (endIndex >= str.Length)
            //    endIndex = str.Length - 1;

            return str.Substring(startIndex, endIndex - startIndex);
        }

        public static string SubstringWithIndex(this string str, int startIndex)
        {
            if (String.IsNullOrEmpty(str))
                return str;
            return str.Substring(startIndex);
        }

        public static string SubstringWithIndex(this StringBuilder str, int startIndex)
        {
            if (String.IsNullOrEmpty(str.ToString()))
                return "";
            return str.ToString().SubstringWithIndex(startIndex);
        }

        public static string SubstringWithIndex(this StringBuilder str, int startIndex, int endIndex)
        {
            if (String.IsNullOrEmpty(str.ToString()))
                return "";
            //if (endIndex >= str.Length)
            //    endIndex = str.Length - 1;

            return str.ToString().SubstringWithIndex(startIndex, endIndex);
        }

        public static StringBuilder ReplaceSubstring(this StringBuilder sb, int startIndex, int endIndex,
                                                     string replacement)
        {
            //if (endIndex >= sb.Length)
            //    endIndex = sb.Length - 1;

            return sb.Remove(startIndex, endIndex - startIndex).Insert(startIndex, replacement);
        }

        public static bool Matches(this string str, string pattern)
        {
            return Matches(str, pattern, false);
        }

        public static bool Matches(this string str, string pattern, bool ignoreCase)
        {
            RegexOptions regexOpts = RegexOptions.None;
            if (ignoreCase)
                regexOpts = RegexOptions.IgnoreCase;

            Match m = Regex.Match(str, pattern, regexOpts);
            return m != null && m.Success && m.Index == 0 && m.Length == str.Length;
        }


        public static string ReplaceFirst(this string str, string regex, string with)
        {
            return ReplaceFirst(str, regex, with, false);
        }

        public static string ReplaceFirst(this string str, string regex, string with, bool ignoreCase)
        {
            RegexOptions regexOpts = RegexOptions.None;
            if (ignoreCase)
                regexOpts = RegexOptions.IgnoreCase;

            string result = str;
            Match match = Regex.Match(str, regex, regexOpts);
            if (match != null && match.Success)
            {
                result = str.Remove(match.Index, match.Length);
                result = result.Insert(match.Index, with);
            }

            return result;
        }

        public static string ReplaceAll(this string str, string regex, string with)
        {
            return ReplaceAll(str, regex, with, false);
        }

        public static string ReplaceAll(this string str, string regex, string with, bool ignoreCase)
        {
            RegexOptions regexOpts = RegexOptions.None;
            if (ignoreCase)
                regexOpts = RegexOptions.IgnoreCase;

            return Regex.Replace(str, regex, with, regexOpts);
        }


        public static bool EqualsIgnoreCase(this string str, string other)
        {
            return str.Equals(other, StringComparison.OrdinalIgnoreCase);
        }
    }
}