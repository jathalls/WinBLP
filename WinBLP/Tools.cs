using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;

namespace BatRecordingManager
{
    /// <summary>
    ///     Class of miscellaneous, multi access functions - all static for ease of re-use
    /// </summary>
    public static class Tools
    {
        /// <summary>
        ///     Formats the time span. Given a Timespan returns a formatted string as mm'ss.sss" or 23h59'58.765"
        /// </summary>
        /// <param name="time">
        ///     The time.
        /// </param>
        /// <returns>
        ///     </returns>
        public static string FormattedTimeSpan(TimeSpan time)
        {
            string result = "";
            if (time != null && time.Ticks >= 0)
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
        ///     To the formatted string.
        /// </summary>
        /// <param name="session">
        ///     The session.
        /// </param>
        /// <returns>
        ///     </returns>
        public static String ToFormattedString(this RecordingSession session)
        {
            String result = "";
            result += session.SessionTag + "\n";
            result += session.Location + "\n";
            result += session.SessionDate.ToShortDateString() + " " +
                session.SessionStartTime ?? "" + " - " +
                session.SessionEndTime ?? "" + "\n";
            result += session.Operator ?? "" + "\n";
            result += (session.LocationGPSLatitude ?? 0.0m) + ", " + (session.LocationGPSLongitude ?? 0.0m) + "\n";
            result += session.Equipment ?? "" + "\n";
            result += session.Microphone ?? "" + "\n";
            result += session.SessionNotes ?? "" + "\n";
            result += "==================================================================\n";

            return (result);
        }

        /// <summary>
        ///     Converts the double in seconds to time span.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <returns>
        ///     </returns>
        internal static TimeSpan ConvertDoubleToTimeSpan(double? value)
        {
            if (value == null) return new TimeSpan();
            int seconds = (int)Math.Floor(value.Value);
            int millis = (int)Math.Round((value.Value - seconds) * 1000.0d);

            int minutes = Math.DivRem(seconds, 60, out seconds);
            return (new TimeSpan(0, 0, minutes, seconds, millis));
        }

        /// <summary>
        ///     Given a valid Segment, generates a formatted string in the format mm'ss.ss" - mm'ss.ss"
        ///     = mm'ss.ss" comment
        /// </summary>
        /// <param name="segment">
        ///     The segment.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static string FormattedSegmentLine(LabelledSegment segment)
        {
            if (segment == null) return ("");
            String result = Tools.FormattedTimeSpan(segment.StartOffset) + " - " +
                Tools.FormattedTimeSpan(segment.EndOffset) + " = " +
                Tools.FormattedTimeSpan(segment.EndOffset - segment.StartOffset) + " " +
                segment.Comment;
            return (result);
        }

        internal static string GetFormattedBatStats(BatStats value, bool showNoBats)
        {
            string result = "";
            if (value == null) return (result);

            if (value.batCommonName.ToUpper() == "NO BATS" || value.batCommonName.ToUpper() == "NOBATS")
            {
                if (showNoBats)
                {
                    return ("No Bats");
                }
                else
                {
                    return ("");
                }
            }
            if (value.passes > 0 || value.segments > 0)
            {
                result = value.batCommonName + " " + value.passes + (value.passes == 1 ? " pass in " : " passes in ") +
                    value.segments + " segment" + (value.segments != 1 ? "s" : "") +

                            " = ( " +
                            "Min=" + Tools.FormattedTimeSpan(value.minDuration) +
                            ", Max=" + Tools.FormattedTimeSpan(value.maxDuration) +
                            ", Mean=" + Tools.FormattedTimeSpan(value.meanDuration) + " )" +
                            "Total duration=" + Tools.FormattedTimeSpan(value.totalDuration);
            }
            return (result);
        }

        internal static int GetNumberOfPassesForSegment(LabelledSegment segment)
        {
            BatStats stat = new BatStats();
            stat.Add(segment.EndOffset - segment.StartOffset);
            return (stat.passes);
        }

        internal static SegmentAndBatList Parse(String segmentLine)
        {
            ObservableCollection<Bat> bats = DBAccess.GetSortedBatList();
            var result = FileProcessor.ProcessLabelledSegment(segmentLine, bats);
            return (result);
        }

        /// <summary>
        ///     Parses a line in the format 00'00.00 into a TimeSpan the original strting has been
        ///     matched by a Regex of the form [0-9\.\']+
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static TimeSpan TimeParse(string value)
        {
            TimeSpan ts = new TimeSpan();
            String[] separators = { "\'", ".", "\"" };
            String[] numbers = value.Split(separators, StringSplitOptions.None);
            if (numbers.Count() == 3)
            {
                int mins = 0;
                int.TryParse(numbers[0], out mins);
                int secs = 0;
                int.TryParse(numbers[1], out secs);
                int millis = 0;
                while (numbers[2].Length > 3)
                {
                    numbers[2] = numbers[2].Substring(0, 3);
                }
                while (numbers[2].Length < 3)
                {
                    numbers[2] = numbers[2] + "0";
                }
                int.TryParse(numbers[2], out millis);
                ts = new TimeSpan(0, 0, mins, secs, millis);
            }
            else if (numbers.Count() == 2)
            {
                int secs = 0;
                int.TryParse(numbers[0], out secs);
                while (numbers[1].Length > 3)
                {
                    numbers[1] = numbers[1].Substring(0, 3);
                }
                while (numbers[1].Length < 3)
                {
                    numbers[1] = numbers[1] + "0";
                }
                int millis = 0;
                int.TryParse(numbers[1], out millis);
                ts = new TimeSpan(0, 0, 0, secs, millis);
            }
            else if (numbers.Count() == 1)
            {
                int secs = 0;
                int.TryParse(numbers[0], out secs);
                ts = new TimeSpan(0, 0, 0, secs, 0);
            }

            return (ts);
        }
    }

    #region TimeSpanDateConverter (ValueConverter)

    /// <summary>
    ///     </summary>
    public class TimeSpanDateConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null) return (DateTime.Now);
                TimeSpan time = (value as TimeSpan?) ?? new TimeSpan();
                DateTime result = new DateTime(time.Ticks);
                return (result);
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TimeSpan result = new TimeSpan(((value as DateTime?) ?? DateTime.Now).Ticks);
            return (result);
        }
    }

    #endregion TimeSpanDateConverter (ValueConverter)

    #region SegmentToTextConverter (ValueConverter)

    /// <summary>
    ///     </summary>
    public class SegmentToTextConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value == null) return ("");
                LabelledSegment segment = value as LabelledSegment;
                String result = Tools.FormattedTimeSpan(segment.StartOffset) + " - " + Tools.FormattedTimeSpan(segment.EndOffset) +
                    "  " + segment.Comment;
                return result;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            String text = value as String;
            LabelledSegment modifiedSegment = new LabelledSegment();
            modifiedSegment.Comment = text;

            return modifiedSegment;
        }
    }

    #endregion SegmentToTextConverter (ValueConverter)

    #region ShortDateConverter (ValueConverter)

    /// <summary>
    ///     </summary>
    public class ShortDateConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                DateTime dateToDisplay = (value as DateTime?) ?? DateTime.Now;
                return dateToDisplay.ToShortDateString();
            }
            catch
            {
                return value.ToString();
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        ///     </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = value as String;
            DateTime result = new DateTime();
            DateTime.TryParse(text, out result);
            return result;
        }
    }

    #endregion ShortDateConverter (ValueConverter)
}