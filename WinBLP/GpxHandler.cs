using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BatRecordingManager
{
    /// <summary>
    /// GpxHandler opens and processes a GPX location file if there is one
    /// inn the working directory.  It will either open the first .gpx file in
    /// the working directory or it will open a .gpx file  specified name.
    /// The contents are read into an XML structure and.
    /// A function is provided to allow an external process to pass a DateTime
    /// and the class returns a latitude and longitude for the time closest
    /// to the spcified time.
    /// </summary>
    internal class GpxHandler
    {
        /// <summary>
        /// The GPX data
        /// </summary>
        private XDocument GPXData;

        /// <summary>
        /// The GPX namespace
        /// </summary>
        private XNamespace gpxNamespace;

        /// <summary>
        /// The GPX file exists
        /// </summary>
        private bool GPXFileExists = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="GpxHandler"/> class.
        /// </summary>
        /// <param name="Location">The location.</param>
        public GpxHandler(string Location)
        {
            string filename = "";
            GPXFileExists = false;
            //GPXData = new XDocument();
            //GPXData.Add(XElement.Parse("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"no\" ?>"));
            if (Location.ToUpper().EndsWith(".GPX"))
            {
                if (File.Exists(Location))
                {
                    filename = Location;
                    GPXFileExists = true;
                    //GPXData.Add(XElement.Load(Location));
                }
            }
            else
            {
                if (Directory.Exists(Location))
                {
                    var gpxFileList = Directory.EnumerateFiles(Location, "*.gpx");
                    if (gpxFileList != null && gpxFileList.Count() > 0)
                    {
                        filename = gpxFileList.FirstOrDefault();
                        GPXFileExists = true;
                        //GPXData.Add(XElement.Load(gpxFileList.FirstOrDefault()));
                    }
                }
            }
            if (!String.IsNullOrWhiteSpace(filename) && File.Exists(filename))
            {
                try
                {
                    GPXData = new XDocument(
                        new XDeclaration("1.0", "UTF-8", "no"),
                        XElement.Load(filename)
                        );
                }
                catch (Exception)
                {
                    GPXFileExists = false;
                }
            }
            if (GPXFileExists)
            {
                gpxNamespace = GetGpxNameSpace();
            }
        }

        /// <summary>
        /// Load the namespace for a standard GPX document
        /// </summary>
        /// <returns></returns>
        private XNamespace GetGpxNameSpace()
        {
            XNamespace gpx = XNamespace.Get("http://www.topografix.com/GPX/1/0");
            return gpx;
        }

        /// <summary>
        /// Gets the location.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        public List<decimal> GetLocation(DateTime time)
        {
            List<decimal> result = new List<decimal>();
            if (GPXFileExists && GPXData != null)
            {
                if (time.Ticks == 0L)
                {
                    return (new List<decimal>());
                }

                DateTime UTCTime = time.ToUniversalTime();

                XElement previous = null;
                var all = GPXData.Descendants();
                var trackPoints = GPXData.Descendants(gpxNamespace + "trkpt");

                Debug.WriteLine(trackPoints.Count());
                Debug.WriteLine(trackPoints.First().Value);
                //var trackPoints =
                //    from tp in GPXData.Descendants("trk")
                //   select (tp.Value);
                if (trackPoints != null && trackPoints.Count() > 0)
                {
                    foreach (var trkpt in trackPoints)
                    {
                        if (TrackPointIsEarlier(UTCTime, trkpt))
                        {
                            previous = trkpt;
                            continue;
                        }
                        if (previous == null)
                        {
                            result = GetGPSCoordinates(trkpt);
                            return (result);
                        }
                        TimeSpan offsetToPrevious = GetOffset(previous, UTCTime);
                        TimeSpan offsetToNext = GetOffset(trkpt, UTCTime);
                        if (offsetToNext <= offsetToPrevious)
                        {
                            result = GetGPSCoordinates(trkpt);
                        }
                        else
                        {
                            result = GetGPSCoordinates(previous);
                        }
                        break;
                    }
                }
            }

            return (result);
        }

        /// <summary>
        /// Gets the offset.
        /// </summary>
        /// <param name="TrackPoint">The track point.</param>
        /// <param name="UTCTime">The UTC time.</param>
        /// <returns></returns>
        private TimeSpan GetOffset(XElement TrackPoint, DateTime UTCTime)
        {
            DateTime TrackPointTime = GetTrackPointTime(TrackPoint);
            return ((TrackPointTime - UTCTime).Duration());
        }

        /// <summary>
        /// Gets the track point time.
        /// </summary>
        /// <param name="TrackPoint">The track point.</param>
        /// <returns></returns>
        private DateTime GetTrackPointTime(XElement TrackPoint)
        {
            String strDateTimeElement = TrackPoint.Descendants(gpxNamespace + "time").First().Value;
            DateTime tpTime = DateTime.Parse(strDateTimeElement);
            return (tpTime);
        }

        /// <summary>
        /// Gets the GPS coordinates.
        /// </summary>
        /// <param name="trkpt">The TRKPT.</param>
        /// <returns></returns>
        private List<decimal> GetGPSCoordinates(XElement trkpt)
        {
            string strLat = trkpt.Attribute("lat").Value;
            string strLong = trkpt.Attribute("lon").Value;
            decimal dLat;
            decimal dLong;
            Decimal.TryParse(strLat, out dLat);
            Decimal.TryParse(strLong, out dLong);
            List<decimal> result = new List<decimal>();
            result.Add(dLat);
            result.Add(dLong);
            return (result);
        }

        /// <summary>
        /// Tracks the point is earlier.
        /// </summary>
        /// <param name="UTCTime">The UTC time.</param>
        /// <param name="trkpt">The TRKPT.</param>
        /// <returns></returns>
        private bool TrackPointIsEarlier(DateTime UTCTime, XElement trkpt)
        {
            DateTime TrackPointTime = GetTrackPointTime(trkpt).ToUniversalTime();
            if (TrackPointTime < UTCTime) return (true);
            return (false);
        }
    }
}