using System.Collections.Generic;
using System.Linq;

namespace d60.Cirqus.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Indents the sequence of lines (by default by 4 spaces, relative to the current indentation level).
        /// Optionally specify another level of indentation by setting <see cref="indentation"/>, possibly
        /// removing any existing indentation by setting <see cref="absolute"/> to true
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> Indented(this IEnumerable<string> lines, int indentation = 4, bool absolute = false, char indentChar = ' ')
        {
            return lines.Select(line =>
            {
                if (absolute) line = line.TrimStart(' ');

                return string.Concat(new string(indentChar, indentation), line);
            });
        }
    }
}