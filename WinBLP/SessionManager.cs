using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BatRecordingManager
{
    public static class SessionManager
    {
        internal static string GetSessionTag(FileBrowser fileBrowser)
        {
            String result = "";
            if (!String.IsNullOrWhiteSpace(fileBrowser.WorkingFolder))
            {
                Regex tagRegex = new Regex("-[a-zA-Z0-9-]+_+[0-9-]+");
                Match match = tagRegex.Match(fileBrowser.WorkingFolder);
                if (match.Success)
                {
                    result = match.Value.Substring(1); // remove the surplus leading hyphen
                }
            }
            return (result);
        }

        internal static RecordingSession PopulateSession(RecordingSession newSession, FileBrowser fileBrowser)
        {
            newSession.SessionTag = SessionManager.GetSessionTag(fileBrowser);
            RecordingSession existingSession = DBAccess.GetRecordingSession(newSession.SessionTag);
            if (existingSession != null)
            {
                return (existingSession);
            }
            else
            {
                String[] headerFileLines = fileBrowser.GetHeaderFile();
                if (headerFileLines != null)
                {
                    newSession = SessionManager.ExtractHeaderData(newSession.SessionTag, headerFileLines);
                }
            }

            return (newSession);
        }

        /// <summary>
        /// Extracts the header data.  Makes a best guess attempt to populate
        /// a RecordingSession instance from a header file.
        /// </summary>
        /// <param name="sessionTag">The session tag.</param>
        /// <param name="headerFile">The header file.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static RecordingSession ExtractHeaderData(string sessionTag, string[] headerFile)
        {
            RecordingSession session = new RecordingSession();
            session.SessionTag = sessionTag;
            session.SessionDate = SessionManager.GetDate(headerFile, sessionTag);
            TimeSpan StartTime = new TimeSpan();
            TimeSpan EndTime = new TimeSpan();
            SessionManager.GetTimes(headerFile, out StartTime, out EndTime);
            session.SessionStartTime = StartTime;
            session.SessionEndTime = EndTime;
            session.Temp = SessionManager.GetTemp(headerFile);
            session.Equipment = SessionManager.GetEquipment(headerFile);
            session.Microphone = SessionManager.GetMicrophone(headerFile);
            session.Operator = SessionManager.GetOperator(headerFile);
            session.Location = SessionManager.GetLocation(headerFile);
            decimal? Longitude = 0m; ;
            decimal? Latitude = 52m;
            SessionManager.GetGPSCoOrdinates(headerFile, out Latitude, out Longitude);
            session.LocationGPSLongitude = Longitude;
            session.LocationGPSLatitude = Latitude;
            session.SessionNotes = "";
            foreach (var line in headerFile)
            {
                session.SessionNotes = session.SessionNotes + line + "\n";
            }
            return (session);
        }

        private static string GetEquipment(string[] headerFile)
        {
            List<string> knownEquipment = DBAccess.GetOperators();
            // get a line in the text containing a known operator
            var matchingEquipment = headerFile.Where(line => knownEquipment.Any(txt => line.ToUpper().Contains(txt.ToUpper())));
            if (matchingEquipment != null && matchingEquipment.Count() > 0)
            {
                return (matchingEquipment.First());
            }
            return ("");
        }

        private static string GetMicrophone(string[] headerFile)
        {
            List<string> knownMicrophones = DBAccess.GetOperators();
            // get a line in the text containing a known operator
            var mm = from line in headerFile
                     join mic in knownMicrophones on line equals mic
                     select mic;

            var matchingMicrophones = headerFile.Where(line => knownMicrophones.Any(txt => line.ToUpper().Contains(txt.ToUpper())));
            if (matchingMicrophones != null && matchingMicrophones.Count() > 0)
            {
                return (matchingMicrophones.First());
            }
            return ("");
        }

        private static void GetGPSCoOrdinates(string[] headerFile, out decimal? latitude, out decimal? longitude)
        {
            //Regex gpsRegex = new Regex();
            latitude = 0.0m;
            longitude = 0.0m;
            string pattern = @"(-?[0-9]{1,2}\.[0-9]{1,6})\s*,\s*(-?[0-9]{1,2}\.[0-9]{1,6})";
            foreach (var line in headerFile)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    decimal value = 0.0m;
                    decimal.TryParse(match.Groups[0].Value, out value);
                    latitude = value;

                    value = 0.0m;
                    decimal.TryParse(match.Groups[1].Value, out value);

                    longitude = value;
                    break;
                }
            }
        }

        private static string GetLocation(string[] headerFile)
        {
            if (headerFile.Count() > 1)
            {
                return (headerFile[1]);
            }
            return ("");
        }

        private static string GetOperator(string[] headerFile)
        {
            List<string> knownOperators = DBAccess.GetOperators();
            // get a line in the text containing a known operator
            var matchingOperators = headerFile.Where(line => knownOperators.Any(txt => line.ToUpper().Contains(txt.ToUpper())));
            if (matchingOperators != null && matchingOperators.Count() > 0)
            {
                return (matchingOperators.First());
            }
            return ("");
        }

        private static short? GetTemp(string[] headerFile)
        {
            short temp = 0;
            string pattern = @"([0-9]{1,2})\s*[C\u00B0]{1}";
            foreach (var line in headerFile)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    short.TryParse(match.Groups[1].Value, out temp);
                    break;
                }
            }
            return (temp);
        }

        private static void GetTimes(string[] headerFile, out TimeSpan startTime, out TimeSpan endTime)
        {
            startTime = new TimeSpan();
            endTime = new TimeSpan();
            List<TimeSpan> times = new List<TimeSpan>();
            string pattern = @"[0-9]{1,2}:[0-9]{2}:{0,1}[0-9]{0,2}";
            foreach (var line in headerFile)
            {
                foreach (Match match in Regex.Matches(line, pattern))
                {
                    TimeSpan ts = new TimeSpan();
                    if (TimeSpan.TryParse(match.Value, out ts))
                    {
                        times.Add(ts);
                    }
                }
                if (times.Count >= 2)
                {
                    startTime = times[0];
                    endTime = times[1];
                    break;// out of lines in headerFile
                }
            }
        }

        private static DateTime GetDate(string[] headerFile, string sessionTag)
        {
            if (!String.IsNullOrWhiteSpace(sessionTag))
            {
                return (GetDateFromTag(sessionTag));
            }
            DateTime result = new DateTime();
            string pattern = @"[0-9]+\s*[a-zA-Z]+\s*(20){0,1}[0-9]{2}";
            foreach (var line in headerFile)
            {
                var match = Regex.Match(line, pattern);
                if (match.Success)
                {
                    DateTime.TryParse(match.Value, out result);
                    break;
                }
            }
            return (result);
        }

        private static DateTime GetDateFromTag(string sessionTag)
        {
            Regex tagRegex = new Regex("([a-zA-Z0-9-]+)(_+)([0-9]{4})([0-9]{2})([0-9]{2})");
            DateTime result = new DateTime();
            Match match = tagRegex.Match(sessionTag);
            if (match.Success)
            {
                if (match.Groups.Count == 6)
                {
                    int day;
                    int month;
                    int year;

                    int.TryParse(match.Groups[5].Value, out day);
                    int.TryParse(match.Groups[4].Value, out month);
                    int.TryParse(match.Groups[3].Value, out year);
                    result = new DateTime(year, month, day);
                }
            }

            return (result);
        }
    }
}