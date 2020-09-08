using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AgencyDispatchFramework.Extensions
{
    public static class StringExtensions
    {
        public static IEnumerable<int> ToIntList(this string str)
        {
            if (String.IsNullOrEmpty(str))
                yield break;

            foreach (var s in str.Split(','))
            {
                if (int.TryParse(s, out int num))
                    yield return num;
            }
        }

        public static string GetRandomString(this string[] items)
        {
            if (items.Length == 0) return String.Empty;

            int count = items.Length - 1;
            int index = new Random().Next(0, count);
            return items[index];
        }

        internal static string WordWrap(this String inputString, int width, string fontFamily)
        {
            inputString = WordWrapText(inputString,
                width,
                fontFamily,
                9f);

            return inputString;
        }

        /// <summary>
        /// Credits to the Computer+ Dev team
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pixels"></param>
        /// <param name="fontFamily"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        private static string WordWrapText(string text, double pixels, string fontFamily, float size)
        {
            double emSize = size;
            emSize = (96.0f / 72.0f) * size;

            string[] originalLines = text.Split(new string[] { " " }, StringSplitOptions.None);
            List<string> separatedLines = new List<string>();
            List<string> wrappedLines = new List<string>();

            foreach (var item in originalLines)
            {
                string[] words = item.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                if (words.Length == 1)
                {
                    separatedLines.Add(words[0]);
                }
                else if (words.Length == 2)
                {
                    separatedLines.Add(words[0]);
                    separatedLines.Add("");
                    separatedLines.Add(words[1]);
                }
                else
                {
                    foreach (string w in words)
                    {
                        separatedLines.Add(w);
                    }
                }
            }

            StringBuilder actualLine = new StringBuilder();
            double actualWidth = 0;

            foreach (var item in separatedLines)
            {
#pragma warning disable CS0618 // Type or member is obsolete
                FormattedText formatted = new FormattedText(item,
                     CultureInfo.GetCultureInfo("en-us"),
                     System.Windows.FlowDirection.LeftToRight,
                     new Typeface(fontFamily), emSize, Brushes.Black);
#pragma warning restore CS0618 // Type or member is obsolete

                actualLine.Append(item + " ");
                actualWidth += formatted.Width;

                if (actualWidth > pixels && item != "")
                {
                    wrappedLines.Add(actualLine.ToString());
                    actualLine.Clear();
                    actualWidth = 0;
                }
            }

            if (actualLine.Length > 0)
                wrappedLines.Add(actualLine.ToString());

            string allLines = string.Join(Environment.NewLine, wrappedLines.ToArray());

            return allLines;
        }
    }
}
