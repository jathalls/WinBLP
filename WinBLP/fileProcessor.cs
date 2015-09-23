using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace WinBLPdB
{
    
    /// <summary>
    /// This class handles the data processing for a single file,
    /// whether a manually generated composite file or a label file
    /// created by Audacity.
    /// </summary>
    internal class FileProcessor
    {
        enum MODE { PROCESS, SKIP, COPY, MERGE };

        private List<String> linesToMerge = null;

        /// <summary>
        /// The output string
        /// </summary>
        private string OutputString = "";

        MODE mode = MODE.PROCESS;
        /// <summary>
        /// The m bat summary
        /// </summary>
        private BatSummary mBatSummary;

        /// <summary>
        /// The bats found
        /// </summary>
        public Dictionary<string, BatStats> BatsFound = new Dictionary<string, BatStats>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileProcessor"/> class.
        /// </summary>
        public FileProcessor()
        {
        }

        /// <summary>
        /// Processes the file.
        /// </summary>
        /// <param name="batSummary">The bat summary.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="gpxHandler">The GPX handler.</param>
        /// <returns></returns>
        public String ProcessFile(BatSummary batSummary, string fileName, GpxHandler gpxHandler)
        {
            mBatSummary = batSummary;
            OutputString = "";
            if (fileName.ToUpper().EndsWith(".TXT"))
            {
                OutputString = ProcessLabelOrManualFile(fileName, gpxHandler);
            }
            return (OutputString);
        }

        /// <summary>
        /// Processes a text file with a simple .txt extension that has been
        /// generated as an Audacity LabelTrack.  The fileName will be added to the
        /// output at the start of the OutputString.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="gpxHandler">The GPX handler.</param>
        /// <returns></returns>
        private string ProcessLabelOrManualFile(string fileName, GpxHandler gpxHandler)
        {
            TimeSpan duration = new TimeSpan();
            Match match = null;
            mode = MODE.PROCESS;
            
        

            BatsFound = new Dictionary<string, BatStats>();
            string[] allLines = new string[1];
            DateTime fileStart;
            DateTime fileEnd;

            if (File.Exists(fileName))
            {
                string wavfile = "";
                duration = GetFileDuration(fileName, out wavfile, out fileStart, out fileEnd);
                OutputString = fileName;
                if (!string.IsNullOrWhiteSpace(wavfile))
                {
                    OutputString = fileName;
                }
                if (duration.Ticks > 0L)
                {
                    OutputString = OutputString + " \t" + duration.Minutes + "m" + duration.Seconds + "s";
                }
                OutputString = OutputString + "\n";
                List<decimal> gpsLocation = gpxHandler.GetLocation(fileStart);
                if (gpsLocation != null && gpsLocation.Count() == 2)
                {
                    OutputString = OutputString + gpsLocation[0] + ", " + gpsLocation[1];
                }
                gpsLocation = gpxHandler.GetLocation(fileEnd);
                if (gpsLocation != null && gpsLocation.Count() == 2)
                {
                    OutputString = OutputString + " => " + gpsLocation[0] + ", " + gpsLocation[1] + "\n";
                }

                try
                {
                    allLines = File.ReadAllLines(fileName);
                }
                catch (Exception ex) { Debug.WriteLine(ex); }

                if (allLines.Count() > 1 && allLines[0].StartsWith("["))
                {
                    if (allLines[0].ToUpper().StartsWith("[SKIP]") || allLines[0].ToUpper().StartsWith("[LOG]"))
                    {
                        mode = MODE.COPY;
                        return ("");
                    }
                    if (allLines[0].ToUpper().StartsWith("[COPY]"))
                    {
                        mode = MODE.COPY;
                        OutputString = "";
                        foreach (var line in allLines)
                        {
                            if (line.Contains("[MERGE]"))
                            {
                                mode = MODE.MERGE;

                                linesToMerge = new List<string>();
                            }
                            if (!line.Contains("[COPY]")  && !line.Contains("[MERGE]"))
                            {
                                if (mode == MODE.MERGE)
                                {
                                    linesToMerge.Add(line);
                                }
                                else
                                {

                                    OutputString = OutputString + line + "\n";
                                }
                            }
                        }
                        return (OutputString);
                    }
                }
                if (allLines != null && allLines.Count() > 0)
                {
                    if(linesToMerge!=null && linesToMerge.Count > 0)
                    {
                        OutputString = OutputString + linesToMerge[0] + "\n";
                        linesToMerge.Remove(linesToMerge[0]);
                    }
                    foreach (var line in allLines)
                    {
                        if (!String.IsNullOrWhiteSpace(line))
                        {
                            string modline = Regex.Replace(line, @"[Ss][Tt][Aa][Rr][Tt]", "0.0");
                            modline = Regex.Replace(modline, @"[Ee][Nn][Dd]", FormattedTimeSpan(duration));

                            if (IsLabelFileLine(modline))
                            {
                                OutputString = OutputString + ProcessLabelFileLine(modline);
                            }
                            else if ((match = IsManualFileLine(modline)) != null)
                            {
                                OutputString = OutputString + ProcessManualFileLine(match);
                            }
                            else
                            {
                                OutputString = OutputString + line + "\n";
                            }
                        }
                    }
                }
            }

            if (!String.IsNullOrWhiteSpace(OutputString) && BatsFound != null && BatsFound.Count() > 0)
            {
                foreach (var bat in BatsFound)
                {
                    OutputString = OutputString + "\n" + FormattedBatStats(bat);
                }
            }

            return (OutputString);
        }

        

        /// <summary>
        /// Processes the manual file line.
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        private string ProcessManualFileLine(Match match)
        {
            string comment = "";

            if (match.Groups.Count >= 5)
            {
                string strStartOffset = match.Groups[1].Value;
                TimeSpan startTime = GetTimeOffset(strStartOffset);
                comment = comment + FormattedTimeSpan(startTime) + " - ";
                string strEndOffset = match.Groups[3].Value;
                TimeSpan endTime = GetTimeOffset(strEndOffset);
                comment = comment + FormattedTimeSpan(endTime) + " = ";
                TimeSpan thisDuration = endTime - startTime;
                comment = comment + FormattedTimeSpan(endTime - startTime) + " \t";
                for (int i = 4; i < match.Groups.Count; i++)
                {
                    comment = comment + match.Groups[i];
                }
                AddToBatSummary(comment, thisDuration);
            }
            return (comment + "\n");
        }

        /// <summary>
        /// Determines whether [is manual file line] [the specified line].
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private Match IsManualFileLine(string line)
        {
            //string regexLabelFileLine = @"\A((\d*'?\s*\d*\.?\d*)|START)\s*-\s*((\d*'?\s*\d*\.?\d*)|END)\s*.*";
            string regexLabelFileLine = "([0-9.'\"]+)([\\s\t-]+)([0-9.'\"]+)\\s+(.*)";
            Match match = Regex.Match(line, regexLabelFileLine);
            if (match == null || match.Groups.Count < 5)
            {
                match = null;
            }

            if (match != null && match.Success) { return (match); }
            return (null);
        }

        /// <summary>
        /// Determines whether [is label file line] [the specified line].
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private bool IsLabelFileLine(string line)
        {
            string regexLabelFileLine = @"\A\d*\.?\d*\s+\d*\.?\d*\s*.*";
            Match match = Regex.Match(line, regexLabelFileLine);
            if (match.Success) { return (true); }
            return (false);
        }

        /// <summary>
        /// Processes the label file line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <returns></returns>
        private string ProcessLabelFileLine(string line)
        {
            string result = "";
            TimeSpan NewDuration;

            if (!string.IsNullOrWhiteSpace(line) && char.IsDigit(line[0]))
            {
                result = ProcessLabelLine(line, out NewDuration) + "\n";
                AddToBatSummary(line, NewDuration);
            }
            else
            {
                result = line + "\n";
            }

            return (result);
        }

        /// <summary>
        /// Adds to bat summary.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="NewDuration">The new duration.</param>
        private void AddToBatSummary(string line, TimeSpan NewDuration)
        {
            List<Bat> bats = mBatSummary.getBatElement(line);
            if (bats != null && bats.Count() > 0)
            {
                foreach (var bat in bats)
                {
                    string batname = bat.BatCommonNames.FirstOrDefault().BatCommonName1;
                    if (!string.IsNullOrWhiteSpace(batname))
                    {
                        if (BatsFound.ContainsKey(batname))
                        {
                            BatsFound[batname].Add(NewDuration);
                        }
                        else
                        {
                            BatsFound.Add(batname, new BatStats(NewDuration));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Formatteds the bat stats.
        /// </summary>
        /// <param name="bat">The bat.</param>
        /// <returns></returns>
        public static string FormattedBatStats(KeyValuePair<string, BatStats> bat)
        {
            int passes = bat.Value.Passes();
            string result = bat.Key + " " + passes + (passes == 1 ? " pass in " : " passes in ") + bat.Value.count + " segment" + (bat.Value.count != 1 ? "s" : "") +
                        " = ( " +
                        "Min=" + FormattedTimeSpan(bat.Value.minDuration) +
                        ", Max=" + FormattedTimeSpan(bat.Value.maxDuration) +
                        ", Mean=" + FormattedTimeSpan(bat.Value.meanDuration) + " )" +
                        "Total duration=" + FormattedTimeSpan(bat.Value.totalDuration);
            return (result);
        }

        /// <summary>
        /// Formatteds the time span.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        public static string FormattedTimeSpan(TimeSpan time)
        {
            string result = "";
            if (time != null && time.Ticks > 0)
            {
                time = time.Duration();
                if (time.Hours > 0)
                {
                    result = result + time.Hours + "h";
                }
                if (time.Hours > 0 || time.Minutes > 0)
                {
                    result = result + time.Minutes + "'";
                }
                decimal seconds = time.Seconds + ((decimal)time.Milliseconds / 1000.0m);
                result = result + string.Format("{0:0.0#}\"", seconds);
            }

            return (result);
        }

        /// <summary>
        /// Processes the label line.
        /// </summary>
        /// <param name="line">The line.</param>
        /// <param name="NewDuration">The new duration.</param>
        /// <returns></returns>
        private string ProcessLabelLine(string line, out TimeSpan NewDuration)
        {
            NewDuration = new TimeSpan(0L);
            line = line.Trim();
            if (!Char.IsDigit(line[0]))
            {
                return (line);
            }
            String outLine = "";
            TimeSpan StartTime;
            TimeSpan EndTime;
            TimeSpan duration;
            string shortened = line;

            Regex regexSeconds = new Regex(@"[0-9]+\.[0-9]+");
            MatchCollection allMatches = regexSeconds.Matches(line);
            if (allMatches.Count == 2)
            {
                double startTimeSeconds;
                double endTimeSeconds;
                Double.TryParse(allMatches[0].Value, out startTimeSeconds);
                Double.TryParse(allMatches[1].Value, out endTimeSeconds);
                int Minutes = (int)Math.Floor(startTimeSeconds / 60);
                int Seconds = (int)Math.Floor(startTimeSeconds - (Minutes * 60));
                int Milliseconds = (int)Math.Floor(1000 * (startTimeSeconds - Math.Floor(startTimeSeconds)));
                StartTime = new TimeSpan(0, 0, Minutes, Seconds, Milliseconds);
                Minutes = (int)Math.Floor(endTimeSeconds / 60);
                Seconds = (int)Math.Floor(endTimeSeconds - (Minutes * 60));
                Milliseconds = (int)Math.Floor(1000 * (endTimeSeconds - Math.Floor(endTimeSeconds)));
                EndTime = new TimeSpan(0, 0, Minutes, Seconds, Milliseconds);
                duration = EndTime - StartTime;
                NewDuration = duration;
                shortened = line.Trim();
                while (!String.IsNullOrWhiteSpace(shortened) && !Char.IsWhiteSpace(shortened[0])) shortened = shortened.Substring(1);
                shortened = shortened.Trim();
                while (!String.IsNullOrWhiteSpace(shortened) && !Char.IsWhiteSpace(shortened[0])) shortened = shortened.Substring(1);
                shortened = shortened.Trim();

                outLine = String.Format("{0:00}\'{1:00}.{2:0##} - {3:00}\'{4:00}.{5:0##} = {6:00}\'{7:00}.{8:0##}\t{9}",
                    StartTime.Minutes, StartTime.Seconds, StartTime.Milliseconds,
                    EndTime.Minutes, EndTime.Seconds, EndTime.Milliseconds,
                    duration.Minutes, duration.Seconds, duration.Milliseconds,shortened);
                /*
                outLine = StartTime.Minutes + @"'" + StartTime.Seconds + "." + StartTime.Milliseconds +
                " - " + EndTime.Minutes + @"'" + EndTime.Seconds + "." + EndTime.Milliseconds +
                " = " + duration.Minutes + @"'" + duration.Seconds + "." + duration.Milliseconds +
                "\t" + shortened;*/
            }
            else
            {
                StartTime = new TimeSpan();
                EndTime = new TimeSpan();
                outLine = line;
            }

            return (outLine);
        }

        /// <summary>
        /// Gets the duration of the file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="wavfile">The wavfile.</param>
        /// <param name="fileStart">The file start.</param>
        /// <param name="fileEnd">The file end.</param>
        /// <returns></returns>
        private TimeSpan GetFileDuration(string fileName, out string wavfile, out DateTime fileStart, out DateTime fileEnd)
        {
            DateTime CreationTime;
            fileStart = new DateTime();
            fileEnd = new DateTime();

            TimeSpan duration = new TimeSpan(0L);
            wavfile = "";
            try
            {
                string wavfilename = fileName.Substring(0, fileName.Length - 4);
                wavfilename = wavfilename + ".wav";
                if (File.Exists(wavfilename))
                {
                    wavfile = wavfilename;

                    String RecordingTime = wavfilename.Substring(fileName.LastIndexOf('_') + 1, 6);
                    DateTime recordingDateTime;
                    CreationTime = File.GetLastWriteTime(wavfilename);

                    if (RecordingTime.Length == 6)
                    {
                        int hour;
                        int minute;
                        int second;
                        if (!int.TryParse(RecordingTime.Substring(0, 2), out hour))
                        {
                            hour = -1;
                        }
                        if (!int.TryParse(RecordingTime.Substring(2, 2), out minute))
                        {
                            minute = -1;
                        }
                        if (!int.TryParse(RecordingTime.Substring(4, 2), out second))
                        {
                            second = -1;
                        }
                        if (hour >= 0 && minute >= 0 && second >= 0)
                        {
                            recordingDateTime = new DateTime(CreationTime.Year, CreationTime.Month, CreationTime.Day, hour, minute, second);
                            duration = CreationTime - recordingDateTime;
                            fileStart = recordingDateTime;
                            fileEnd = CreationTime;
                        }
                    }
                }
            }
            catch (Exception ex) { Debug.WriteLine(ex); }

            return (duration);
        }

        

 /*       /// <summary>
        /// using a string that matches the regex @"[0-9]+\.[0-9]+"
        /// or a string that matches the regex @"[0-9]+'?[0-9]*\.?[0-9]+"
        /// extracts one to three numeric portions and converts them to
        /// a timespan.  3 number represent minute,seconds,fraction
        /// 2 numbers represent seconds,fraction or minutes,seconds
        /// 1 number represents minutes or seconds
        /// </summary>
        /// <param name="match">The match.</param>
        /// <returns></returns>
        private static TimeSpan GetTimeOffset(Match match)
        {
            return (FileProcessor.GetTimeOffset(match.Value));
        }*/

        /// <summary>
        /// Gets the time offset.
        /// </summary>
        /// <param name="strTime">The string time.</param>
        /// <returns></returns>
        private static TimeSpan GetTimeOffset(String strTime)
        {
            int Minutes = 0;
            int Seconds = 0;
            int Milliseconds = 0;
            TimeSpan result = new TimeSpan();

            if (strTime.ToUpper().Contains("START") || strTime.ToUpper().Contains("END"))
            {
                strTime = "0.0";
            }

            String NumberRegex = @"[0-9]+";
            Regex regex = new Regex(NumberRegex);
            MatchCollection allMatches = regex.Matches(strTime);
            if (allMatches != null)
            {
                if (allMatches.Count == 3)
                {
                    int.TryParse(allMatches[0].Value, out Minutes);
                    int.TryParse(allMatches[1].Value, out Seconds);
                    int.TryParse(allMatches[2].Value, out Milliseconds);
                }
                else if (allMatches.Count == 2)
                {
                    if (strTime.Contains(@"'"))
                    {
                        int.TryParse(allMatches[0].Value, out Minutes);
                        int.TryParse(allMatches[1].Value, out Seconds);
                    }
                    else
                    {
                        int.TryParse(allMatches[0].Value, out Seconds);
                        int.TryParse(allMatches[1].Value, out Milliseconds);
                    }
                }
                else if (allMatches.Count == 1)
                {
                    if (strTime.Contains(@"'"))
                    {
                        int.TryParse(allMatches[0].Value, out Minutes);
                    }
                    else
                    {
                        int.TryParse(allMatches[0].Value, out Seconds);
                    }
                }
                result = new TimeSpan(0, 0, Minutes, Seconds, Milliseconds);
            }
            return (result);
        }
    }
}