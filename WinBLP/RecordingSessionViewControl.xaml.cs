using Microsoft.Maps.MapControl.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for RecordingSessionViewControl.xaml
    /// </summary>
    public partial class RecordingSessionViewControl : UserControl
    {
        #region SelectedSession

        /// <summary>
        ///     recordingSession Dependency Property
        /// </summary>
        public static readonly DependencyProperty SelectedSessionProperty =
            DependencyProperty.Register("recordingSession", typeof(RecordingSession), typeof(RecordingSessionViewControl),
                new FrameworkPropertyMetadata((RecordingSession)new RecordingSession()));

        /// <summary>
        ///     Gets or sets the recordingSession property. This dependency property indicates ....
        /// </summary>
        public RecordingSession recordingSession
        {
            get
            {
                return (RecordingSession)GetValue(SelectedSessionProperty);
            }
            set
            {
                SetValue(SelectedSessionProperty, value);
                if (value != null)
                {
                    SessionTagTextBlock.Text = value.SessionTag ?? "";
                    SessionDatePicker.Text = value.SessionDate.ToShortDateString();
                    StartTimePicker.Text = (value.SessionStartTime ?? new TimeSpan()).ToString();
                    EndTimePicker.Text = (value.SessionEndTime ?? new TimeSpan()).ToString();
                    TemperatureIntegerUpDown.Text = value.Temp <= 0 ? "" : value.Temp.ToString() + @"°C";
                    EquipmentComboBox.Text = value.Equipment ?? "";
                    MicrophoneComboBox.Text = value.Microphone ?? "";
                    OperatorComboBox.Text = value.Operator ?? "";
                    LocationComboBox.Text = value.Location ?? "";
                    if (value.LocationGPSLatitude == null || value.LocationGPSLatitude.Value < -90.0m || value.LocationGPSLatitude.Value > 90.0m)
                    {
                        GPSLatitudeTextBox.Text = "";
                    }
                    else
                    {
                        GPSLatitudeTextBox.Text = value.LocationGPSLatitude.Value.ToString();
                    }
                    if (value.LocationGPSLongitude == null || value.LocationGPSLongitude.Value < -180.0m || value.LocationGPSLongitude.Value > 180.0m)
                    {
                        GPSLongitudeTextBox.Text = "";
                    }
                    else
                    {
                        GPSLongitudeTextBox.Text = value.LocationGPSLongitude.Value.ToString();
                    }
                    SessionNotesRichtextBox.Text = value.SessionNotes ?? "";
                }
                else
                {
                    SessionTagTextBlock.Text = "";
                    SessionDatePicker.Text = "";
                    StartTimePicker.Text = "";
                    EndTimePicker.Text = "";
                    TemperatureIntegerUpDown.Text = "";
                    EquipmentComboBox.Text = "";
                    MicrophoneComboBox.Text = "";
                    OperatorComboBox.Text = "";
                    LocationComboBox.Text = "";
                    GPSLatitudeTextBox.Text = "";
                    GPSLongitudeTextBox.Text = "";
                    SessionNotesRichtextBox.Text = "";
                }
            }
        }

        #endregion SelectedSession

        public RecordingSessionViewControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void GPSLatitudeTextBox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            GPSMapButton_Click(sender, new RoutedEventArgs());
        }

        private void GPSMapButton_Click(object sender, RoutedEventArgs e)
        {
            Location coordinates;
            double lat = 200.0d;
            double longit = 200.0d;
            if (!double.TryParse(GPSLatitudeTextBox.Text, out lat)) return;
            if (!double.TryParse(GPSLongitudeTextBox.Text, out longit)) return;
            if (Math.Abs(lat) > 90.0d || Math.Abs(longit) > 180.0d) return;
            coordinates = new Location(lat, longit);

            MapWindow mapWindow = new MapWindow(false);
            mapWindow.mapControl.coordinates = coordinates;
            mapWindow.Show();
            if (recordingSession != null && recordingSession.Recordings != null && recordingSession.Recordings.Count > 0)
            {
                int i = 0;
                foreach (var rec in recordingSession.Recordings)
                {
                    i++;
                    double latitude = 200;
                    double longitude = 200;
                    if (Double.TryParse(rec.RecordingGPSLatitude, out latitude))
                    {
                        if (Double.TryParse(rec.RecordingGPSLongitude, out longitude))
                        {
                            if (Math.Abs(latitude) <= 90.0d && Math.Abs(longitude) <= 180.0d)
                            {
                                mapWindow.mapControl.AddPushPin(new Location(latitude, longitude), i.ToString());
                            }
                        }
                    }
                }
            }
        }
    }
}