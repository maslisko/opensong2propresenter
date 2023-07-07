using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Text.RegularExpressions;
using System.Xml;

namespace OpenSong2ProPresenter
{
    class Program
    {
        const string ProPresenterRelativePath = "ProPresenter";
        static readonly List<string> SectionMarks = new List<string>();
        static Dictionary<string, string> SectionMarkReplacements = new Dictionary<string, string>()
        {
            { "[1]", "Verse 1"},
            { "[2]", "Verse 2"},
            { "[3]", "Verse 3"},
            { "[4]", "Verse 4"},
            { "[5]", "Verse 5"},
            { "[b]", "Bridge"},
            { "[B]", "Bridge"},
            { "[B1]", "Bridge 1"},
            { "[B2]", "Bridge 2"},
            { "[B3]", "Bridge 3"},
            { "[c]", "Chorus"},
            { "[C]", "Chorus"},
            { "[c1]", "Chorus 1"},
            { "[C1]", "Chorus 1"},
            { "[c2]", "Chorus 2"},
            { "[C2]", "Chorus 2"},
            { "[C3]", "Chorus 3"},
            { "[C4]", "Chorus 4"},
            { "[C5]", "Chorus"},
            { "[Ca]", "Chorus 1"},
            { "[CA]", "Chorus 1"},
            { "[Cb]", "Chorus 2"},
            { "[CB]", "Chorus 2"},
            { "[I]", "Intro"},
            { "[P]", "Prechorus"},
            { "[P1]", "Prechorus"},
            { "[P2]", "Prechorus"},
            { "[r]", "Chorus"},
            { "[R]", "Chorus"},
            { "[R1]", "Chorus 1"},
            { "[R2]", "Chorus 2"},
            { "[R3]", "Chorus 3"},
            { "[R4]", "Chorus 4"},
            { "[R5]", "Chorus"},
            { "[T]", "Turnaround"},
            { "[Ta]", "Turnaround"},
            { "[Tb]", "Turnaround"},
            { "[V]", "Verse"},
            { "[v1]", "Verse 1"},
            { "[V1]", "Verse 1"},
            { "[v2]", "Verse 2"},
            { "[V2]", "Verse 2"},
            { "[v3]", "Verse 3"},
            { "[V3]", "Verse 3"},
            { "[v4]", "Verse 4"},
            { "[V4]", "Verse 4"},
            { "[v5]", "Verse 5"},
            { "[V5]", "Verse 5"},
            { "[V6]", "Verse 6"},
            { "[V7]", "Verse"},
            { "[V8]", "Verse"},
            { "[V9]", "Verse"},
            { "[VV]", "Verse"}
        };

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Error: path to OpenSong data directory is missing");
                Console.WriteLine("Example: opensong2propresenter C:\\temp\\songs");
                Console.ReadKey(true);
                return;
            }
            string dataPath = args[0];

            string[] filenames;
            try
            {
                filenames = Directory.GetFiles(dataPath, "*", SearchOption.AllDirectories);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return;
            }

            foreach (var filePath in filenames)
            {
                Console.WriteLine($"Processing: {filePath}");

                var destinationRelativePath = Path.GetDirectoryName(filePath).Replace(dataPath + "\\", "");
                var destinationFilePath = Path.Combine(dataPath, ProPresenterRelativePath, destinationRelativePath);
                var fileName = Path.GetFileName(filePath);
                if (!string.IsNullOrWhiteSpace(destinationRelativePath))
                {
                    Directory.CreateDirectory(destinationFilePath);
                }

                XDocument xml = new XDocument();
                try
                {
                    xml = XDocument.Load(filePath);

                    var lyrics = xml.Root.Descendants("lyrics").FirstOrDefault().Value;
                    var songName = xml.Root.Descendants("title").FirstOrDefault().Value;
                    fileName = xml.Root.Descendants("title").FirstOrDefault().Value
                        .Replace(":", "-")
                        .Replace("?", "");

                    AddSectionMarksFromSongLyrics(lyrics);

                    lyrics = ConvertSectionMarks(lyrics);
                    lyrics = ProcessLines(lyrics);

                    File.WriteAllText(Path.Combine(destinationFilePath, fileName + ".txt"), $"{songName}\n\n{lyrics}");
                }
                catch (Exception ex)
                {
                    ReportException(ex);
                    continue;
                }
            }

            if (SectionMarks.Count > 0)
            {
                SectionMarks.Sort();
                File.WriteAllText(Path.Combine(dataPath, "sectionMarks.txt"), SectionMarks.Aggregate((i, j) => i + "\r\n" + "{ \"" + j + "\", \"\"},"));
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }

        private static void ReportException(Exception ex)
        {
            switch (ex)
            {
                case XmlException e:
                    Console.WriteLine("XML exception: " + e.SourceUri);
                    break;
                case Exception e:
                    Console.WriteLine($"Generic exception: {e.GetType()}: {e.Message}");
                    break;
            }
        }

        private static string ProcessLines(string lyrics)
        {
            List<string> lines = lyrics.Split(
                new[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            ).ToList();

            StringBuilder processedLyrics = new StringBuilder();
            foreach (var l in lines)
            {
                string line = l;
                line = RemoveChordsFromLine(line);
                line = RemoveLeadingWhiteSpacesFromLine(line);
                line = RemoveTrailingEolsFromLine(line);

                processedLyrics.Append(line + "\r\n");
            }
            return processedLyrics.ToString();
        }

        private static string RemoveLeadingWhiteSpacesFromLine(string line)
        {
            return line.TrimStart(' ');
        }

        private static string RemoveTrailingEolsFromLine(string line)
        {
            return line.Replace(Environment.NewLine, "");
        }

        private static string RemoveChordsFromLine(string line)
        {
            return line.StartsWith(".") ? string.Empty : line;
        }

        private static string ConvertSectionMarks(string lyrics)
        {
            foreach (var mark in SectionMarkReplacements.Keys)
            {
                lyrics = lyrics.Replace(mark, SectionMarkReplacements[mark]);
            }
            return lyrics;
        }

        private static void AddSectionMarksFromSongLyrics(string lyrics)
        {
            MatchCollection matchList = Regex.Matches(lyrics, @"\[[a-zA-Z0-9]{1,2}\]");
            foreach (Match match in matchList)
            {
                if (SectionMarks.Contains(match.Value))
                {
                    continue;
                }
                SectionMarks.Add(match.Value);
            }
        }
    }
}
