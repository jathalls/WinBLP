using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for RecordingSessionControl.xaml
    /// </summary>
    public partial class RecordingSessionControl : UserControl
    {
        #region recordingSession

        /// <summary>
        /// recordingSession Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingSessionProperty =
            DependencyProperty.Register("recordingSession", typeof(RecordingSession), typeof(RecordingSessionControl),
                new FrameworkPropertyMetadata((RecordingSession)new RecordingSession()));

        /// <summary>
        /// Gets or sets the recordingSession property.  This dependency property
        /// indicates ....
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

                    session.Location = LocationtextBox.Text;

                    decimal value;
                    Decimal.TryParse(GPSLatitudeTextBox.Text, out value);
                    session.LocationGPSLatitude = value;
                    value = 0.0m;
                    Decimal.TryParse(GPSLongitudeTextBox.Text, out value);
                    session.LocationGPSLongitude = value;

                    session.Operator = OperatorTextBox.Text;
                    session.SessionNotes = SessionNotesRichtextBox.Text;
                }
                return (session);
            }
            set
            {
                if (value != null)
                {
                    SessionTagTextBlock.Text = value.SessionTag;
                    StartTimePicker.Value = new DateTime() + (value.SessionStartTime ?? new TimeSpan());

                    EndTimePicker.Value = new DateTime() + (value.SessionEndTime ?? new TimeSpan());

                    EquipmentComboBox.Text = value.Equipment;
                    MicrophoneComboBox.Text = value.Microphone;
                    LocationtextBox.Text = value.Location;
                    GPSLatitudeTextBox.Text = (value.LocationGPSLatitude ?? 0.0m).ToString();
                    GPSLongitudeTextBox.Text = (value.LocationGPSLongitude ?? 0.0m).ToString();
                    OperatorTextBox.Text = value.Operator;
                    SessionNotesRichtextBox.Text = value.SessionNotes;
                    SessionDatePicker.DisplayDate = value.SessionDate;
                    SessionDatePicker.SelectedDate = value.SessionDate;
                    TemperatureIntegerUpDown.Value = value.Temp;
                }
                SetValue(recordingSessionProperty, value);
            }
        }

        #endregion recordingSession

        #region selectedFolder

        /// <summary>
        /// selectedFolder Dependency Property
        /// </summary>
        public static readonly DependencyProperty selectedFolderProperty =
            DependencyProperty.Register("selectedFolder", typeof(String), typeof(RecordingSessionControl),
                new FrameworkPropertyMetadata((String)""));

        /// <summary>
        /// Gets or sets the selectedFolder property.  This dependency property
        /// indicates ....
        /// </summary>
        public String selectedFolder
        {
            get { return (String)GetValue(selectedFolderProperty); }
            set { SetValue(selectedFolderProperty, value); }
        }

        #endregion selectedFolder

       

        private List<String> equipmentList = new List<string>();
        private List<String> microphoneList = new List<string>();

        public RecordingSessionControl()
        {
            selectedFolder = "";
            equipmentList = DBAccess.GetEquipmentList();
            microphoneList = DBAccess.GetMicrophoneList();

            InitializeComponent();
            this.DataContext = this;
            MicrophoneComboBox.ItemsSource = microphoneList;
            EquipmentComboBox.ItemsSource = equipmentList;
        }

        /// <summary>
        /// Verifies the form contents.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public bool VerifyFormContents()
        {
            bool result = true;

            if (String.IsNullOrWhiteSpace(recordingSession.SessionTag))
            {
                result = false;
            }
            DateTime? date = recordingSession.SessionDate;
            if (date == null)
            {
                result = false;
            }
            else
            {
                if (date.Value.Year < 1990)
                {
                    result = false;
                }
                if (date.Value > DateTime.Now)
                {
                    result = false;
                }
            }
            if (String.IsNullOrWhiteSpace(recordingSession.Location))
            {
                result = false;
            }

            return (result);
        }
    }
}