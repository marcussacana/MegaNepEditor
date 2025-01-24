using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MegaNepEditor
{
    public class AdvBinHelper
    {

        AdvBin AdvBin;
        public AdvBinHelper(byte[] Script)
        {
            AdvBin = new AdvBin(Script);
        }

        List<string> Formats = new List<string>();
        List<int> Counts = new List<int>();

        public string[] Import()
        {
            var OriLines = AdvBin.Import();


            List<string> OutLines = new List<string>();
            for (int i = 0; i < OriLines.Length; i++)
            {
                SplitLines(OriLines[i], out string[] Lines, out string Format);

                Counts.Add(Lines.Length);
                Formats.Add(Format);
                OutLines.AddRange(Lines);
            }

            return OutLines.ToArray();
        }

        public byte[] Export(string[] Content)
        {
            List<string> FullLines = new List<string>();
            for (int i = 0, x = 0; i < Content.Length;)
            {
                var Count = Counts[x];
                var Format = Formats[x++];

                var Lines = Content.Skip(i).Take(Count).ToArray();

                var FinalLine = string.Format(Format, Lines);

                FullLines.Add(FinalLine);

                i += Count;
            }
            return AdvBin.Export(FullLines.ToArray());
        }

        public static void SplitLines(string FullLine, out string[] Lines, out string Format)
        {
            var OriLines = FullLine.Split('\n');

            Format = string.Empty;
            int Tag = 0;
            bool inFormat = false;
            bool inBegin = false;

            var CurrentLine = string.Empty;

            List<string> SplitedLines = new List<string>();
            for (var i = 0; i < OriLines.Length; i++)
            {
                inBegin = true;
                bool isLast = i + 1 >= OriLines.Length;
                bool inTag = false;
                foreach (var Char in OriLines[i])
                {
                    switch (Char)
                    {
                        case '<':
                            inTag = true;
                            BeginTag(ref Format, ref Tag, SplitedLines, ref CurrentLine, ref inFormat, ref inBegin);
                            Format += "<";
                            break;
                        case '>':
                            inTag = false;
                            Format += ">";
                            break;
                        default:
                            if (inTag)
                            {
                                inFormat = false;
                                Format += Char.ToString();

                                if (Char == '{' || Char == '}')
                                    Format += Char.ToString();
                            }
                            else
                            {
                                if (!char.IsWhiteSpace(Char))
                                    inFormat = false;

                                CurrentLine += Char.ToString();
                            }
                            break;
                    }
                    inBegin = true;
                }

                if (!isLast)
                {
                    if (string.IsNullOrWhiteSpace(CurrentLine))
                    {
                        Format += $"{CurrentLine}\n";
                        CurrentLine = string.Empty;
                    }
                    else
                        CurrentLine += "\n";
                }
            }
            BeginTag(ref Format, ref Tag, SplitedLines, ref CurrentLine, ref inFormat, ref inBegin);
            Lines = SplitedLines.ToArray();
        }

        private static void BeginTag(ref string Format, ref int Tag, List<string> SplitedLines, ref string CurrentLine, ref bool inFormat, ref bool inBegin)
        {
            if (!string.IsNullOrWhiteSpace(CurrentLine))
            {
                if (inFormat)
                {
                    SplitedLines[SplitedLines.Count - 1] += CurrentLine;
                }
                else
                {
                    var PostFormatData = string.Empty;
                    if (inBegin && CurrentLine.TrimEnd() != CurrentLine)
                    {
                        var Trimmed = CurrentLine.TrimEnd();
                        PostFormatData = CurrentLine.Substring(Trimmed.Length);
                        CurrentLine = CurrentLine.TrimEnd();
                    }
                    SplitedLines.Add(CurrentLine);
                    Format += $"{{{Tag++}}}{PostFormatData}";
                }
                CurrentLine = string.Empty;
                inFormat = true;
            }
            else
            {
                Format += CurrentLine;
                CurrentLine = string.Empty;
            }
        }
    }
}
