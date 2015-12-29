using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for RecordingSessionListDetailControl.xaml
    /// </summary>
    public partial class RecordingSessionListDetailControl : UserControl
    {
        #region recordingSessionList

        /// <summary>
        /// recordingSessionList Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingSessionListProperty =
            DependencyProperty.Register("recordingSessionList", typeof(List<RecordingSession>), typeof(RecordingSessionListDetailControl),
                new FrameworkPropertyMetadata((List<RecordingSession>)new List<RecordingSession>()));

        /// <summary>
        /// Gets or sets the recordingSessionList property.  This dependency property
        /// indicates ....
        /// </summary>
        public List<RecordingSession> recordingSessionList
        {
            get { return (List<RecordingSession>)GetValue(recordingSessionListProperty); }
            set
            {
                SetValue(recordingSessionListProperty, value);
                RecordingSessionListView.ItemsSource = value;
            }
        }

        #endregion recordingSessionList

        /// <summary>
        /// Initializes a new instance of the <see cref="RecordingSessionListDetailControl"/> class.
        /// </summary>
        public RecordingSessionListDetailControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        /// <summary>
        /// Handles the SelectionChanged event of the RecordingSessionListView control.
        /// Selection has changed in the list, so update the details panel with the newly
        /// selected item.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void RecordingSessionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            recordingSessionControl.recordingSession = (RecordingSession)RecordingSessionListView.SelectedItem;
            List<BatStats> statsForSession = DBAccess.GetStatsForSession(recordingSessionControl.recordingSession);

            var commonNames = (from stat in statsForSession
                               select stat.batCommonName).Distinct();
            foreach (var name in commonNames)
            {
                int passes = statsForSession.Where(ss => ss.batCommonName == name).Sum(pass => pass.passes);
                int segs = statsForSession.Where(ss => ss.batCommonName == name).Sum(seg => seg.segments);
                Debug.WriteLine(name + " " + passes + "/" + segs);
            }

            statsForSession = CondenseStatsList(statsForSession);
            SessionSummaryStackPanel.Children.Clear();
            foreach (var batstat in statsForSession)
            {
                BatPassSummaryControl batPassSummary = new BatPassSummaryControl();
                batPassSummary.Content = Tools.GetFormattedBatStats(batstat,false);
                SessionSummaryStackPanel.Children.Add(batPassSummary);
            }
            PopulateRecordingsList(recordingSessionControl.recordingSession);
        }

        /// <summary>
        /// Condenses the stats list.  Given a List of BatStats for a wide collection
        /// of bats and passes, condenses it to have a single BatStat for each bat
        /// type along with the cumulative number of passes and segments.
        /// </summary>
        /// <param name="statsForSession">The stats for session.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private List<BatStats> CondenseStatsList(List<BatStats> statsForSession)
        {
            List<BatStats> result = new List<BatStats>();
            foreach (var stat in statsForSession)
            {
                var matchingStats = from s in result
                                    where s.batCommonName == stat.batCommonName
                                    select s;
                if (matchingStats != null && matchingStats.Count() > 0)
                {
                    var existingStat = matchingStats.First();
                    existingStat.Add(stat);
                }
                else
                {
                    result.Add(stat);
                }
            }
            return (result);
        }

        /// <summary>
        /// Populates the segment list.
        /// The recordingSessionControl has been automatically updated by writing the
        /// selected session to it.  This function uses the selected recordingSession to
        /// fill in the list of LabelledSegments.
        /// </summary>
        /// <param name="recordingSession">The recording session.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void PopulateRecordingsList(RecordingSession recordingSession)
        {
            // TODO each recording will give access to a passes summary
            // these should be merged into a session summary and each 'bat'
            // in the summary must be added to the SessionSummaryStackPanel
            
            RecordingsListView.Items.Clear();
            if (recordingSession == null) return;
            foreach (var recording in recordingSession.Recordings)
            {
                RecordingItemControl recordingControl = new RecordingItemControl();
                recordingControl.recordingItem = recording;

                RecordingsListView.Items.Add(recordingControl);
            }
        }

        private void AddRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingSessionForm recordingSessionForm = new RecordingSessionForm();

            recordingSessionForm.Clear();
            recordingSessionForm.recordingSessionControl.recordingSession.SessionDate = DateTime.Now;
            recordingSessionForm.recordingSessionControl.recordingSession.SessionStartTime = new TimeSpan(18, 0, 0);
            recordingSessionForm.recordingSessionControl.recordingSession.SessionEndTime = new TimeSpan(22, 0, 0);
            AddEditRecordingSession(recordingSessionForm);

        }

        private void AddEditRecordingSession(RecordingSessionForm recordingSessionForm)
        {

            RecordingSession newSession = recordingSessionForm.GetRecordingSession();
            string error = "No Data Entered";


            if (!recordingSessionForm.ShowDialog() ?? false)
            {

                if (recordingSessionForm.DialogResult ?? false)
                {

                    error = DBAccess.InsertRecordingSession(newSession);
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        MessageBox.Show(error);
                    }
                }

            }
            this.recordingSessionList = DBAccess.GetRecordingSessionList();



        }

        private void EditRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingSessionForm form = new RecordingSessionForm();
            if(RecordingSessionListView.SelectedItem!= null)
            {
                form.recordingSessionControl.recordingSession = RecordingSessionListView.SelectedItem as RecordingSession;


            }
            else
            {
                form.Clear();
            }
            AddEditRecordingSession(form);

        }

        private void DeleteRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if(RecordingSessionListView.SelectedItem!= null)
            {
                RecordingSession session = RecordingSessionListView.SelectedItem as RecordingSession;
                DBAccess.DeleteSession(session);
                recordingSessionList = DBAccess.GetRecordingSessionList();
            }
        }

        private void AddRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            Recording recording = new Recording();
            AddEditRecording(recording);
        }

        private void EditRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            Recording recording = new Recording();
            if(RecordingsListView.SelectedItem!= null)
            {
                recording = (RecordingsListView.SelectedItem as RecordingItemControl).recordingItem;
            }
            AddEditRecording(recording);

        }

        private void AddEditRecording(Recording recording)
        {
            if (recording == null) recording = new Recording();

            RecordingForm recordingForm = new RecordingForm();
            recordingForm.recording = recording;
            
                if (recordingForm.ShowDialog() ?? false)
                {
                    if (recordingForm.DialogResult ?? false)
                    {
                        DBAccess.UpdateRecording(recordingForm.recording, null);
                        
                    }
                }
            
            PopulateRecordingsList((RecordingSessionListView.SelectedItem ?? null) as RecordingSession);
        }

        private void DeleteRecordingButton_Click(object sender, RoutedEventArgs e)
        {
            if(RecordingsListView.SelectedItem!= null)
            {
                RecordingItemControl selectedRecording = RecordingsListView.SelectedItem as RecordingItemControl;
                selectedRecording.DeleteRecording();
            }
            PopulateRecordingsList((RecordingSessionListView.SelectedItem ?? null) as RecordingSession);
        }
    }
}