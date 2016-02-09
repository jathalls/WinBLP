using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for RecordingSessionListDetailControl.xaml
    /// </summary>
    public partial class RecordingSessionListDetailControl : UserControl
    {
        #region recordingSessionList

        /// <summary>
        ///     recordingSessionList Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingSessionListProperty =
            DependencyProperty.Register("recordingSessionList", typeof(ObservableCollection<RecordingSession>), typeof(RecordingSessionListDetailControl),
                new FrameworkPropertyMetadata((ObservableCollection<RecordingSession>)new ObservableCollection<RecordingSession>()));

        /// <summary>
        ///     The _displayed recordings
        /// </summary>
        private ObservableCollection<Recording> _displayedRecordings;

        /// <summary>
        ///     Gets or sets the displayed recordings.
        /// </summary>
        /// <value>
        ///     The displayed recordings.
        /// </value>
        public ObservableCollection<Recording> displayedRecordings
        {
            get
            {
                return (_displayedRecordings);
            }
            set
            {
                _displayedRecordings = value;
                if (value != null && RecordingsListControl != null)
                {
                    RecordingsListControl.recordingsList = displayedRecordings;
                }
            }
        }

        /// <summary>
        ///     Gets or sets the recordingSessionList property. This dependency property indicates ....
        /// </summary>
        public ObservableCollection<RecordingSession> recordingSessionList
        {
            get
            {
                return (ObservableCollection<RecordingSession>)GetValue(recordingSessionListProperty);
            }
            set
            {
                SetValue(recordingSessionListProperty, value);
                if (value != null && RecordingSessionListView != null)
                {
                    RecordingSessionListView.ItemsSource = value;
                }
            }
        }

        #endregion recordingSessionList

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordingSessionListDetailControl"/> class.
        /// </summary>
        public RecordingSessionListDetailControl()
        {
            recordingSessionList = new ObservableCollection<RecordingSession>();
            displayedRecordings = new ObservableCollection<Recording>();

            InitializeComponent();
            this.DataContext = this;

            //RecordingsListView.ItemsSource = displayedRecordingControls;
        }

        /// <summary>
        ///     Refreshes the data in the display when this pane is made visible; It might slow down
        ///     context switches, but is necessary if other panes have changed the data. A more
        ///     sophisticated approach would be to have any display set a 'modified' flag which
        ///     would trigger the update or not as necessary;
        /// </summary>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal void RefreshData()
        {
            int old_selection;

            old_selection = RecordingSessionListView.SelectedIndex;
            recordingSessionList = DBAccess.GetRecordingSessionList();
            RecordingSessionListView.ItemsSource = recordingSessionList;
            RecordingSessionListView.SelectedIndex = old_selection;
            CollectionViewSource.GetDefaultView(RecordingSessionListView.ItemsSource).Refresh();
        }

        /// <summary>
        ///     Selects the specified recording session.
        /// </summary>
        /// <param name="recordingSession">
        ///     The recording session.
        /// </param>
        internal void Select(RecordingSession recordingSession)
        {
            for (int i = 0; i < RecordingSessionListView.Items.Count; i++)
            {
                RecordingSession session = RecordingSessionListView.Items[i] as RecordingSession;
                if (session.Id == recordingSession.Id)
                {
                    RecordingSessionListView.SelectedIndex = i;
                    break;
                }
            }
        }

        private void AddEditRecordingSession(RecordingSessionForm recordingSessionForm)
        {
            int selectedIndex = RecordingSessionListView.SelectedIndex;

            string error = "No Data Entered";

            if (!recordingSessionForm.ShowDialog() ?? false)
            {
                if (recordingSessionForm.DialogResult ?? false)
                {
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        MessageBox.Show(error);
                    }
                }
            }
            this.recordingSessionList = DBAccess.GetRecordingSessionList();
            if (selectedIndex >= 0 && selectedIndex <= this.RecordingSessionListView.Items.Count)
            {
                RecordingSessionListView.SelectedIndex = selectedIndex;
            }
        }

        private void AddRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingSessionForm recordingSessionForm = new RecordingSessionForm();

            recordingSessionForm.Clear();
            RecordingSession newSession = new RecordingSession();
            newSession.SessionDate = DateTime.Today;
            newSession.SessionStartTime = new TimeSpan(18, 0, 0);
            newSession.SessionEndTime = new TimeSpan(24, 0, 0);
            recordingSessionForm.SetRecordingSession(newSession);

            AddEditRecordingSession(recordingSessionForm);
        }

        /// <summary>
        ///     Condenses the stats list. Given a List of BatStats for a wide collection of bats and
        ///     passes, condenses it to have a single BatStat for each bat type along with the
        ///     cumulative number of passes and segments.
        /// </summary>
        /// <param name="statsForSession">
        ///     The stats for session.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private ObservableCollection<BatStats> CondenseStatsList(ObservableCollection<BatStats> statsForSession)
        {
            ObservableCollection<BatStats> result = new ObservableCollection<BatStats>();
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

        private void DeleteRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (RecordingSessionListView.SelectedItem != null)
            {
                RecordingSession session = RecordingSessionListView.SelectedItem as RecordingSession;
                DBAccess.DeleteSession(session);
                recordingSessionList = DBAccess.GetRecordingSessionList();
            }
        }

        private void EditRecordingSessionButton_Click(object sender, RoutedEventArgs e)
        {
            RecordingSessionForm form = new RecordingSessionForm();
            if (RecordingSessionListView.SelectedItem != null)
            {
                form.recordingSessionControl.recordingSession = RecordingSessionListView.SelectedItem as RecordingSession;
            }
            else
            {
                form.Clear();
            }
            AddEditRecordingSession(form);
        }

        private void OnListViewItemFocused(object sender, RoutedEventArgs e)
        {
            //ListViewItem lvi = sender as D
            //lvi.IsSelected = true;
            //lvi.BringIntoView();
        }

        /// <summary>
        ///     Handles the SelectionChanged event of the RecordingSessionListView control.
        ///     Selection has changed in the list, so update the details panel with the newly
        ///     selected item.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="SelectionChangedEventArgs"/> instance containing the event data.
        /// </param>
        private void RecordingSessionListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            try
            {
                recordingSessionControl.recordingSession = (RecordingSession)RecordingSessionListView.SelectedItem;
                if (recordingSessionControl.recordingSession == null)
                {
                    SessionSummaryStackPanel.Children.Clear();

                    displayedRecordings.Clear();
                    RecordingsListControl.selectedSession = recordingSessionControl.recordingSession;
                }
                else
                {
                    ObservableCollection<BatStats> statsForSession = DBAccess.GetStatsForSession(recordingSessionControl.recordingSession);

                    statsForSession = CondenseStatsList(statsForSession);
                    SessionSummaryStackPanel.Children.Clear();
                    foreach (var batstat in statsForSession)
                    {
                        BatPassSummaryControl batPassSummary = new BatPassSummaryControl();
                        batPassSummary.Content = Tools.GetFormattedBatStats(batstat, false);
                        SessionSummaryStackPanel.Children.Add(batPassSummary);
                    }
                    displayedRecordings = new ObservableCollection<Recording>(recordingSessionControl.recordingSession.Recordings);
                    RecordingsListControl.selectedSession = recordingSessionControl.recordingSession;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }
    }
}