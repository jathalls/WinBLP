using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BatRecordingManager
{
    /// <summary>
    /// Class to add functionality to the String class
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Truncates to the specified maximum length.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <param name="maxLength">The maximum length.</param>
        /// <returns></returns>
        public static string Truncate(this string s, int maxLength)
        {
            if (s.Length > maxLength)
            {
                return (s.Substring(0, maxLength));
            }
            return (s);
        }
    }

    /// <summary>
    /// Static class of Database interface functions
    /// </summary>
    public static class DBAccess
    {
        /// <summary>
        /// Merges the bat.  The supplied bat is either inserted
        /// into the database if it is not already there, or,
        /// if a bat with the same genus and species is present
        /// then the data in this bat is added to and merged with
        /// the data for the existing bat.
        /// Sort orders are taken from the new bat and duplicate
        /// tags or common names are removed, otherwise any tag or
        /// common name differing ins pelling or capitalization will
        /// be treated as a new item.
        /// Existing tags or common names which do not exist in the new
        /// bat will be removed.
        /// Notes from the new bat will replace notes in the existing bat.
        /// The bat 'name' will be updated to reflect the common name
        /// with the lowest sort index.
        /// Returns a suitable error message if the process failed, or an
        /// empty string if the process was successful;
        /// </summary>
        /// <param name="bat">The bat.</param>
        /// <returns></returns>
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
                dataContext.SubmitChanges();
            }

            return (result);
        }

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

        internal static List<Bat> GetDescribedBats(string description)
        {
            List<Bat> matchingBats = new List<Bat>();
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
            return (matchingBats.Distinct().ToList());
        }

        /// <summary>
        /// For a given recording session produce List of BatStats each of which gives the
        /// number of passes and segments for a single recording for a single bat.
        /// </summary>
        /// <param name="recordingSession">The recording session.</param>
        /// <returns></returns>
        internal static List<BatStats> GetStatsForSession(RecordingSession recordingSession)
        {
            List<BatStats> result = new List<BatStats>();
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
        /// Deletes the session provided as a parameter and identified by
        /// the Id.  All related recordings are also deleted.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static void DeleteSession(RecordingSession session)
        {
            if (session!=null && session.Id > 0)
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
        /// Deletes all recordings in session and all Segments in all
        /// those recordings.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="dc">The dc.</param>
        /// <exception cref="System.NotImplementedException"></exception>
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
        /// Deletes all segments in session.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <param name="dc">The dc.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void DeleteAllSegmentsInSession(RecordingSession session, BatReferenceDBLinqDataContext dc)
        {
            var segmentsToDelete = from seg in dc.LabelledSegments
                                   where seg.Recording.RecordingSessionId == session.Id
                                   select seg;
            dc.LabelledSegments.DeleteAllOnSubmit(segmentsToDelete);
            dc.SubmitChanges();
        }

        /// <summary>
        /// Deletes the recording supplied as a parameter and all
        /// LabelledSegments related to that recording.
        /// </summary>
        /// <param name="recording">The recording.</param>
        public static String DeleteRecording(Recording recording)
        {
            String result=null;
            if(recording!= null && recording.Id>0)
            {
                /*
                if(recording.LabelledSegments!=null && recording.LabelledSegments.Count() > 0)
                {
                    foreach(var segment in recording.LabelledSegments)
                    {
                        DBAccess.DeleteSegment(segment);
                    }
                }*/
                try {
                    BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
                    DBAccess.DeleteAllSegmentsInRecording(recording, dc);
                    var recordingToDelete = (from rec in dc.Recordings
                                             where rec.Id == recording.Id
                                             select rec).Single();
                    dc.Recordings.DeleteOnSubmit(recordingToDelete);
                    dc.SubmitChanges();
                }catch(Exception ex)
                {
                    result = "Error deleting recording:- " + ex.Message;
                }
            }
            return (result);
        }

        /// <summary>
        /// Deletes all segments in recording passes as a parameter, using the
        /// supplied DataContext.
        /// </summary>
        /// <param name="recording">The recording.</param>
        /// <param name="dc">The dc.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void DeleteAllSegmentsInRecording(Recording recording, BatReferenceDBLinqDataContext dc)
        {
            if (recording != null)
            {
                var segmentsToDelete = from seg in dc.LabelledSegments
                                       where seg.RecordingID == recording.Id
                                       select seg;
                dc.LabelledSegments.DeleteAllOnSubmit(segmentsToDelete);
                dc.SubmitChanges();
            }
        }

        /// <summary>
        /// Deletes the segment provided as a parameter and identified
        /// by it's Id.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
        private static void DeleteSegment(LabelledSegment segment)
        {
            if (segment!=null && segment.Id > 0)
            {
                BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
                var segmentToDelete=(from seg in dc.LabelledSegments
                                    where seg.Id==segment.Id
                                    select seg).Single();
                dc.LabelledSegments.DeleteOnSubmit(segmentToDelete);
                dc.SubmitChanges();
            }
        }

        /// <summary>
        /// Inserts the recording session provided into the database.
        /// An existing 'identical' record has either the same Id, or
        /// the same date and location, or same date and GPS co-ords.
        /// If there is a matching session then the existing session is
        /// updated to the new data, even if the new data is incomplete.
        /// If there is no existing session then the new session must be
        /// validated before being entered into the database.
        /// The function returns an informative error string if the update/
        /// insertion fails, or a null/empty string if the process is
        /// successful.
        /// </summary>
        /// <param name="newSession">The new session.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static string InsertRecordingSession(RecordingSession newSession)
        {
            String result = "";
            try {
                DBAccess.UpdateRecordingSession(newSession);
            }catch(Exception ex)
            {
                result = ex.Message;
            }
            return (result);
        }

        

        internal static void MoveTagDown(BatTag tag)
        {
            DBAccess.MoveTag(tag, 1);
        }

        
        internal static List<string> GetOperators()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            List<string> operators = ((from op in dc.RecordingSessions
                                       select op.Operator).Distinct()).ToList<string>();
            return (operators);
        }

        /// <summary>
        /// Updates the recording session if it already exists in the database
        /// or adds it to the database
        /// </summary>
        /// <param name="sessionForFolder">The session for folder.</param>
        /// <exception cref="System.NotImplementedException"></exception>
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
            }
            dc.SubmitChanges();
        }

        /// <summary>
        /// Gets the microphone list .
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static List<string> GetMicrophoneList()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            List<String> result = (from sess in dc.RecordingSessions
                                   select sess.Microphone).Distinct().ToList<String>();
            return (result);
        }

        /// <summary>
        /// Gets the recording session list.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static List<RecordingSession> GetRecordingSessionList()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            List<RecordingSession> result = dc.RecordingSessions.ToList<RecordingSession>();
            return (result);
        }

        /// <summary>
        /// Updates the recording.  Adds it to the database if not already present
        /// or modifies the existing entry to match if present.  The measure of
        /// presence depends on the Name filed which is the name of the wav file
        /// and should be unique in the database.
        /// </summary>
        /// <param name="recording">The recording.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static string UpdateRecording(Recording recording, List<SegmentAndBatList> listOfSegmentAndBatList)
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
                    var existingRecordings = (from rec in dc.Recordings
                                              where rec.RecordingName == recording.RecordingName || rec.Id == recording.Id
                                              select rec);
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
                    DBAccess.UpdateLabelledSegments(listOfSegmentAndBatList, existingRecording.Id, dc);
                }
                return (errmsg);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("UpdateRecording - " + ex.Message);
                return (ex.Message);
            }
        }

        private static void UpdateLabelledSegments(List<SegmentAndBatList> listOfSegmentAndBatList, int id, BatReferenceDBLinqDataContext dc)
        {
            if (listOfSegmentAndBatList != null && listOfSegmentAndBatList.Count() > 0)
            {
                foreach (var seg in listOfSegmentAndBatList)
                {
                    UpdateLabelledSegment(seg, id, dc);
                }
            }
        }

        /// <summary>
        /// Updates the labelled segments. using the data in the combinedSgementsAndPasses
        /// and linked to the recording identified by the Id.  Also adds data to the
        /// extendedBatPasses table.
        /// </summary>
        /// <param name="segmentAndBatList">The combined segments and passes.</param>
        /// <param name="recordingId">The identifier.</param>
        /// <param name="dc">The dc.</param>
        /// <exception cref="System.NotImplementedException"></exception>
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
                               where seg.RecordingID == recordingId && seg.StartOffset == segmentAndBatList.segment.StartOffset
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
        /// Updates the extended bat pass, or inserts it if it does not already exist in the database
        /// </summary>
        /// <param name="pass">The new pass to be added or merged.</param>
        /// <param name="segment">the parent LabelledSegment that this pass belongs to</param>
        /// <param name="dc">The dc.</param>
        /// <exception cref="System.NotImplementedException"></exception>
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
        /// Validates the recording.  Confirms that the recording structure
        ///contains valid and complete data, or returns a suitable
        /// and informative error message.
        ///
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private static String ValidateRecording(Recording recording)
        {
            String result = "";

            return (result);
        }

        /// <summary>
        /// Gets the equipment list.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        internal static List<string> GetEquipmentList()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            List<String> result = (from sess in dc.RecordingSessions
                                   select sess.Equipment).Distinct().ToList<String>();
            return (result);
        }

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
        /// Deletes the bat passed as a parameter, and re-indexxes the sort order
        /// </summary>
        /// <param name="selectedBat">The selected bat.</param>
        /// <exception cref="System.NotImplementedException"></exception>
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
        /// Moves the bat.
        /// Moves the indicated bat up or down in the sorted bat list by the amount indicated
        /// in the second parameter
        /// </summary>
        /// <param name="bat">The bat.</param>
        /// <param name="v">The v.</param>
        /// <exception cref="System.NotImplementedException"></exception>
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
        /// Merges the tags.
        /// </summary>
        /// <param name="existingBat">The existing bat.</param>
        /// <param name="bat">The bat.</param>
        /// <param name="dataContext">The data context.</param>
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
        /// Gets the named bat.
        /// Returns the bat identified by a specified Name
        /// or null if the bat does not exist in the database
        /// </summary>
        /// <param name="newBatName">New name of the bat.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static Bat GetNamedBat(string newBatName)
        {
            BatReferenceDBLinqDataContext dataContext = DBAccess.GetDataContext();
            return (GetNamedBat(newBatName, dataContext));
        }

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
        /// Inserts the bat.
        /// Adds the supplied bat to the database.  It is assumed that
        /// the bat has been verified and that it does not already exist
        /// in the database
        /// </summary>
        /// <param name="bat">The bat.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
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

        public static string InsertBat(Bat bat)
        {
            BatReferenceDBLinqDataContext dataContext = DBAccess.GetDataContext();
            return (InsertBat(bat, dataContext));
        }

        /// <summary>
        /// Gets the matching bat.
        /// Returns a bat from the database which has the same
        /// genus and species as the bat passes as a parameter
        /// or null if no matching bat is found.
        /// If more than one matching bat is found (should not
        /// happen) will return the one with the lowest sortIndex.
        /// </summary>
        /// <param name="bat">The bat.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static Bat GetMatchingBat(Bat bat, BatReferenceDBLinqDataContext dataContext)
        {
            try
            {
                if (bat == null) return (null);
                var sortedMatchingBats = from b in dataContext.Bats
                                         where b.Batgenus == bat.Batgenus &&
                                           b.BatSpecies == bat.BatSpecies
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

        public static Bat GetMatchingBat(Bat bat)
        {
            BatReferenceDBLinqDataContext dataContext = DBAccess.GetDataContext();
            return (GetMatchingBat(bat, dataContext));
        }

        /// <summary>
        /// Validates the bat.
        /// Checkes to see if the required fields exist and are valid in format
        /// and if so returns an empty string.
        /// Otherwise returns a string identifying which fields are missing
        /// or incorrect.
        /// </summary>
        /// <param name="newBat">The new bat.</param>
        /// <returns></returns>
        public static string ValidateBat(Bat newBat)
        {
            String message = "";
            if (String.IsNullOrWhiteSpace(newBat.Name ))
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
        /// Returns a list of all known bats sorted on SortOrder
        /// </summary>
        /// <returns></returns>
        public static List<Bat> GetSortedBatList()
        {
            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();
            List<Bat> result = (from bat in dc.Bats
                                orderby bat.SortIndex
                                select bat).ToList<Bat>();
            return (result);
        }

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
        /// Resequences the bats by SortIndex, retaining the original sequence
        /// as much as possible.
        /// </summary>
        /// <exception cref="System.NotImplementedException"></exception>
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

        private static void copyXMLDataToDatabase(string xmlFile, BatReferenceDBLinqDataContext batReferenceDataContext)
        {
            var xmlBats = XElement.Load(xmlFile).Descendants("Bat");
            short i = 0;
            foreach (XElement bat in xmlBats)
            {
                MergeXMLBatToDB(bat, batReferenceDataContext, i++);
            }
        }

        private static void MergeXMLBatToDB(XElement bat, BatReferenceDBLinqDataContext batReferenceDataContext, short i)
        {
            Bat batToMerge = ConvertXMLBat(bat);
            DBAccess.MergeBat(batToMerge);
        }

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

        internal static void MoveTagUp(BatTag tag)
        {
            DBAccess.MoveTag(tag, -1);
        }

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
        /// Gets the stats for recording.  Given the ID of a specific recording
        /// produces a list with an element for each bat type that was present
        /// in the recording and the number of passes and the min, max, mean durations
        /// of each pass or labelled segment.
        /// </summary>
        /// <param name="recordingId">The recording identifier.</param>
        /// <returns></returns>
        internal static List<BatStats> GetStatsForRecording(int recordingId)
        {
            List<BatStats> result = new List<BatStats>();
            if (recordingId <= 0) return (result);

            BatReferenceDBLinqDataContext dc = DBAccess.GetDataContext();

            var batSegments = from link in dc.BatSegmentLinks
                              where link.LabelledSegment.RecordingID == recordingId
                              select link;
            if (batSegments != null && batSegments.Count() > 0)
            {
                // batSegments contains entries for each unique segment/bat combination
                // in the specified recording
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

        
    }
}