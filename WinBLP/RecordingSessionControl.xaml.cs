using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for RecordingSessionControl.xaml
    /// </summary>
    public partial class RecordingSessionControl : UserControl
    {
        #region recordingSession

        /// <summary>
        ///     recordingSession Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingSessionProperty =
            DependencyProperty.Register("recordingSession", typeof(RecordingSession), typeof(RecordingSessionControl),
                new FrameworkPropertyMetadata((RecordingSession)new RecordingSession()));

        /// <summary>
        ///     Gets or sets the recordingSession property. This dependency property indicates ....
        /// </summary>
        public RecordingSession recordingSession
        {
            get
            {
                RecordingSession session = (RecordingSession)GetValue(recordingSessionProperty);
                if (session != null)
                {
                    session.SessionTag = SessionTagTextBlock.Text;
                    session.SessionDate = SessionDatePicker.SelectedDate ?? new DateTime();
                    session.SessionStartTime = (StartTimePicker.Value ?? new DateTime()).TimeOfDay;
                    session.SessionEndTime = (EndTimePicker.Value ?? new DateTime()).TimeOfDay;
                    session.Temp = (short?)TemperatureIntegerUpDown.Value;
                    session.Equipment = EquipmentComboBox.Text;
                    session.Microphone = MicrophoneComboBox.Text;
                    session.OriginalFilePath = FolderTextBox.Text;

                    session.Location = LocationComboBox.Text;

                    decimal value;
                    Decimal.TryParse(GPSLatitudeTextBox.Text, out value);
                    session.LocationGPSLatitude = value;
                    value = 0.0m;
                    Decimal.TryParse(GPSLongitudeTextBox.Text, out value);
                    session.LocationGPSLongitude = value;

                    session.Operator = OperatorComboBox.Text;
                    session.SessionNotes = SessionNotesRichtextBox.Text;
                }
                return (session);
            }
            set
            {
                if (value != null)
                {
                    selectedFolder = value.SessionTag;
                    SessionTagTextBlock.Text = value.SessionTag;
                    StartTimePicker.Value = new DateTime() + (value.SessionStartTime ?? new TimeSpan());

                    EndTimePicker.Value = new DateTime() + (value.SessionEndTime ?? new TimeSpan());
                    EquipmentComboBox.ItemsSource = DBAccess.GetEquipmentList();
                    EquipmentComboBox.Text = value.Equipment;
                    EquipmentComboBox.SelectedItem = value.Equipment;

                    MicrophoneComboBox.ItemsSource = DBAccess.GetMicrophoneList();
                    MicrophoneComboBox.Text = value.Microphone;
                    MicrophoneComboBox.SelectedItem = value.Microphone;

                    LocationComboBox.ItemsSource = DBAccess.GetLocationList();
                    LocationComboBox.Text = value.Location;
                    LocationComboBox.SelectedItem = value.Location;

                    OperatorComboBox.ItemsSource = DBAccess.GetOperators();
                    OperatorComboBox.Text = value.Operator;
                    OperatorComboBox.SelectedItem = value.Operator;

                    GPSLatitudeTextBox.Text = (value.LocationGPSLatitude ?? 0.0m).ToString();
                    GPSLongitudeTextBox.Text = (value.LocationGPSLongitude ?? 0.0m).ToString();

                    SessionNotesRichtextBox.Text = value.SessionNotes;

                    SessionDatePicker.DisplayDate = value.SessionDate;
                    SessionDatePicker.SelectedDate = value.SessionDate;
                    TemperatureIntegerUpDown.Value = value.Temp;
                    FolderTextBox.Text = value.OriginalFilePath;
                }
                else
                {
                    SessionTagTextBlock.Text = "";
                    StartTimePicker.Value = DateTime.Now;
                    EndTimePicker.Value = DateTime.Now;

                    EquipmentComboBox.Text = "";
                    MicrophoneComboBox.Text = "";
                    LocationComboBox.Text = "";
                    GPSLatitudeTextBox.Text = "";
                    GPSLongitudeTextBox.Text = "";
                    OperatorComboBox.Text = "";
                    SessionNotesRichtextBox.Text = "";
                    SessionDatePicker.DisplayDate = DateTime.Now;
                    SessionDatePicker.SelectedDate = DateTime.Now;
                    TemperatureIntegerUpDown.Value = null;
                    FolderTextBox.Text = "";
                }
                SetValue(recordingSessionProperty, value);
            }
        }

        #endregion recordingSession

        #region selectedFolder

        /// <summary>
        ///     selectedFolder Dependency Property
        /// </summary>
        public static readonly DependencyProperty selectedFolderProperty =
            DependencyProperty.Register("selectedFolder", typeof(String), typeof(RecordingSessionControl),
                new FrameworkPropertyMetadata((String)""));

        /// <summary>
        ///     Gets or sets the selectedFolder property. This dependency property indicates ....
        /// </summary>
        public String selectedFolder
        {
            get { return (String)GetValue(selectedFolderProperty); }
            set { SetValue(selectedFolderProperty, value); }
        }

        #endregion selectedFolder

        private ObservableCollection<String> equipmentList = new ObservableCollection<String>();
        private ObservableCollection<String> locationList = new ObservableCollection<String>();
        private ObservableCollection<String> microphoneList = new ObservableCollection<String>();
        private ObservableCollection<String> operatorList = new ObservableCollection<String>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="RecordingSessionControl"/> class.
        /// </summary>
        public RecordingSessionControl()
        {
            selectedFolder = "";
            equipmentList = DBAccess.GetEquipmentList();
            microphoneList = DBAccess.GetMicrophoneList();
            operatorList = DBAccess.GetOperators();
            locationList = DBAccess.GetLocationList();

            InitializeComponent();
            this.DataContext = this;
            MicrophoneComboBox.ItemsSource = microphoneList;
            EquipmentComboBox.ItemsSource = equipmentList;
            OperatorComboBox.ItemsSource = operatorList;
            LocationComboBox.ItemsSource = locationList;
        }

        /// <summary>
        ///     Verifies the form contents.
        /// </summary>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        public string VerifyFormContents()
        {
            string result = "";

            if (String.IsNullOrWhiteSpace(recordingSession.SessionTag))
            {
                result = "Must have a valid session Tag";
            }
            DateTime? date = recordingSession.SessionDate;
            if (date == null)
            {
                result = "Must have a valid date";
            }
            else
            {
                if (date.Value.Year < 1990)
                {
                    result = "Must have a valid date later than 1990";
                }
                if (date.Value > DateTime.Now)
                {
                    result = "Must have a valid date earlier than now";
                }
            }
            if (String.IsNullOrWhiteSpace(recordingSession.Location))
            {
                result = "Must have a valid Location";
            }

            return (result);
        }

        private void FolderBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            FileBrowser fileBrowser = new FileBrowser();
            fileBrowser.SelectFolder();
            FolderTextBox.Text = fileBrowser.WorkingFolder;
        }

        private void GPSLatitudeTextBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Tuple<decimal, decimal> coordinates;
            decimal lat = 100.0m;
            decimal longit = 200.0m;
            if (!decimal.TryParse(GPSLatitudeTextBox.Text, out lat)) return;
            if (!decimal.TryParse(GPSLongitudeTextBox.Text, out longit)) return;
            if (!(lat <= 90.0m && lat >= -90.0m && longit <= 180.0m && longit >= -180.0m)) return;
            coordinates = new Tuple<decimal, decimal>(lat, longit);

            MapWindow mapWindow = new MapWindow();
            mapWindow.mapControl.coordinates = coordinates;
            mapWindow.Show();
            if (recordingSession != null && recordingSession.Recordings != null && recordingSession.Recordings.Count > 0)
            {
                int i = 0;
                foreach (var rec in recordingSession.Recordings)
                {
                    i++;
                    double latitude = 100;
                    double longitude = 200;
                    if (Double.TryParse(rec.RecordingGPSLatitude, out latitude))
                    {
                        if (Double.TryParse(rec.RecordingGPSLongitude, out longitude))
                        {
                            if (latitude <= 90.0 && latitude >= -90.0 && longitude <= 180.0 && longitude >= -180.0)
                            {
                                mapWindow.mapControl.AddPushPin(new Tuple<double, double>(latitude, longitude), i.ToString());
                            }
                        }
                    }
                }
            }
        }
    }
}