using System;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for RecordingSessionForm.xaml
    /// </summary>
    public partial class RecordingSessionForm : Window
    {
        public RecordingSessionForm()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (recordingSessionControl.VerifyFormContents())
            {
                DialogResult = true;
                this.Close();
            }
            else
            {
                DialogResult = false;
                MessageBox.Show("Recording Session Validation failed");
            }
        }

        public void SetRecordingSession(RecordingSession session)
        {
            recordingSessionControl.recordingSession = session;
        }

        public RecordingSession GetRecordingSession()
        {
            return (recordingSessionControl.recordingSession);
        }

        internal void Clear()
        {
            recordingSessionControl.recordingSession = new RecordingSession();
        }
    }
}