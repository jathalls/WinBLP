using System;

namespace BatRecordingManager
{
    /// <summary>
    /// Class of miscellaneous, multi access functions - all static for ease of re-use
    /// </summary>
    public static class Tools
    {
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

        /// <summary>
        /// Formats the time span.
        /// Given a Timespan returns a formatted string as
        /// mm'ss.sss" or 23h59'58.765"
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        public static string FormattedTimeSpan(TimeSpan time)
        {
            /// <summary>
            /// Formatteds the time span.
            /// </summary>
            /// <param name="time">The time.</param>
            /// <returns></returns>

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

        internal static int GetNumberOfPassesForSegment(LabelledSegment segment)
        {
            BatStats stat = new BatStats();
            stat.Add(segment.EndOffset - segment.StartOffset);
            return (stat.passes);
        }
    }
}