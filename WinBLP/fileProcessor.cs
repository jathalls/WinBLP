using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BatRecordingManager
{
    /// <summary>
    ///     Class to hold details of a specific LabelledSegment, and a List of Bats that were present
    ///     during this segment.
    /// </summary>
    public class SegmentAndBatList
    {
        /// <summary>
        ///     The List of Bats present during the segment
        /// </summary>
        public ObservableCollection<Bat> batList = new ObservableCollection<Bat>();

        /// <summary>
        ///     The Labelled Segment
        /// </summary>
        public LabelledSegment segment = new LabelledSegment();

        /// <summary>
        ///     Initializes a new instance of the <see cref="SegmentAndBatList"/> class.
        /// </summary>
        public SegmentAndBatList()
        {
            segment = new LabelledSegment();
            batList = new ObservableCollection<Bat>();
        }
    }

    /// <summary>
    ///     This class handles the data processing for a single file, whether a manually generated
    ///     composite file or a label file created by Audacity.
    /// </summary>
    internal class FileProcessor
    {
        /// <summary>
        ///     The bats found
        /// </summary>
        public Dictionary<string, BatStats> BatsFound = new Dictionary<string, BatStats>();

        private ObservableCollection<String> linesToMerge = null;

        /// <summary>
        ///     The m bat summary
        /// </summary>
        private BatSummary mBatSummary;

        private MODE mode = MODE.PROCESS;

        /// <summary>
        ///     The output string
        /// </summary>
        private string OutputString = "";

        /// <summary>
        ///     Initializes a new instance of the <see cref="FileProcessor"/> class.
        /// </summary>
        public FileProcessor()
        {
        }

        private enum MODE
        { PROCESS, SKIP, COPY, MERGE };

        /// <summary>
        ///     Determines whether [is label file line] [the specified line].
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="startStr">
        ///     The start string.
        /// </param>
        /// <param name="endStr">
        ///     The end string.
        /// </param>
        /// <param name="comment">
        ///     The comment.
        /// </param>
        /// <returns>
        /// </returns>
        public static bool IsLabelFileLine(string line, out string startStr, out string endStr, out string comment)
        {
            startStr = "";
            endStr = "";
            comment = "";
            string regexLabelFileLine = "\\A\\s*(\\d*\\.?\\d*)\\\"?\\s+-?\\s*(\\d*\\.?\\d*)\\\"?\\s*(.*)";
            // e.g. (groups in brackets) <start> (nnn.nnn)" - (nnn.nnn)" (other text)
            Match match = Regex.Match(line, regexLabelFileLine);
            if (match.Success)
            {
                startStr = match.Groups[1].Value;
                endStr = match.Groups[2].Value;
                comment = match.Groups[3].Value;
                return (true);
            }
            return (false);
        }

        public static bool IsLabelFileLine(string line, out TimeSpan start, out TimeSpan end, out string comment)
        {
            string startStr = "";
            string endStr = "";
            if (!IsLabelFileLine(line, out startStr, out endStr, out comment))
            {
                start = new TimeSpan();
                end = new TimeSpan();
                return (false);
            }

            start = Tools.TimeParse(startStr);
            end = Tools.TimeParse(endStr);

            return (true);
        }

        /// <summary>
        ///     Processes the labelled segment. Accepts a processed segment comment line consisting
        ///     of a start offset, end offset, duration and comment string and generates a new
        ///     Labelled segment instance and BatSegmentLink instances for each bat represented in
        ///     the Labelled segment. The instances are merged into a single instance of
        ///     CombinedSegmentAndBatPasses to be returned. If the line to be processed is not in the
        ///     correct format then an instance containing an empty LabelledSegment instance and an
        ///     empty List of ExtendedBatPasses. The comment section is checked for the presence of a
        ///     call parameter string and if present new Call is created and populated.
        /// </summary>
        /// <param name="processedLine">
        ///     The processed line.
        /// </param>
        /// <param name="bats">
        ///     The bats.
        /// </param>
        /// <returns>
        /// </returns>
        /// <exception cref="System.NotImplementedException">
        /// </exception>
        public static SegmentAndBatList ProcessLabelledSegment(string processedLine, ObservableCollection<Bat> bats)
        {
            LabelledSegment segment = new LabelledSegment();
            SegmentAndBatList result = new SegmentAndBatList();
            var match = Regex.Match(processedLine, "([0-9\\.\\']+)[\\\"]?\\s-?\\s*([0-9\\.\\']+)[\\\"]?\\s=\\s([0-9\\.\']+)[\\\"]?\\s(.+)");
            //e.g. (123'12.3)" - (123'12.3)" = (123'12.3)" (other text)
            if (match.Success)
            {
                //int passes = 1;
                // The line structure matches a labelled segment
                if (match.Groups.Count > 3)
                {
                    segment.Comment = match.Groups[4].Value;

                    TimeSpan ts = Tools.TimeParse(match.Groups[2].Value);
                    segment.EndOffset = ts;
                    ts = Tools.TimeParse(match.Groups[1].Value);
                    segment.StartOffset = ts;
                    result.segment = segment;
                    result.batList = bats;
                    //ts = TimeParse(match.Groups[3].Value);
                    //passes = new BatStats(ts).passes;
                }
                // result.batPasses = IdentifyBatPasses(passes, bats);
            }

            return (result);
        }

        /// <summary>
        ///     Adds to bat summary.
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="NewDuration">
        ///     The new duration.
        /// </param>
        public ObservableCollection<Bat> AddToBatSummary(string line, TimeSpan NewDuration)
        {
            ObservableCollection<Bat> bats = mBatSummary.getBatElement(line);
            if (bats != null && bats.Count() > 0)
            {
                foreach (var bat in bats)
                {
                    string batname = bat.Name;
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
            return (bats);
        }

        /// <summary>
        ///     Processes the file using ProcessLabelOrManualFile.
        /// </summary>
        /// <param name="batSummary">
        ///     The bat summary.
        /// </param>
        /// <param name="fileName">
        ///     Name of the file.
        /// </param>
        /// <param name="gpxHandler">
        ///     The GPX handler.
        /// </param>
        /// <param name="CurrentRecordingSessionId">
        ///     The current recording session identifier.
        /// </param>
        /// <returns>
        /// </returns>
        public String ProcessFile(BatSummary batSummary, string fileName, GpxHandler gpxHandler, int CurrentRecordingSessionId)
        {
            mBatSummary = batSummary;
            OutputString = "";
            if (fileName.ToUpper().EndsWith(".TXT"))
            {
                OutputString = ProcessLabelOrManualFile(fileName, gpxHandler, CurrentRecordingSessionId);
            }
            return (OutputString);
        }

        /// <summary>
        ///     Processes the manual file line.
        /// </summary>
        /// <param name="match">
        ///     The match.
        /// </param>
        /// <param name="bats">
        ///     The bats.
        /// </param>
        /// <returns>
        /// </returns>
        public string ProcessManualFileLine(Match match, out ObservableCollection<Bat> bats)
        {
            string comment = "";
            bats = new ObservableCollection<Bat>();

            if (match.Groups.Count >= 5)
            {
                string strStartOffset = match.Groups[1].Value;
                TimeSpan startTime = GetTimeOffset(strStartOffset);
                comment = comment + Tools.FormattedTimeSpan(startTime) + " - ";
                string strEndOffset = match.Groups[3].Value;
                TimeSpan endTime = GetTimeOffset(strEndOffset);
                comment = comment + Tools.FormattedTimeSpan(endTime) + " = ";
                TimeSpan thisDuration = endTime - startTime;
                comment = comment + Tools.FormattedTimeSpan(endTime - startTime) + " \t";
                for (int i = 4; i < match.Groups.Count; i++)
                {
                    comment = comment + match.Groups[i];
                }
                bats = AddToBatSummary(comment, thisDuration);
            }
            return (comment + "\n");
        }

        /// <summary>
        ///     Gets the time offset.
        /// </summary>
        /// <param name="strTime">
        ///     The string time.
        /// </param>
        /// <returns>
        /// </returns>
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

        /// <summary>
        ///     Gets the duration of the file.
        /// </summary>
        /// <param name="fileName">
        ///     Name of the file.
        /// </param>
        /// <param name="wavfile">
        ///     The wavfile.
        /// </param>
        /// <param name="fileStart">
        ///     The file start.
        /// </param>
        /// <param name="fileEnd">
        ///     The file end.
        /// </param>
        /// <returns>
        /// </returns>
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

        /*
        private ObservableCollection<BatSegmentLink> IdentifyBatPasses(int passes, ObservableCollection<Bat> bats)
        {
            ObservableCollection<BatSegmentLink> passList = new ObservableCollection<BatSegmentLink>();
            foreach (var bat in bats)
            {
                BatSegmentLink pass = new BatSegmentLink();
                pass.Bat = bat;
                pass.NumberOfPasses = passes;
                passList.Add(pass);
            }
            return (passList);
        }*/

        /// <summary>
        ///     Determines whether [is manual file line] [the specified line].
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <returns>
        /// </returns>
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
        ///     Processes the label file line.
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="startStr">
        ///     The start string.
        /// </param>
        /// <param name="endStr">
        ///     The end string.
        /// </param>
        /// <param name="comment">
        ///     The comment.
        /// </param>
        /// <param name="bats">
        ///     The bats.
        /// </param>
        /// <returns>
        /// </returns>
        private string ProcessLabelFileLine(string line, string startStr, string endStr, string comment, out ObservableCollection<Bat> bats)
        {
            string result = "";
            TimeSpan NewDuration;
            bats = new ObservableCollection<Bat>();

            if (!string.IsNullOrWhiteSpace(line) && char.IsDigit(line[0]))
            {
                result = ProcessLabelLine(line, startStr, endStr, comment, out NewDuration) + "\n";
                bats = AddToBatSummary(line, NewDuration);
            }
            else
            {
                result = line + "\n";
            }

            return (result);
        }

        /// <summary>
        ///     Processes the label line.
        /// </summary>
        /// <param name="line">
        ///     The line.
        /// </param>
        /// <param name="startStr">
        ///     The start string.
        /// </param>
        /// <param name="endStr">
        ///     The end string.
        /// </param>
        /// <param name="comment">
        ///     The comment.
        /// </param>
        /// <param name="NewDuration">
        ///     The new duration.
        /// </param>
        /// <returns>
        /// </returns>
        private string ProcessLabelLine(string line, string startStr, string endStr, string comment, out TimeSpan NewDuration)
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

            /*Regex regexSeconds = new Regex(@"([0-9]+\.[0-9]+)\s*-*\s*([0-9]+\.[0-9]+)\s*(.*)");
            Match match = Regex.Match(line, @"([0-9]+\.[0-9]+)\s*-*\s*([0-9]+\.[0-9]+)\s*(.*)");
            //MatchCollection allMatches = regexSeconds.Matches(line);
            if (match.Success)
            {*/
            double startTimeSeconds;
            double endTimeSeconds;
            Double.TryParse(startStr, out startTimeSeconds);
            Double.TryParse(endStr, out endTimeSeconds);

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
            shortened = comment;

            outLine = Tools.FormattedTimeSpan(StartTime) + " - " + Tools.FormattedTimeSpan(EndTime) + " = " +
                Tools.FormattedTimeSpan(duration) + "\t" + shortened;
            //outLine = String.Format("{0:00}\'{1:00}.{2:0##} - {3:00}\'{4:00}.{5:0##} = {6:00}\'{7:00}.{8:0##}\t{9}",
            //StartTime.Minutes, StartTime.Seconds, StartTime.Milliseconds,
            //EndTime.Minutes, EndTime.Seconds, EndTime.Milliseconds,
            //duration.Minutes, duration.Seconds, duration.Milliseconds, shortened);
            /*
            outLine = StartTime.Minutes + @"'" + StartTime.Seconds + "." + StartTime.Milliseconds +
            " - " + EndTime.Minutes + @"'" + EndTime.Seconds + "." + EndTime.Milliseconds +
            " = " + duration.Minutes + @"'" + duration.Seconds + "." + duration.Milliseconds +
            "\t" + shortened;*/
            /* }
             else
             {
                 StartTime = new TimeSpan();
                 EndTime = new TimeSpan();
                 outLine = line;
             }*/

            return (outLine);
        }

        /// <summary>
        ///     Processes a text file with a simple .txt extension that has been generated as an
        ///     Audacity LabelTrack. The fileName will be added to the output at the start of the OutputString.
        /// </summary>
        /// <param name="fileName">
        ///     Name of the file.
        /// </param>
        /// <param name="gpxHandler">
        ///     The GPX handler.
        /// </param>
        /// <param name="CurrentRecordingSessionId">
        ///     The current recording session identifier.
        /// </param>
        /// <returns>
        /// </returns>
        private string ProcessLabelOrManualFile(string fileName, GpxHandler gpxHandler, int CurrentRecordingSessionId)
        {
            ObservableCollection<SegmentAndBatList> ListOfsegmentAndBatLists = new ObservableCollection<SegmentAndBatList>();
            TimeSpan duration = new TimeSpan();
            Match match = null;
            mode = MODE.PROCESS;
            Recording recording = new Recording();
            recording.RecordingSessionId = CurrentRecordingSessionId;

            BatsFound = new Dictionary<string, BatStats>();
            string[] allLines = new string[1];
            DateTime fileStart;
            DateTime fileEnd;

            if (File.Exists(fileName))
            {
                string wavfile = "";
                duration = GetFileDuration(fileName, out wavfile, out fileStart, out fileEnd);
                recording.RecordingStartTime = TimeSpan.Parse(fileStart.ToShortTimeString());
                recording.RecordingEndTime = TimeSpan.Parse(fileEnd.ToShortTimeString());
                OutputString = fileName;
                if (!string.IsNullOrWhiteSpace(wavfile))
                {
                    OutputString = wavfile;
                    recording.RecordingName = wavfile.Substring(wavfile.LastIndexOf('\\'));
                }
                if (duration.Ticks > 0L)
                {
                    OutputString = OutputString + " \t" + duration.Minutes + "m" + duration.Seconds + "s";
                }
                OutputString = OutputString + "\n";
                ObservableCollection<decimal> gpsLocation = gpxHandler.GetLocation(fileStart);
                if (gpsLocation != null && gpsLocation.Count() == 2)
                {
                    OutputString = OutputString + gpsLocation[0] + ", " + gpsLocation[1];
                    recording.RecordingGPSLatitude = gpsLocation[0].ToString();
                    recording.RecordingGPSLongitude = gpsLocation[1].ToString();
                    if (recording.RecordingSession != null)
                    {
                        if (recording.RecordingSession.LocationGPSLatitude == null || recording.RecordingSession.LocationGPSLatitude < 5.0m)
                        {
                            recording.RecordingSession.LocationGPSLatitude = gpsLocation[0];
                            recording.RecordingSession.LocationGPSLongitude = gpsLocation[1];
                        }
                    }
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

                                linesToMerge = new ObservableCollection<String>();
                            }
                            if (!line.Contains("[COPY]") && !line.Contains("[MERGE]"))
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
                    if (linesToMerge != null && linesToMerge.Count > 0)
                    {
                        OutputString = OutputString + linesToMerge[0] + "\n";
                        linesToMerge.Remove(linesToMerge[0]);
                    }
                    foreach (var line in allLines)
                    {
                        if (!String.IsNullOrWhiteSpace(line))
                        {
                            string modline = Regex.Replace(line, @"[Ss][Tt][Aa][Rr][Tt]", "0.0");
                            modline = Regex.Replace(modline, @"[Ee][Nn][Dd]", ((decimal)(duration.TotalSeconds)).ToString());
                            string processedLine = "";
                            ObservableCollection<Bat> bats = new ObservableCollection<Bat>();
                            string startStr, endStr, comment;
                            if (FileProcessor.IsLabelFileLine(modline, out startStr, out endStr, out comment))
                            {
                                processedLine = ProcessLabelFileLine(modline, startStr, endStr, comment, out bats);
                            }
                            else if ((match = IsManualFileLine(modline)) != null)
                            {
                                processedLine = ProcessManualFileLine(match, out bats);
                            }
                            else
                            {
                                processedLine = line + "\n";
                            }
                            ListOfsegmentAndBatLists.Add(ProcessLabelledSegment(processedLine, bats) ?? new SegmentAndBatList());
                            // one added for each line that is processed as a segment label
                            OutputString = OutputString + processedLine;
                        }
                    }
                }
            }

            if (!String.IsNullOrWhiteSpace(OutputString) && BatsFound != null && BatsFound.Count() > 0)
            {
                foreach (var bat in BatsFound)
                {
                    bat.Value.batCommonName = bat.Key;
                    OutputString = OutputString + "\n" + Tools.GetFormattedBatStats(bat.Value, true);
                }
            }

            if (ListOfsegmentAndBatLists != null && ListOfsegmentAndBatLists.Count() > 0)
            {
                for (int i = ListOfsegmentAndBatLists.Count - 1; i >= 0; i--)
                {
                    if (String.IsNullOrWhiteSpace(ListOfsegmentAndBatLists[i].segment.Comment))
                    {
                        ListOfsegmentAndBatLists.RemoveAt(i);
                    }
                }
                DBAccess.UpdateRecording(recording, ListOfsegmentAndBatLists);
            }

            return (OutputString);
        }

        /*       /// <summary>
               /// using a string that matches the regex @"[0-9]+\.[0-9]+" or a string that matches
               /// the regex @"[0-9]+'?[0-9]*\.?[0-9]+" extracts one to three numeric portions and
               /// converts them to a timespan. 3 number represent minute,seconds,fraction 2 numbers
               /// represent seconds,fraction or minutes,seconds 1 number represents minutes or
               /// seconds </summary> <param name="match">The match.</param> <returns></returns>
               private static TimeSpan GetTimeOffset(Match match)
               {
                   return (FileProcessor.GetTimeOffset(match.Value));
               }*/
    }
}