using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Eocron.Algorithms.Paths
{
    public static class PathEx
    {
        /// <summary>
        ///     Evaluates all special commands in path, reducing its variants, such as:
        ///     a/./b/ to a/b/
        ///     a/../b/ to b/
        ///     without converting it to absolute path (so no need to call file system for this)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="joinSeparator"></param>
        /// <returns></returns>
        public static string Eval(string path, char? joinSeparator = null)
        {
            joinSeparator ??= '/';
            var parts = path.Split(Separators, StringSplitOptions.RemoveEmptyEntries);
            if (!parts.Any())
                return string.Empty;

            var result = new List<string>(parts.Length);
            var startSeparator = Separators.Any(path.StartsWith);
            var endSeparator = Separators.Any(path.EndsWith);
            var startDot = parts[0].StartsWith('.');
            foreach (var part in parts)
            {
                if (part == "..")
                {
                    var lastIdx = result.Count - 1;
                    if (lastIdx >= 0 && !result[lastIdx].EndsWith(':') && !result[lastIdx].EndsWith('.'))
                        result.RemoveAt(lastIdx);

                    continue;
                }

                if (part == "." && result.Count > 0)
                    continue;

                result.Add(part);
            }

            var sb = new StringBuilder(result.Capacity + path.Length + 3);
            if (!startDot && startSeparator)
                sb.Append(joinSeparator.Value);
            for (var i = 0; i < result.Count; i++)
            {
                if (i > 0)
                    sb.Append(joinSeparator.Value);
                sb.Append(result[i]);
            }

            if (endSeparator)
                sb.Append(joinSeparator.Value);
            return sb.ToString();
        }

        private static readonly char[] Separators =
            { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '\\', '/' };
    }
}