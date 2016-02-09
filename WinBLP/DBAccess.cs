using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BatRecordingManager
{
    /// <summary>
    ///     Static class of Database interface functions
    /// </summary>
    public static class DBAccess
    {
        /// <summary>
        ///     Deletes the recording supplied as a parameter and all LabelledSegments related to
        ///     that recording.
        /// </summary>
        /// <param name="recording">
        ///     The recording.
        /// </param>
        public static String DeleteRecording(Recording recording)
        {
            String result = null;
            if (recording != null && recording.Id > 0)
            {
                /*
                if(recording.LabelledSegments!=null && recording.LabelledSegments.Count() > 0)
                {
                    foreach(var segment in recording.LabelledSegments)
                    {
                        DBAccess.DeleteSegment(segment);
                    }
                }*/
                try
                {
                    BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
                    DBAccess.DeleteAllSegmentsInRecording(recording, dc);
                    var recordingToDelete = (from rec in dc.Recordings
                                             where rec.Id == recording.Id
                                             select rec).Single();
                    dc.Recordings.DeleteOnSubmit(recordingToDelete);
                    dc.SubmitChanges();
                }
                catch (Exception ex)
                {
                    result = "Error deleting recording:- " + ex.Message;
                }
            }
            return (result);
        }

        /// <summary>
        ///     Gets the matching bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <returns>
        ///     </returns>
        public static Bat GetMatchingBat(Bat bat)
        {
            BatReferenceDBLinqDataContext dataContext = DBAccess.GetDataContext();
            return (GetMatchingBat(bat, dataContext));
        }

        /// <summary>
        ///     Gets the named bat. Returns the bat identified by a specified Name or null if the
        ///     bat does not exist in the database
        /// </summary>
        /// <param name="newBatName">
        ///     New name of the bat.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="NotImplementedException">
        ///     </exception>
        public static Bat GetNamedBat(string newBatName)
        {
            BatReferenceDBLinqDataContext dataContext = DBAccess.GetDataContext();
            return (GetNamedBat(newBatName, dataContext));
        }

        /// <summary>
        ///     Returns a list of all known bats sorted on SortOrder
        /// </summary>
        /// <returns>
        ///     </returns>
        public static ObservableCollection<Bat> GetSortedBatList()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var result = (from bat in dc.Bats
                          orderby bat.SortIndex
                          select bat).Distinct();
            return (new ObservableCollection<Bat>(result));
        }

        /// <summary>
        ///     Inserts the bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <returns>
        ///     </returns>
        public static string InsertBat(Bat bat)
        {
            BatReferenceDBLinqDataContext dataContext = DBAccess.GetDataContext();
            return (InsertBat(bat, dataContext));
        }

        /// <summary>
        ///     Merges the bat. The supplied bat is either inserted into the database if it is not
        ///     already there, or, if a bat with the same genus and species is present then the data
        ///     in this bat is added to and merged with the data for the existing bat. Sort orders
        ///     are taken from the new bat and duplicate tags or common names are removed, otherwise
        ///     any tag or common name differing ins pelling or capitalization will be treated as a
        ///     new item. Existing tags or common names which do not exist in the new bat will be
        ///     removed. Notes from the new bat will replace notes in the existing bat. The bat
        ///     'name' will be updated to reflect the common name with the lowest sort index.
        ///     Returns a suitable error message if the process failed, or an empty string if the
        ///     process was successful;
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <returns>
        ///     </returns>
        public static string MergeBat(Bat bat)
        {
            string result = DBAccess.ValidateBat(bat);
            if (!String.IsNullOrWhiteSpace(result))
            {
                return (result); // bat is not suitable for merging or insertion
            }
            BatReferenceDBLinqDataContext dataContext = DBAccess.GetDataContext();
            Bat existingBat = GetMatchingBat(bat, dataContext);
            if (existingBat == null)
            {
                return (InsertBat(bat));
            }
            else
            {
                MergeTags(existingBat, bat, dataContext);
                existingBat.Notes = bat.Notes;
                existingBat.Name = bat.Name;
                existingBat.Batgenus = bat.Batgenus;
                existingBat.BatSpecies = bat.BatSpecies;
                existingBat.SortIndex = bat.SortIndex;
                dataContext.SubmitChanges();
            }

            return (result);
        }

        /// <summary>
        ///     Updates the labelled segments. Given an entity set of LabelledSegments (from a
        ///     Recording instance) updates the full set in the database, inserting where necessary
        ///     and parsing the comments for bat names and Updating all BatSegment links as required.
        /// </summary>
        /// <param name="labelledSegments">
        ///     The labelled segments.
        /// </param>
        /// <param name="recording">
        ///     The recording.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        public static void UpdateLabelledSegments(EntitySet<LabelledSegment> labelledSegments, Recording recording, BatReferenceDBLinqDataContext dc)
        {
            if (dc == null)
            {
                dc = DBAccess.GetDataContext();
            }

            // if no segments, delete all existing segments for this recording id
            DBAccess.DeleteAllSegmentsInRecording(recording, dc);
            if (labelledSegments == null || labelledSegments.Count <= 0)
            {
                return;
            }
            else
            {// we do have some segments to update
                var bats = DBAccess.GetSortedBatList();
                foreach (var seg in labelledSegments)
                {
                    SegmentAndBatList segBatList = FileProcessor.ProcessLabelledSegment(Tools.FormattedSegmentLine(seg), bats);
                    DBAccess.UpdateLabelledSegment(segBatList, recording.Id, dc);
                }
            }
        }

        /// <summary>
        ///     Validates the bat. Checkes to see if the required fields exist and are valid in
        ///     format and if so returns an empty string. Otherwise returns a string identifying
        ///     which fields are missing or incorrect.
        /// </summary>
        /// <param name="newBat">
        ///     The new bat.
        /// </param>
        /// <returns>
        ///     </returns>
        public static string ValidateBat(Bat newBat)
        {
            String message = "";
            if (String.IsNullOrWhiteSpace(newBat.Name))
            {
                message = message + "Common Name required\n";
            }

            if (String.IsNullOrWhiteSpace(newBat.BatSpecies))
            {
                message = message + "Bat species required; use \'sp.\' if not known\n";
            }

            if (String.IsNullOrWhiteSpace(newBat.Batgenus))
            {
                message = message + "Bat genus required; use \'unknown\' if not known\n";
            }
            if (newBat.BatTags == null || newBat.BatTags.Count() <= 0)
            {
                message = message + "At least one tag is requuired";
            }

            return (message);
        }

        /// <summary>
        /// Validates the recording.  Confirms that the recording structure
        ///contains valid and complete data, or returns a suitable
        /// and informative error message.
        ///
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static String ValidateRecording(Recording recording)
        {
            String result = "";
            if (String.IsNullOrWhiteSpace(recording.RecordingName))
            {
                return ("Recording Name (.wav file name) is required");
            }
            if (!recording.RecordingName.ToUpper().EndsWith(".WAV"))
            {
                return ("Recording file must be of type .wav");
            }
            if (recording.RecordingEndTime < recording.RecordingStartTime)
            {
                return ("Recording cannot end before it has begun");
            }

            return (result);
        }

        /// <summary>
        ///     Adds the tag.
        /// </summary>
        /// <param name="tagText">
        ///     The tag text.
        /// </param>
        /// <param name="BatID">
        ///     The bat identifier.
        /// </param>
        /// <returns>
        ///     </returns>
        internal static int AddTag(string tagText, int BatID)
        {
            if (String.IsNullOrWhiteSpace(tagText)) return (-1);
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            BatTag newTag = new BatTag();
            newTag.SortIndex = 0;
            newTag.BatTag1 = tagText;
            var BatForTag = (from bat in dc.Bats
                             where bat.Id == BatID
                             select bat).Single();
            BatForTag.BatTags.Add(newTag);
            dc.SubmitChanges();
            return (ResequenceTags(newTag, dc));
        }

        /// <summary>
        ///     Deletes the bat passed as a parameter, and re-indexxes the sort order
        /// </summary>
        /// <param name="selectedBat">
        ///     The selected bat.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static void DeleteBat(Bat selectedBat)
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            if (selectedBat.Id > 0)
            {
                try
                {
                    var bat = (from b in dc.Bats
                               where b.Id == selectedBat.Id
                               select b).Single();
                    var tags = from t in dc.BatTags
                               where t.BatID == selectedBat.Id
                               select t;
                    dc.BatTags.DeleteAllOnSubmit(tags);

                    dc.Bats.DeleteOnSubmit(bat);
                    dc.SubmitChanges();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Error deleting Bat:- " + ex.Message);
                }
            }

            DBAccess.ResequenceBats();
        }

        /// <summary>
        ///     Deletes the segment provided as a parameter and identified by it's Id.
        /// </summary>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static void DeleteSegment(LabelledSegment segment)
        {
            if (segment != null && segment.Id > 0)
            {
                LabelledSegment segmentToDelete;
                BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
                DBAccess.DeleteLinksForSegmentId(segment.Id, dc);
                try
                {
                    segmentToDelete = (from seg in dc.LabelledSegments
                                       where seg.Id == segment.Id
                                       select seg).Single();
                }
                catch (Exception)
                {
                    return;
                }
                dc.LabelledSegments.DeleteOnSubmit(segmentToDelete);
                dc.SubmitChanges();
            }
        }

        /// <summary>
        ///     Deletes the session provided as a parameter and identified by the Id. All related
        ///     recordings are also deleted.
        /// </summary>
        /// <param name="session">
        ///     The session.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static void DeleteSession(RecordingSession session)
        {
            if (session != null && session.Id > 0)
            {
                /*
                if(session.Recordings!=null && session.Recordings.Count()>0)
                foreach(var recording in session.Recordings)
                {
                    DBAccess.DeleteRecording(recording);
                }*/
                BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
                DBAccess.DeleteAllRecordingsInSession(session, dc);
                var sessionsToDelete = (from rec in dc.RecordingSessions
                                        where rec.Id == session.Id
                                        select rec).Single();
                dc.RecordingSessions.DeleteOnSubmit(sessionsToDelete);
                dc.SubmitChanges();
            }
        }

        /// <summary>
        ///     Deletes the tag.
        /// </summary>
        /// <param name="tag">
        ///     The tag.
        /// </param>
        internal static void DeleteTag(BatTag tag)
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            BatTag tagToDelete = (from tg in dc.BatTags
                                  where tg.Id == tag.Id
                                  select tg).Single<BatTag>();
            dc.BatTags.DeleteOnSubmit(tagToDelete);
            dc.SubmitChanges();
            ResequenceTags(tag, dc);
        }

        internal static BatStatistics GetBatSessionStatistics(int BatId, int SessionId)
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();

            return (GetBatSessionStatistics(BatId, SessionId, dc));
        }

        /// <summary>
        ///     Gets the bat session statistics.
        /// </summary>
        /// <param name="BatId">
        ///     The bat identifier.
        /// </param>
        /// <param name="SessionId">
        ///     The session identifier.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <returns>
        ///     </returns>
        internal static BatStatistics GetBatSessionStatistics(int BatId, int SessionId, BatReferenceDBLinqDataContext dc)
        {
            BatStatistics thisBatStats = new BatStatistics();

            thisBatStats.stats = new BatStats();
            var relevantSegments = from bsl in dc.BatSegmentLinks
                                   where bsl.BatID == BatId && bsl.LabelledSegment.Recording.RecordingSessionId == SessionId
                                   select bsl.LabelledSegment;
            if (relevantSegments != null && relevantSegments.Count() > 0)
            {
                foreach (var seg in relevantSegments)
                {
                    thisBatStats.stats.Add(seg.EndOffset - seg.StartOffset);
                }
            }

            var relevantRecordings = (from seg in relevantSegments
                                      select seg.Recording).Distinct();
            thisBatStats.recordings = new ObservableCollection<Recording>(relevantRecordings);

            var relevantSessions = (from sess in dc.RecordingSessions
                                    where sess.Id == SessionId
                                    select sess).Distinct();
            thisBatStats.sessions = new ObservableCollection<RecordingSession>(relevantSessions);

            Bat thisBat = (from bat in dc.Bats
                           where bat.Id == BatId
                           select bat).Single();

            thisBatStats.Name = thisBat.Name;
            thisBatStats.Genus = thisBat.Batgenus;
            thisBatStats.Species = thisBat.BatSpecies;

            return (thisBatStats);
        }

        /// <summary>
        ///     Gets the bat statistics.
        /// </summary>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static ObservableCollection<BatStatistics> GetBatStatistics()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            ObservableCollection<BatStatistics> result = new ObservableCollection<BatStatistics>();

            var allBats = DBAccess.GetSortedBatList();
            if (allBats != null && allBats.Count > 0)
            {
                foreach (var bat in allBats)
                {
                    BatStatistics thisBatStats = new BatStatistics();
                    thisBatStats.Name = bat.Name;
                    thisBatStats.Genus = bat.Batgenus;
                    thisBatStats.Species = bat.BatSpecies;
                    thisBatStats.sessions = DBAccess.GetSessionsForBat(bat, dc);
                    thisBatStats.recordings = DBAccess.GetRecordingsForBat(bat, dc);
                    thisBatStats.stats = DBAccess.GetPassesForBat(bat, dc);
                    result.Add(thisBatStats);
                }
            }
            return (result);
        }

        /// <summary>
        ///     Gets the blank bat.
        /// </summary>
        /// <returns>
        ///     </returns>
        internal static Bat GetBlankBat()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var batlist = from bat in dc.Bats
                          where bat.Name == "No Bats"
                          select bat;
            if (batlist == null || batlist.Count() <= 0)
            {
                Bat noBat = new Bat();
                noBat.Name = "No Bats";

                BatTag tag = new BatTag();
                tag.BatTag1 = "No Bats";
                tag.SortIndex = 1;
                noBat.BatTags.Add(tag);
                noBat.BatSpecies = "sp.";
                noBat.Batgenus = "Unknown";
                noBat.SortIndex = int.MaxValue;
                dc.Bats.InsertOnSubmit(noBat);
                dc.SubmitChanges();
                return (noBat);
            }

            return (batlist.First());
        }

        /// <summary>
        ///     Gets the data context.
        /// </summary>
        /// <returns>
        ///     </returns>
        internal static BatReferenceDBLinqDataContext GetDataContext()
        {
            string DBFileName = "BatReferenceDB.mdf";

            String workingDatabaseLocation = DBAccess.GetWorkingDatabaseLocation();

            BatReferenceDBLinqDataContext batReferenceDataContext = new BatReferenceDBLinqDataContext(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + workingDatabaseLocation + DBFileName + @";Integrated Security=False;Connect Timeout=30");
            if (batReferenceDataContext == null)
            {
                return (new BatReferenceDBLinqDataContext());
            }

            return (batReferenceDataContext);
        }

        /// <summary>
        ///     Gets the described bats.
        /// </summary>
        /// <param name="description">
        ///     The description.
        /// </param>
        /// <returns>
        ///     </returns>
        internal static ObservableCollection<Bat> GetDescribedBats(string description)
        {
            ObservableCollection<Bat> matchingBats = new ObservableCollection<Bat>();
            if (String.IsNullOrWhiteSpace(description))
            {
                Bat nobat = DBAccess.GetBlankBat();
                matchingBats.Add(nobat);
                return (matchingBats);
            }
            BatReferenceDBLinqDataContext batReferenceDataContext = DBAccess.GetDataContext();

            if (batReferenceDataContext == null) return (null);

            foreach (var tag in batReferenceDataContext.BatTags)
            {
                if (!tag.BatTag1.ToUpper().StartsWith(tag.BatTag1)) // if the tag is not all upper case
                {
                    if (description.ToUpper().Contains(tag.BatTag1.ToUpper())) // match regardless of case
                    {
                        matchingBats.Add(tag.Bat);
                    }
                }
                else // tag is all upper case
                {
                    if (description.Contains(tag.BatTag1)) // match has to match case too
                    {
                        matchingBats.Add(tag.Bat);
                    }
                }
            }
            if (matchingBats.Count() <= 0)
            {
                matchingBats.Add(DBAccess.GetUnknownBat());
            }
            return (new ObservableCollection<Bat>(matchingBats.Distinct()));
        }

        /// <summary>
        ///     Gets the equipment list.
        /// </summary>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static ObservableCollection<String> GetEquipmentList()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var result = (from sess in dc.RecordingSessions
                          select sess.Equipment).Distinct();
            return (new ObservableCollection<String>(result));
        }

        /// <summary>
        ///     Gets the location list.
        /// </summary>
        /// <returns>
        ///     </returns>
        internal static ObservableCollection<String> GetLocationList()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var locations = (from sess in dc.RecordingSessions
                             select sess.Location).Distinct();
            return (new ObservableCollection<String>(locations));
        }

        /// <summary>
        ///     Gets the microphone list .
        /// </summary>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static ObservableCollection<String> GetMicrophoneList()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var result = (from sess in dc.RecordingSessions
                          select sess.Microphone).Distinct();
            return (new ObservableCollection<String>(result));
        }

        /// <summary>
        ///     Gets the operators.
        /// </summary>
        /// <returns>
        ///     </returns>
        internal static ObservableCollection<String> GetOperators()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var operators = ((from op in dc.RecordingSessions
                              select op.Operator).Distinct());
            return (new ObservableCollection<String>(operators));
        }

        /// <summary>
        ///     Gets the recording with the specified Id
        /// </summary>
        /// <param name="id">
        ///     The identifier.
        /// </param>
        /// <returns>
        ///     </returns>
        internal static Recording GetRecording(int id)
        {
            Recording recording = null;
            if (id <= 0) return (null);
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            try
            {
                var recs = from rec in dc.Recordings
                           where rec.Id == id
                           select rec;
                if (recs != null)
                {
                    recording = recs.Single();
                }
            }
            catch (Exception)
            {
                return (null);
            }
            return (recording);
        }

        /// <summary>
        ///     Gets the recording session.
        /// </summary>
        /// <param name="sessionTag">
        ///     The session tag.
        /// </param>
        /// <returns>
        ///     </returns>
        internal static RecordingSession GetRecordingSession(string sessionTag)
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            RecordingSession session = new RecordingSession();
            var sessions = (from rs in dc.RecordingSessions
                            where rs.SessionTag == sessionTag
                            select rs);
            if (sessions != null && sessions.Count() > 0)
            {
                session = sessions.First();
                return (session);
            }

            return (null);
        }

        /// <summary>
        ///     Gets the recording session list.
        /// </summary>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static ObservableCollection<RecordingSession> GetRecordingSessionList()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var result = dc.RecordingSessions.Select(session => session);
            return (new ObservableCollection<RecordingSession>(result));
        }

        /// <summary>
        ///     Gets the recordings for session indicated by the supplied Id
        /// </summary>
        /// <param name="id">
        ///     The identifier.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static EntitySet<Recording> GetRecordingsForSession(int id)
        {
            EntitySet<Recording> result = new EntitySet<Recording>();
            if (id <= 0) return (result);
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var recordings = from rec in dc.Recordings
                             where rec.RecordingSessionId == id
                             select rec;
            if (recordings != null && recordings.Count() > 0)
            {
                foreach (var rec in recordings)
                {
                    result.Add(rec);
                }
            }
            return (result);
        }

        internal static ObservableCollection<LabelledSegment> GetSegmentsForRecording(int id)
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var segments = from seg in dc.LabelledSegments
                           where seg.RecordingID == id
                           select seg;
            return (new ObservableCollection<LabelledSegment>(segments));
        }

        /// <summary>
        ///     Gets the stats for recording. Given the ID of a specific recording produces a list
        ///     with an element for each bat type that was present in the recording and the number
        ///     of passes and the min, max, mean durations of each pass or labelled segment.
        /// </summary>
        /// <param name="recordingId">
        ///     The recording identifier.
        /// </param>
        /// <returns>
        ///     </returns>
        internal static ObservableCollection<BatStats> GetStatsForRecording(int recordingId)
        {
            ObservableCollection<BatStats> result = new ObservableCollection<BatStats>();
            if (recordingId <= 0) return (result);

            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();

            var batSegments = from link in dc.BatSegmentLinks
                              where link.LabelledSegment.RecordingID == recordingId
                              select link;
            if (batSegments != null && batSegments.Count() > 0)
            {
                // batSegments contains entries for each unique segment/bat combination in the
                // specified recording
                var bats = (from link in batSegments
                            select link.Bat).Distinct<Bat>();
                if (bats != null && bats.Count() > 0)
                {
                    foreach (var bat in bats)

                    {
                        BatStats stat = new BatStats();
                        stat.batCommonName = bat.Name;
                        var segmentsForThisBat = (from link in batSegments
                                                  where link.BatID == bat.Id
                                                  select link.LabelledSegment).Distinct();
                        if (segmentsForThisBat != null && segmentsForThisBat.Count() > 0)
                        {
                            foreach (var segment in segmentsForThisBat)
                            {
                                stat.Add(segment.EndOffset - segment.StartOffset);
                            }
                        }
                        result.Add(stat);
                    }
                }
            }

            return (result);
        }

        /// <summary>
        ///     For a given recording session produce List of BatStats each of which gives the
        ///     number of passes and segments for a single recording for a single bat.
        /// </summary>
        /// <param name="recordingSession">
        ///     The recording session.
        /// </param>
        /// <returns>
        ///     </returns>
        internal static ObservableCollection<BatStats> GetStatsForSession(RecordingSession recordingSession)
        {
            ObservableCollection<BatStats> result = new ObservableCollection<BatStats>();
            if (recordingSession != null)
            {
                BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();

                var recordingsInSession = from rec in dc.Recordings
                                          where rec.RecordingSessionId == recordingSession.Id
                                          select rec;
                if (recordingsInSession != null && recordingsInSession.Count() > 0)
                {
                    var labelledSegmentsinSession = dc.LabelledSegments.Where(
                        ls => recordingsInSession.Any(rec => rec.Id == ls.RecordingID));

                    if (labelledSegmentsinSession != null && labelledSegmentsinSession.Count() > 0)
                    {
                        foreach (var seg in labelledSegmentsinSession)
                        {
                            foreach (var pass in seg.BatSegmentLinks)
                            {
                                BatStats stat = new BatStats();
                                stat.batCommonName = pass.Bat.Name;
                                stat.Add(seg.EndOffset - seg.StartOffset);
                                result.Add(stat);
                            }
                        }
                    }
                }
            }
            return (result);
        }

        /// <summary>
        ///     Gets the unknown bat.
        /// </summary>
        /// <returns>
        ///     </returns>
        internal static Bat GetUnknownBat()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var batlist = from bat in dc.Bats
                          where bat.Name == "Unknown"
                          select bat;
            if (batlist == null || batlist.Count() <= 0)
            {
                Bat noBat = new Bat();
                noBat.Name = "Unknown";

                BatTag tag = new BatTag();
                tag.BatTag1 = "Unknown";
                tag.SortIndex = 1;
                noBat.BatTags.Add(tag);
                noBat.BatSpecies = "sp.";
                noBat.Batgenus = "Unknown";
                noBat.SortIndex = int.MaxValue;
                dc.Bats.InsertOnSubmit(noBat);
                dc.SubmitChanges();
                return (noBat);
            }

            return (batlist.First());
        }

        /// <summary>
        ///     Gets the working database location.
        /// </summary>
        /// <returns>
        ///     </returns>
        internal static String GetWorkingDatabaseLocation()
        {
            string workingDatabaseLocation = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                @"Echolocation\WinBLP\");
            if (!Directory.Exists(workingDatabaseLocation))
            {
                Directory.CreateDirectory(workingDatabaseLocation);
            }
            return (workingDatabaseLocation);
        }

        /// <summary>
        ///     Initializes the database.
        /// </summary>
        internal static void InitializeDatabase()
        {
            try
            {
                String workingDatabaseLocation = DBAccess.GetWorkingDatabaseLocation();
                String dbShortName = "BatReferenceDB";
                if (!File.Exists(workingDatabaseLocation + dbShortName + ".mdf"))
                {
                    if (File.Exists(dbShortName + ".mdf"))
                    {
                        File.Copy(dbShortName + ".mdf", workingDatabaseLocation + dbShortName + ".mdf");
                        if (!File.Exists(workingDatabaseLocation + dbShortName + ".ldf") && File.Exists(dbShortName + ".ldf"))
                        {
                            File.Copy(dbShortName + ",ldf", workingDatabaseLocation + dbShortName + ".ldf");
                        }
                    }
                }

                BatReferenceDBLinqDataContext batReferenceDataContext = DBAccess.GetDataContext();

                if (File.Exists(workingDatabaseLocation + "EditableBatReferenceXMLFile.xml"))
                {
                    copyXMLDataToDatabase(workingDatabaseLocation + "EditableBatReferenceXMLFile.xml", batReferenceDataContext);
                    if (File.Exists(workingDatabaseLocation + "EditableBatReferenceXMLFile.xml.bak"))
                    {
                        File.Delete(workingDatabaseLocation + "EditableBatReferenceXMLFile.xml.bak");
                    }
                    File.Move(workingDatabaseLocation + "EditableBatReferenceXMLFile.xml", workingDatabaseLocation + "EditableBatReferenceXMLFile.xml.bak");
                }

                DBAccess.ResequenceBats();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        ///     Inserts the recording session provided into the database. An existing 'identical'
        ///     record has either the same Id, or the same date and location, or same date and GPS
        ///     co-ords. If there is a matching session then the existing session is updated to the
        ///     new data, even if the new data is incomplete. If there is no existing session then
        ///     the new session must be validated before being entered into the database. The
        ///     function returns an informative error string if the update/ insertion fails, or a
        ///     null/empty string if the process is successful.
        /// </summary>
        /// <param name="newSession">
        ///     The new session.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static string InsertRecordingSession(RecordingSession newSession)
        {
            String result = "";
            try
            {
                DBAccess.UpdateRecordingSession(newSession);
            }
            catch (Exception ex)
            {
                result = ex.Message;
            }
            return (result);
        }

        /// <summary>
        ///     Moves the bat. Moves the indicated bat up or down in the sorted bat list by the
        ///     amount indicated in the second parameter
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="v">
        ///     The v.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static void MoveBat(Bat bat, int distance)
        {
            try
            {
                BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
                var batToMove = (from bt in dc.Bats
                                 where bt.Batgenus == bat.Batgenus && bt.BatSpecies == bat.BatSpecies
                                 select bt).Single();
                if (batToMove == null) return;
                var batToReplace = (from bt in dc.Bats
                                    where bt.SortIndex == (batToMove.SortIndex + distance)
                                    select bt).Single();
                if (batToReplace == null) return;
                int? temp = batToMove.SortIndex;
                batToMove.SortIndex = batToReplace.SortIndex;
                batToReplace.SortIndex = temp;
                dc.SubmitChanges();
            }
            catch (Exception)
            {
                return;
            }
        }

        /// <summary>
        ///     Moves the tag.
        /// </summary>
        /// <param name="tag">
        ///     The tag.
        /// </param>
        /// <param name="offset">
        ///     The offset.
        /// </param>
        internal static void MoveTag(BatTag tag, short offset)
        {
            if (tag == null) return;
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var tagPair = from tg in dc.BatTags
                          where tg.BatID == tag.BatID &&
                          (tg.SortIndex == tag.SortIndex || tg.SortIndex == tag.SortIndex + offset)
                          select tg;
            if (tagPair != null && tagPair.Count() == 2)
            {
                short temp = tagPair.First().SortIndex.Value;
                tagPair.First().SortIndex = tagPair.Last().SortIndex.Value;
                tagPair.Last().SortIndex = temp;
            }

            dc.SubmitChanges();
            DBAccess.ResequenceTags(tag, dc);
        }

        /// <summary>
        ///     Moves the tag down.
        /// </summary>
        /// <param name="tag">
        ///     The tag.
        /// </param>
        internal static void MoveTagDown(BatTag tag)
        {
            DBAccess.MoveTag(tag, 1);
        }

        /// <summary>
        ///     Moves the tag up.
        /// </summary>
        /// <param name="tag">
        ///     The tag.
        /// </param>
        internal static void MoveTagUp(BatTag tag)
        {
            DBAccess.MoveTag(tag, -1);
        }

        /// <summary>
        ///     Updates the bat.
        /// </summary>
        /// <param name="selectedBat">
        ///     The selected bat.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static void UpdateBat(Bat selectedBat)
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            try
            {
                Bat ExistingBat = (from bat in dc.Bats
                                   where bat.Id == selectedBat.Id
                                   select bat).Single();
                if (ExistingBat != null)
                {
                    ExistingBat.Batgenus = selectedBat.Batgenus;
                    ExistingBat.BatSpecies = selectedBat.BatSpecies;
                    ExistingBat.Name = selectedBat.Name;
                    ExistingBat.Notes = selectedBat.Notes;
                    ExistingBat.SortIndex = selectedBat.SortIndex;
                    DBAccess.MergeTags(ExistingBat, selectedBat, dc);

                    dc.SubmitChanges();
                }
            }
            catch (NullReferenceException nex)
            {
                Debug.WriteLine("NULL in UpdateBat:- " + nex.Message);
                Debug.Write(nex.StackTrace);
            }
        }

        /// <summary>
        ///     Updates the bat list.
        /// </summary>
        /// <param name="batList">
        ///     The bat list.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static void UpdateBatList(ObservableCollection<Bat> batList)
        {
            if (batList != null)
            {
                foreach (var bat in batList)
                {
                    DBAccess.UpdateBat(bat);
                }
            }
        }

        /// <summary>
        ///     Updates the recording in the database with the supplied recording. This version does
        ///     not require a SegmentsAndBatsList and uses the LabelledSegments in the recording
        ///     instance. Each LabelledSegment is parsed for bat identity as it is added or modified
        ///     into the database.
        /// </summary>
        /// <param name="recording">
        ///     The recording.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static string UpdateRecording(Recording recording)
        {
            string err = "";
            if (recording == null) return ("No recording supplied to be updated");
            Recording existingRecording;
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            existingRecording = (from rec in dc.Recordings
                                 where rec.Id == recording.Id
                                 select rec).Single();
            if (existingRecording == null)
            {
                existingRecording = recording;
                err = DBAccess.ValidateRecording(existingRecording);
                if (!String.IsNullOrWhiteSpace(err))
                {
                    return (err);
                }
                dc.Recordings.InsertOnSubmit(existingRecording);
                dc.SubmitChanges();
            }
            else
            {
                existingRecording.RecordingEndTime = recording.RecordingEndTime;
                existingRecording.RecordingGPSLatitude = recording.RecordingGPSLatitude;
                existingRecording.RecordingGPSLongitude = recording.RecordingGPSLongitude;
                existingRecording.RecordingName = recording.RecordingName;
                existingRecording.RecordingNotes = recording.RecordingNotes;
                existingRecording.RecordingStartTime = recording.RecordingStartTime;
                if (existingRecording.LabelledSegments == null)
                {
                    existingRecording.LabelledSegments = new System.Data.Linq.EntitySet<LabelledSegment>();
                }
                existingRecording.LabelledSegments = recording.LabelledSegments;
            }

            err = ValidateRecording(existingRecording);
            if (String.IsNullOrWhiteSpace(err))
            {
                DBAccess.UpdateLabelledSegments(existingRecording.LabelledSegments, existingRecording, dc);
                dc.SubmitChanges();
            }

            return (err);
        }

        /// <summary>
        ///     Updates the recording. Adds it to the database if not already present or modifies
        ///     the existing entry to match if present. The measure of presence depends on the Name
        ///     filed which is the name of the wav file and should be unique in the database.
        /// </summary>
        /// <param name="recording">
        ///     The recording.
        /// </param>
        /// <param name="listOfSegmentAndBatList">
        ///     The list of segment and bat list.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static string UpdateRecording(Recording recording, ObservableCollection<SegmentAndBatList> listOfSegmentAndBatList)
        {
            //TODO update/insert LabelledSegments and ExtendedBatPasses to go with this recording
            string errmsg = null;
            Recording existingRecording = null;
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            errmsg = DBAccess.ValidateRecording(recording);
            try
            {
                if (String.IsNullOrWhiteSpace(errmsg))
                {
                    RecordingSession session = (from sess in dc.RecordingSessions
                                                where sess.Id == recording.RecordingSessionId
                                                select sess).Single();
                    if (session == null) return ("Unable to Locate Session for this Recording");
                    IQueryable<Recording> existingRecordings = null;
                    if (recording.Id <= 0 && !String.IsNullOrWhiteSpace(recording.RecordingName))
                    {
                        existingRecordings = from rec in dc.Recordings
                                             where rec.RecordingName == recording.RecordingName
                                             select rec;
                    }
                    else if (recording.Id > 0)
                    {
                        existingRecordings = from rec in dc.Recordings
                                             where rec.Id == recording.Id
                                             select rec;
                    }
                    if (existingRecordings != null && existingRecordings.Count() > 0)
                    {
                        existingRecording = existingRecordings.First();
                    }
                    if (existingRecording == null)
                    {
                        recording.RecordingSessionId = session.Id;
                        existingRecording = recording;
                        dc.Recordings.InsertOnSubmit(existingRecording);
                    }
                    else
                    {
                        existingRecording.RecordingEndTime = recording.RecordingEndTime;
                        existingRecording.RecordingGPSLatitude = recording.RecordingGPSLatitude;
                        existingRecording.RecordingGPSLongitude = recording.RecordingGPSLongitude;
                        existingRecording.RecordingName = recording.RecordingName;
                        existingRecording.RecordingNotes = recording.RecordingNotes;
                        existingRecording.RecordingSessionId = session.Id;
                        existingRecording.RecordingStartTime = recording.RecordingStartTime;
                    }
                    dc.SubmitChanges();
                    if (listOfSegmentAndBatList != null)
                    {
                        DBAccess.UpdateLabelledSegments(listOfSegmentAndBatList, existingRecording.Id, dc);
                    }
                }
                return (errmsg);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UpdateRecording - " + ex.Message);
                return (ex.Message);
            }
        }

        /// <summary>
        ///     Updates the recording session if it already exists in the database or adds it to the database
        /// </summary>
        /// <param name="sessionForFolder">
        ///     The session for folder.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal static void UpdateRecordingSession(RecordingSession sessionForFolder)
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            RecordingSession existingSession = null;

            var existingSessions = (from sess in dc.RecordingSessions
                                    where sess.Id == sessionForFolder.Id || sess.SessionTag == sessionForFolder.SessionTag
                                    select sess);
            if (existingSessions != null && existingSessions.Count() > 0)
            {
                existingSession = existingSessions.First();
            }

            if (existingSession == null)
            {
                dc.RecordingSessions.InsertOnSubmit(sessionForFolder);
            }
            else
            {
                existingSession.SessionTag = sessionForFolder.SessionTag.Truncate(20);
                existingSession.Equipment = sessionForFolder.Equipment.Truncate(120);
                existingSession.Microphone = sessionForFolder.Microphone.Truncate(120);
                existingSession.Location = sessionForFolder.Location.Truncate(50);
                existingSession.SessionDate = sessionForFolder.SessionDate;
                existingSession.SessionStartTime = sessionForFolder.SessionStartTime;
                existingSession.SessionEndTime = sessionForFolder.SessionEndTime;
                existingSession.SessionNotes = sessionForFolder.SessionNotes;
                existingSession.Temp = sessionForFolder.Temp;
                existingSession.Operator = sessionForFolder.Operator;
                existingSession.LocationGPSLatitude = sessionForFolder.LocationGPSLatitude;
                existingSession.LocationGPSLongitude = sessionForFolder.LocationGPSLongitude;
                existingSession.OriginalFilePath = sessionForFolder.OriginalFilePath;
            }
            dc.SubmitChanges();
        }

        internal static void UpdateSegment(LabelledSegment segmentToEdit)
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            if (segmentToEdit.Id <= 0)
            {
                dc.LabelledSegments.InsertOnSubmit(segmentToEdit);
                dc.SubmitChanges();
            }
            else
            {
                var existingSegment = (from seg in dc.LabelledSegments
                                       where seg.Id == segmentToEdit.Id
                                       select seg).Single();
                if (existingSegment != null)
                {
                    existingSegment.Comment = segmentToEdit.Comment;
                    existingSegment.StartOffset = segmentToEdit.StartOffset;
                    existingSegment.EndOffset = segmentToEdit.EndOffset;
                    dc.SubmitChanges();
                }
            }
        }

        /// <summary>
        ///     Converts the XML bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <returns>
        ///     </returns>
        private static Bat ConvertXMLBat(XElement bat)
        {
            Bat newBat = new Bat();
            try
            {
                newBat.Name = bat.Attribute("Name").Value;
                newBat.Batgenus = bat.Descendants("BatGenus").FirstOrDefault().Value;
                newBat.BatSpecies = bat.Descendants("BatSpecies").FirstOrDefault().Value;
                newBat.SortIndex = 2000;
                newBat.Notes = "";

                var newTags = bat.Descendants("BatTag");
                if (newTags != null && newTags.Count() > 0)
                {
                    short index = 0;
                    foreach (var tag in newTags)
                    {
                        BatTag bt = new BatTag();
                        bt.BatTag1 = tag.Value;
                        bt.BatID = newBat.Id;
                        bt.SortIndex = index++;
                        newBat.BatTags.Add(bt);
                    }
                }

                var newCommonNames = bat.Descendants("BatCommonName");
                if (newCommonNames != null && newCommonNames.Count() > 0)
                {
                    bat.Name = newCommonNames.First().Value;

                    /*short index = 0;
                    foreach (var name in newCommonNames)
                    {
                        BatCommonName bcn = new BatCommonName();
                        bcn.BatCommonName1 = name.Value;
                        bcn.BatID = newBat.Id;
                        bcn.SortIndex = index++;
                        newBat.BatCommonNames.Add(bcn);
                    }*/
                }
            }
            catch (Exception)
            {
            }

            return (newBat);
        }

        /// <summary>
        ///     Copies the XML data to database.
        /// </summary>
        /// <param name="xmlFile">
        ///     The XML file.
        /// </param>
        /// <param name="batReferenceDataContext">
        ///     The bat reference data context.
        /// </param>
        private static void copyXMLDataToDatabase(string xmlFile, BatReferenceDBLinqDataContext batReferenceDataContext)
        {
            var xmlBats = XElement.Load(xmlFile).Descendants("Bat");
            short i = 0;
            foreach (XElement bat in xmlBats)
            {
                MergeXMLBatToDB(bat, batReferenceDataContext, i++);
            }
        }

        /// <summary>
        ///     Deletes all recordings in session and all Segments in all those recordings.
        /// </summary>
        /// <param name="session">
        ///     The session.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private static void DeleteAllRecordingsInSession(RecordingSession session, BatReferenceDBLinqDataContext dc)
        {
            DBAccess.DeleteAllSegmentsInSession(session, dc);
            var recordingsToDelete = from rec in dc.Recordings
                                     where rec.RecordingSessionId == session.Id
                                     select rec;
            dc.Recordings.DeleteAllOnSubmit(recordingsToDelete);
            dc.SubmitChanges();
        }

        /// <summary>
        ///     Deletes all segments in recording passes as a parameter, using the supplied DataContext.
        /// </summary>
        /// <param name="recording">
        ///     The recording.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private static void DeleteAllSegmentsInRecording(Recording recording, BatReferenceDBLinqDataContext dc)
        {
            if (recording != null)
            {
                var segmentsToDelete = from seg in dc.LabelledSegments
                                       where seg.RecordingID == recording.Id
                                       select seg;
                if (segmentsToDelete != null)
                {
                    foreach (var seg in segmentsToDelete)
                    {
                        DeleteLinksForSegmentId(seg.Id, dc);
                    }
                }
                dc.LabelledSegments.DeleteAllOnSubmit(segmentsToDelete);
                dc.SubmitChanges();
            }
        }

        /// <summary>
        ///     Deletes all segments in session.
        /// </summary>
        /// <param name="session">
        ///     The session.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private static void DeleteAllSegmentsInSession(RecordingSession session, BatReferenceDBLinqDataContext dc)
        {
            var segmentsToDelete = from seg in dc.LabelledSegments
                                   where seg.Recording.RecordingSessionId == session.Id
                                   select seg;
            if (segmentsToDelete != null)
            {
                foreach (var seg in segmentsToDelete)
                {
                    DeleteLinksForSegmentId(seg.Id, dc);
                }
            }
            dc.LabelledSegments.DeleteAllOnSubmit(segmentsToDelete);
            dc.SubmitChanges();
        }

        /// <summary>
        ///     Deletes the links for segment identifier.
        /// </summary>
        /// <param name="id">
        ///     The identifier.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        private static void DeleteLinksForSegmentId(int id, BatReferenceDBLinqDataContext dc)
        {
            if (dc == null) dc = DBAccess.GetDataContext();
            var linksToDelete = from lnk in dc.BatSegmentLinks
                                where lnk.LabelledSegmentID == id
                                select lnk;
            if (linksToDelete != null && linksToDelete.Count() > 0)
            {
                dc.BatSegmentLinks.DeleteAllOnSubmit(linksToDelete);
                dc.SubmitChanges();
            }
        }

        /// <summary>
        ///     Gets the matching bat. Returns a bat from the database which has the same genus and
        ///     species as the bat passes as a parameter or null if no matching bat is found. If
        ///     more than one matching bat is found (should not
        ///     happen) will return the one with the lowest sortIndex.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="NotImplementedException">
        ///     </exception>
        private static Bat GetMatchingBat(Bat bat, BatReferenceDBLinqDataContext dataContext)
        {
            try
            {
                if (bat == null) return (null);
                var sortedMatchingBats = from b in dataContext.Bats
                                         where (bat.Id > 0 && b.Id == bat.Id) ||
                                         (b.Batgenus == bat.Batgenus &&
                                           b.BatSpecies == bat.BatSpecies)
                                         orderby b.SortIndex
                                         select b;
                if (sortedMatchingBats == null) return (null);
                if (sortedMatchingBats.Count() <= 0) return (null);
                return (sortedMatchingBats.First());
            }
            catch (Exception)
            {
                return (null);
            }
        }

        /// <summary>
        ///     Gets the named bat.
        /// </summary>
        /// <param name="name">
        ///     The name.
        /// </param>
        /// <param name="dataContext">
        ///     The data context.
        /// </param>
        /// <returns>
        ///     </returns>
        private static Bat GetNamedBat(string name, BatReferenceDBLinqDataContext dataContext)
        {
            var namedBats = from b in dataContext.Bats
                            where b.Name == name
                            orderby b.SortIndex
                            select b;
            if (namedBats == null || namedBats.Count() <= 0)
            {
                return (null);
            }
            return (namedBats.First());
        }

        /// <summary>
        ///     Gets the passes for bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <returns>
        ///     </returns>
        private static BatStats GetPassesForBat(Bat bat, BatReferenceDBLinqDataContext dc)
        {
            BatStats stats = new BatStats();
            if (bat != null && dc != null)
            {
                var segments = from link in dc.BatSegmentLinks
                               where link.BatID == bat.Id
                               select link.LabelledSegment;
                if (segments != null && segments.Count() > 0)
                {
                    foreach (var seg in segments)
                    {
                        stats.Add(seg.EndOffset - seg.StartOffset);
                    }
                }
            }

            return (stats);
        }

        /// <summary>
        ///     Gets the recordings for bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <returns>
        ///     </returns>
        private static ObservableCollection<Recording> GetRecordingsForBat(Bat bat, BatReferenceDBLinqDataContext dc)
        {
            ObservableCollection<Recording> result = new ObservableCollection<Recording>();
            if (bat != null && dc != null)
            {
                var segments = (from link in dc.BatSegmentLinks
                                where link.BatID == bat.Id
                                select link.LabelledSegment).Distinct();
                var recordings = (from rec in dc.Recordings
                                  from seg in segments
                                  where seg.RecordingID == rec.Id
                                  select rec).Distinct();
                if (recordings != null)
                {
                    result = new ObservableCollection<Recording>(recordings);
                }
            }

            return (result);
        }

        /// <summary>
        ///     Gets the sessions for bat.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private static ObservableCollection<RecordingSession> GetSessionsForBat(Bat bat, BatReferenceDBLinqDataContext dc)
        {
            ObservableCollection<RecordingSession> result = new ObservableCollection<RecordingSession>();
            if (bat != null && dc != null)
            {
                var segments = (from link in dc.BatSegmentLinks
                                where link.BatID == bat.Id
                                select link.LabelledSegment).Distinct();
                var sessions = (from sess in dc.RecordingSessions
                                from seg in segments
                                where seg.Recording.RecordingSessionId == sess.Id
                                select sess).Distinct();
                if (sessions != null)
                {
                    result = new ObservableCollection<RecordingSession>(sessions);
                }
            }
            return (result);
        }

        /// <summary>
        ///     Inserts the bat. Adds the supplied bat to the database. It is assumed that the bat
        ///     has been verified and that it does not already exist in the database
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="NotImplementedException">
        ///     </exception>
        private static string InsertBat(Bat bat, BatReferenceDBLinqDataContext dataContext)
        {
            try
            {
                dataContext.Bats.InsertOnSubmit(bat);
                dataContext.SubmitChanges();
                DBAccess.ResequenceBats();
            }
            catch (Exception ex)
            {
                return (ex.Message);
            }
            return ("");
        }

        /// <summary>
        ///     Merges the tags.
        /// </summary>
        /// <param name="existingBat">
        ///     The existing bat.
        /// </param>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="dataContext">
        ///     The data context.
        /// </param>
        private static void MergeTags(Bat existingBat, Bat bat, BatReferenceDBLinqDataContext dataContext)
        {
            var tagsToDelete = existingBat.BatTags.Where(
                ebtags => bat.BatTags.Any(btags => btags.BatTag1 == ebtags.BatTag1));
            dataContext.BatTags.DeleteAllOnSubmit(tagsToDelete);

            var tagsToAdd = bat.BatTags.Where(
                btag => existingBat.BatTags.Any(ebtag => ebtag.BatTag1 == btag.BatTag1));
            existingBat.BatTags.AddRange(tagsToAdd);

            dataContext.SubmitChanges();

            var existingTags = from tag in dataContext.BatTags
                               where tag.BatID == existingBat.Id
                               orderby tag.SortIndex
                               select tag;
            short i = 0;
            foreach (var tag in existingTags)
            {
                tag.SortIndex = i++;
            }
        }

        /// <summary>
        ///     Merges the XML bat to database.
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="batReferenceDataContext">
        ///     The bat reference data context.
        /// </param>
        /// <param name="i">
        ///     The i.
        /// </param>
        private static void MergeXMLBatToDB(XElement bat, BatReferenceDBLinqDataContext batReferenceDataContext, short i)
        {
            Bat batToMerge = ConvertXMLBat(bat);
            DBAccess.MergeBat(batToMerge);
        }

        /// <summary>
        ///     Resequences the bats by SortIndex, retaining the original sequence as much as possible.
        /// </summary>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private static void ResequenceBats()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            var orderedBats = from bt in dc.Bats
                              orderby bt.SortIndex
                              select bt;
            short index = 0;
            foreach (var bat in orderedBats)
            {
                bat.SortIndex = index++;
            }
            dc.SubmitChanges();
        }

        /// <summary>
        ///     Resequences the tags.
        /// </summary>
        /// <param name="tag">
        ///     The tag.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <returns>
        ///     </returns>
        private static int ResequenceTags(BatTag tag, BatReferenceDBLinqDataContext dc)
        {
            int result = 0;
            var TagsToSort = from tg in dc.BatTags
                             where tg.BatID == tag.BatID
                             orderby tg.SortIndex
                             select tg;
            short index = 0;
            foreach (var tg in TagsToSort)
            {
                tg.SortIndex = index++;
                if (tg.BatTag1 == tag.BatTag1)
                {
                    result = (int)index - 1;
                }
            }
            dc.SubmitChanges();
            return (result);
        }

        /// <summary>
        ///     Updates the extended bat pass, or inserts it if it does not already exist in the database
        /// </summary>
        /// <param name="bat">
        ///     The bat.
        /// </param>
        /// <param name="segment">
        ///     the parent LabelledSegment that this pass belongs to
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private static void UpdateBatSegmentLinks(Bat bat, LabelledSegment segment, BatReferenceDBLinqDataContext dc)
        {
            BatSegmentLink batSegmentLink = null;

            var matchingPasses = from p in dc.BatSegmentLinks
                                 where p.BatID == bat.Id && p.LabelledSegmentID == segment.Id
                                 select p;
            if (matchingPasses != null && matchingPasses.Count() > 0)
            {
                batSegmentLink = matchingPasses.First();

                batSegmentLink.NumberOfPasses = Tools.GetNumberOfPassesForSegment(segment);
            }
            else
            {
                batSegmentLink = new BatSegmentLink();
                batSegmentLink.LabelledSegmentID = segment.Id;
                batSegmentLink.BatID = bat.Id;
                batSegmentLink.NumberOfPasses = Tools.GetNumberOfPassesForSegment(segment);
                dc.BatSegmentLinks.InsertOnSubmit(batSegmentLink);
            }
            try
            {
                dc.SubmitChanges();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UpdatedExtendedBatPAss - " + ex.Message);
            }
        }

        /// <summary>
        ///     Updates the labelled segments. using the data in the combinedSgementsAndPasses and
        ///     linked to the recording identified by the Id. Also adds data to the
        ///     extendedBatPasses table.
        /// </summary>
        /// <param name="segmentAndBatList">
        ///     The combined segments and passes.
        /// </param>
        /// <param name="recordingId">
        ///     The identifier.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private static void UpdateLabelledSegment(SegmentAndBatList segmentAndBatList, int recordingId, BatReferenceDBLinqDataContext dc)
        {
            try
            {
                LabelledSegment existingSegment = null;
                if (dc == null)
                {
                    dc = DBAccess.GetDataContext();
                }
                var segments = from seg in dc.LabelledSegments
                               where seg.Id == segmentAndBatList.segment.Id
                               select seg;
                if (segments != null && segments.Count() > 0)
                {
                    existingSegment = segments.First();
                    existingSegment.EndOffset = segmentAndBatList.segment.EndOffset;
                    existingSegment.Comment = segmentAndBatList.segment.Comment;
                }
                if (existingSegment == null)
                {
                    existingSegment = segmentAndBatList.segment;
                    existingSegment.RecordingID = recordingId;
                    dc.LabelledSegments.InsertOnSubmit(existingSegment);
                }
                dc.SubmitChanges();
                if (segmentAndBatList.batList != null && segmentAndBatList.batList.Count > 0)
                {
                    foreach (var bat in segmentAndBatList.batList)
                    {
                        DBAccess.UpdateBatSegmentLinks(bat, existingSegment, dc);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UpdateLabelledSegments - " + ex.Message);
            }
        }

        /// <summary>
        ///     Updates the labelled segments.
        /// </summary>
        /// <param name="listOfSegmentAndBatList">
        ///     The list of segment and bat list.
        /// </param>
        /// <param name="id">
        ///     The identifier.
        /// </param>
        /// <param name="dc">
        ///     The dc.
        /// </param>
        private static void UpdateLabelledSegments(ObservableCollection<SegmentAndBatList> listOfSegmentAndBatList, int id, BatReferenceDBLinqDataContext dc)
        {
            if (listOfSegmentAndBatList != null && listOfSegmentAndBatList.Count() > 0)
            {
                foreach (var seg in listOfSegmentAndBatList)
                {
                    UpdateLabelledSegment(seg, id, dc);
                }
            }
        }
    }

    /// <summary>
    ///     Class to add functionality to the String class
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        ///     Truncates to the specified maximum length.
        /// </summary>
        /// <param name="s">
        ///     The s.
        /// </param>
        /// <param name="maxLength">
        ///     The maximum length.
        /// </param>
        /// <returns>
        ///     </returns>
        public static string Truncate(this string s, int maxLength)
        {
            if (s.Length > maxLength)
            {
                return (s.Substring(0, maxLength));
            }
            return (s);
        }
    }
}