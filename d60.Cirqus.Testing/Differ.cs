using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnergyProjects.Tests.Utils
{
    public class Differ : diff_match_patch
    {
        public List<Diff> LineByLine(string text1, string text2)
        {
            var a = diff_linesToChars(text1, text2);
            var lineText1 = (string)a[0];
            var lineText2 = (string)a[1];
            var lineArray = (List<string>)a[2];

            var diffs = diff_main(lineText1, lineText2, false);

            diff_charsToLines(diffs, lineArray);
            return diffs;
        }

        public string PrettyLineByLine(List<Diff> diffs)
        {
            var s = new StringBuilder();
            foreach (var aDiff in diffs)
            {
                var lines = aDiff.text.Split(new[] { "\r\n" }, StringSplitOptions.None).ToList();
                if (lines.Last() == "")
                {
                    lines.Remove(lines.Last());
                }

                foreach (var line in lines)
                {
                    switch (aDiff.operation)
                    {
                        case Operation.INSERT:
                            s.Append("+ ").AppendLine(line);
                            break;
                        case Operation.DELETE:
                            s.Append("- ").AppendLine(line);
                            break;
                        case Operation.EQUAL:
                            s.Append("  ").AppendLine(line);
                            break;
                    }
                }
            }

            return s.ToString();
        }
    }

}