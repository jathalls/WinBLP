using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Linq;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BatRecordingManager
{
    /*
    RecordingDetailGrid
    RecordingNameTextBox - Binding {RecordinName}
    BrowseForFileButton
    GPSLatitudeTextBox - Binding {RecordingGPSLatitude}
    GPSLongitudeTextBox - Binding {RecordingGPSLongitude}
    StartTimeTimePicker - Binding {RecordingStartTime}
    EndTimeTimePicker - Binding {RecordingEndTime}
    RecordingNotesTextBox - Binding {RecordingNotes}
    LabelledSegmentsListView
    ButtonBarStackPanel
    OKButton
    CancelButton

        RecordingForm
	LabelledSegmentsList(P)
	ModifiedFlag
	recording(P)
		Get
			populates recording from form fields
		Set
			populates LabelledSegmentsList and ModifiedFlag from value
			populates form fields from value
	RecordingForm()
		DataContext=this
	AddEditSegment()
		Creates and Shows segmentForm
		if OK adds the new segment to the recording
	AddSegmentButton_Click - calls AddEditSegment
	BrowseForFileButton_Click()
		Creates a new FileBrowser()
		fileBrowser.SelectWavFile()
		recording.RecordingName=selected file name
		updates the form RecordingName text box
	ButtonSaveSegment_Click()
		Extracts the segment for the specific button clicked
		FileProcessor.IsLabelFileLine() extracts times and comments
		segment modified with new values
		Button set to hidden
	CancelButton_Click()
		sets DialogResult to false
		closes the dialog
	DeleteSegmentButton_Click()
		extracts the selected segment
		Displays warning MessageBox for confirmation
		DBAccess.DeleteSegment()
		DBAccess.GetRecording() // loses any added segments not yet committed
		sets the selected segment to be the same or nearest index
	OKButton_Click()
		foreach segment in the listview
			DBAccess.GetDescribedBats()
			Tools.FormattedSegmentLine()
			FileProcessor.ProcessLabelledSegment(formattedLine,describedBats)
			set segment ID
			processedSegments.Add()
		DBAccess.UpdateRecording(recording,processedSegments)
		set DialogResult
		Close dialog
	OnListViewItemFocused()
		sender as ListViewItem.Iselected=true
	OnTextBoxFocused()
		extract Button connected to this textbox
		make the button Visible

    */

    /// <summary>
    ///     Interaction logic for RecordingForm.xaml
    /// </summary>
    public partial class RecordingForm : Window
    {
        #region LabelledSegmentsList

        /// <summary>
        ///     LabelledSegmentsList Dependency Property
        /// </summary>
        public static readonly DependencyProperty LabelledSegmentsListProperty =
            DependencyProperty.Register("LabelledSegmentsList", typeof(ObservableCollection<LabelledSegment>), typeof(RecordingForm),
                new FrameworkPropertyMetadata((ObservableCollection<LabelledSegment>)new ObservableCollection<LabelledSegment>()));

        /// <summary>
        ///     Gets or sets the LabelledSegmentsList property. This dependency property indicates ....
        /// </summary>
        public ObservableCollection<LabelledSegment> LabelledSegmentsList
        {
            get
            {
                return (ObservableCollection<LabelledSegment>)GetValue(LabelledSegmentsListProperty);
            }
            set
            {
                SetValue(LabelledSegmentsListProperty, value);
                ModifiedFlag = new ObservableCollection<bool>();
                if (value.Count > 0)
                {
                    foreach (var item in value)
                    {
                        ModifiedFlag.Add(false);
                    }
                }
            }
        }

        #endregion LabelledSegmentsList

        private ObservableCollection<bool> ModifiedFlag = new ObservableCollection<bool>();

        #region recording

        /// <summary>
        ///     recording Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingProperty =
            DependencyProperty.Register("recording", typeof(Recording), typeof(RecordingForm),
                new FrameworkPropertyMetadata((Recording)new Recording()));

        /// <summary>
        ///     Gets or sets the recording property. This dependency property indicates ....
        /// </summary>
        public Recording recording
        {
            get
            {
                Recording recording = (Recording)GetValue(recordingProperty);
                recording.RecordingEndTime = new TimeSpan((EndTimeTimePicker.Value ?? DateTime.Now).Ticks);
                recording.RecordingStartTime = new TimeSpan((StartTimeTimePicker.Value ?? DateTime.Now).Ticks);
                recording.RecordingName = RecordingNameTextBox.Text;
                recording.RecordingNotes = RecordingNotesTextBox.Text;
                recording.RecordingGPSLatitude = GPSLatitudeTextBox.Text;
                recording.RecordingGPSLongitude = GPSLongitudeTextBox.Text;

                return (recording);
            }
            set
            {
                SetValue(recordingProperty, value);
                LabelledSegmentsList = new ObservableCollection<LabelledSegment>();

                if (value != null)
                {
                    if (value.LabelledSegments != null && value.LabelledSegments.Count() > 0)
                    {
                        LabelledSegmentsList = new ObservableCollection<LabelledSegment>(value.LabelledSegments);
                    }
                }
                //LabelledSegmentsListView.ItemsSource = LabelledSegmentsList;
                EndTimeTimePicker.Value = new DateTime((value.RecordingEndTime ?? new TimeSpan(22, 0, 0)).Ticks);
                StartTimeTimePicker.Value = new DateTime((value.RecordingStartTime ?? new TimeSpan(18, 0, 0)).Ticks);
                RecordingNameTextBox.Text = value.RecordingName ?? "";
                RecordingNotesTextBox.Text = value.RecordingNotes ?? "";
                GPSLatitudeTextBox.Text = value.RecordingGPSLatitude ?? "";
                GPSLongitudeTextBox.Text = value.RecordingGPSLongitude ?? "";
            }
        }

        #endregion recording

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordingForm"/> class.
        /// </summary>
        public RecordingForm()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        /// <summary>
        ///     Adds a new segment. Editing is not done here but is done in place.
        /// </summary>
        /// <param name="segmentToEdit">
        ///     The segment to edit.
        /// </param>
        private void AddEditSegment(LabelledSegment segmentToEdit)
        {
            LabelledSegmentForm segmentForm = new LabelledSegmentForm();
            int os = LabelledSegmentsListView.SelectedIndex;
            if (segmentToEdit == null)
            {
                segmentForm.labelledSegment = new LabelledSegment();
            }
            else
            {
                segmentForm.labelledSegment = segmentToEdit;
            }

            if ((segmentForm.ShowDialog() ?? false))
            {
                if (segmentForm.DialogResult ?? false)
                {
                    Debug.WriteLine("AdEditSegment OK");
                    Debug.Write(recording.LabelledSegments.Count + " -> ");
                    recording.LabelledSegments.Add(segmentForm.labelledSegment);
                    LabelledSegmentsList = new ObservableCollection<LabelledSegment>(recording.LabelledSegments);

                    var view = CollectionViewSource.GetDefaultView(LabelledSegmentsListView.ItemsSource);
                    if (view != null) view.Refresh();
                    Debug.WriteLine(LabelledSegmentsListView.Items.Count);

                    /*
                    segmentToEdit = segmentForm.labelledSegment;
                    if (segmentToEdit.RecordingID <= 0)
                    {
                        segmentToEdit.RecordingID = recording.Id;
                        recording.LabelledSegments.Add(segmentToEdit);
                    }
                    else
                    {
                        bool done = false;
                        for (int i = 0; i < recording.LabelledSegments.Count; i++)
                        {
                            if (recording.LabelledSegments[i].Id == segmentToEdit.Id)
                            {
                                recording.LabelledSegments[i].StartOffset = segmentToEdit.StartOffset;
                                recording.LabelledSegments[i].EndOffset = segmentToEdit.EndOffset;
                                recording.LabelledSegments[i].Comment = segmentToEdit.Comment;
                                done = true;
                            }
                        }
                        if (!done)
                        {
                            recording.LabelledSegments.Add(segmentToEdit);
                        }
                    }

                    DBAccess.UpdateRecording(recording);

                    ICollectionView view = CollectionViewSource.GetDefaultView(LabelledSegmentsListView.ItemsSource);
                    view.Refresh();
                    if (os >= 0 && os < LabelledSegmentsListView.Items.Count)
                    {
                        LabelledSegmentsListView.SelectedIndex = os;
                    }*/
                }
            }
        }

        /// <summary>
        ///     Handles the 1 event of the AddSegmentButton_Click control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void AddSegmentButton_Click_1(object sender, RoutedEventArgs e)
        {
            AddEditSegment(null);
        }

        /// <summary>
        ///     Handles the Click event of the BrowseForFileButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void BrowseForFileButton_Click(object sender, RoutedEventArgs e)
        {
            FileBrowser fileBrowser = new FileBrowser();
            fileBrowser.SelectWavFile();
            if (fileBrowser.TextFileNames != null && fileBrowser.TextFileNames.Count > 0)
            {
                string filename = fileBrowser.GetUnqualifiedFilename(0);
                RecordingNameTextBox.Text = filename;
            }

            recording.RecordingName = RecordingNameTextBox.Text;
        }

        /// <summary>
        ///     Handles the Click event of the ButtonSaveSegment control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void ButtonSaveSegment_Click(object sender, RoutedEventArgs e)
        {
            Button thisButton = sender as Button;
            String text = ((thisButton.Parent as WrapPanel).Children[1] as TextBox).Text;

            LabelledSegment segment = ((thisButton.Parent as WrapPanel).TemplatedParent as ContentPresenter).Content as LabelledSegment;

            LabelledSegmentsListView.SelectedItem = segment;
            int index = LabelledSegmentsListView.SelectedIndex;

            TimeSpan start;
            TimeSpan end;
            string comment;
            if (FileProcessor.IsLabelFileLine(text, out start, out end, out comment))
            {
                segment.StartOffset = start;
                segment.EndOffset = end;
                segment.Comment = comment;
                thisButton.Visibility = Visibility.Hidden; // hide the button again now it has successfully saved
            }
            if (index >= 0 && index < recording.LabelledSegments.Count)
            {
                recording.LabelledSegments[index].StartOffset = segment.StartOffset;
                recording.LabelledSegments[index].EndOffset = segment.EndOffset;
                recording.LabelledSegments[index].Comment = segment.Comment;
                LabelledSegmentsList = new ObservableCollection<LabelledSegment>(recording.LabelledSegments);
                //LabelledSegmentsListView.ItemsSource = LabelledSegmentsList;
            }
        }

        /// <summary>
        ///     Handles the Click event of the CancelButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        /// <summary>
        ///     Handles the 1 event of the DeleteSegmentButton_Click control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void DeleteSegmentButton_Click_1(object sender, RoutedEventArgs e)
        {
            LabelledSegment segmentToDelete = null;
            if (LabelledSegmentsListView == null) return;

            if (LabelledSegmentsListView.Items == null) return;
            if (LabelledSegmentsListView.Items.Count <= 0) return;
            try
            {
                int indexToDelete = LabelledSegmentsListView.SelectedIndex;

                segmentToDelete = LabelledSegmentsList[indexToDelete];
                if (segmentToDelete == null) return;
            }
            catch (Exception)
            {
                return;
            }
            int index = LabelledSegmentsListView.SelectedIndex;
            var result = MessageBox.Show("Are you sure you want to permanently delete this Segment?", "Deleting \"" + segmentToDelete.Comment + "\"", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                DBAccess.DeleteSegment(segmentToDelete);
            }

            recording.LabelledSegments.Remove(segmentToDelete);
            var processedSegments = recording.LabelledSegments;
            recording = DBAccess.GetRecording(recording.Id);
            recording.LabelledSegments = processedSegments;
            //LabelledSegmentsListView.ItemsSource = recording.LabelledSegments;
            if (index >= recording.LabelledSegments.Count)
            {
                index = recording.LabelledSegments.Count - 1;
            }
            LabelledSegmentsListView.SelectedIndex = index;
        }

        private void MapButton_Click(object sender, RoutedEventArgs e)
        {
            MapWindow map = new MapWindow(true);
            Location location = Tools.ValidCoordinates(GPSLatitudeTextBox.Text, GPSLongitudeTextBox.Text);
            if (location != null)
            {
                map.Coordinates = location;
            }
            if (map.ShowDialog() ?? false)
            {
                if (map.DialogResult ?? false)
                {
                    location = Tools.ValidCoordinates(map.lastSelectedLocation);
                    if (location != null)
                    {
                        GPSLatitudeTextBox.Text = location.Latitude.ToString();
                        GPSLongitudeTextBox.Text = location.Longitude.ToString();
                    }
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the OKButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            string err = "";

            ObservableCollection<SegmentAndBatList> processedSegments = new ObservableCollection<SegmentAndBatList>();
            if (LabelledSegmentsListView.Items != null && LabelledSegmentsListView.Items.Count > 0)
            {
                foreach (var seg in LabelledSegmentsListView.Items)
                {
                    LabelledSegment segment = seg as LabelledSegment;
                    ObservableCollection<Bat> bats = DBAccess.GetDescribedBats(segment.Comment);
                    String segmentLine = Tools.FormattedSegmentLine(segment);
                    SegmentAndBatList thisProcessedSegment = FileProcessor.ProcessLabelledSegment(segmentLine, bats);
                    thisProcessedSegment.segment.Id = segment.Id;
                    processedSegments.Add(thisProcessedSegment);
                }
            }
            err = DBAccess.UpdateRecording(recording, processedSegments);

            if (String.IsNullOrWhiteSpace(err))
            {
                DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show(err);
            }
        }

        /// <summary>
        ///     Called when [ListView item focused].
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void OnListViewItemFocused(object sender, RoutedEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            lvi.IsSelected = true;
        }

        /// <summary>
        ///     Called when [text box focused].
        /// </summary>
        /// <param name="sender">
        ///     The sender.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void OnTextBoxFocused(object sender, RoutedEventArgs e)
        {
            TextBox segmentTextBox = sender as TextBox;
            Button mySaveButton = ((segmentTextBox.Parent as WrapPanel).Children[0] as Button);
            mySaveButton.Visibility = Visibility.Visible;
        }
    }
}