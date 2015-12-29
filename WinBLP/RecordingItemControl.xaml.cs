using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for RecordingItemControl.xaml
    /// </summary>
    public partial class RecordingItemControl : UserControl
    {
        #region recordingItem

        /// <summary>
        /// recordingItem Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingItemProperty =
            DependencyProperty.Register("recordingItem", typeof(Recording), typeof(RecordingItemControl),
                new FrameworkPropertyMetadata((Recording)new Recording()));

        /// <summary>
        /// Gets or sets the recordingItem property.  This dependency property
        /// indicates ....
        /// </summary>
        public Recording recordingItem
        {
            get { return (Recording)GetValue(recordingItemProperty); }
            set
            {
                SetValue(recordingItemProperty, value);
                summary = DBAccess.GetStatsForRecording(value.Id);
                LineOneLabel.Content = value.RecordingName + " " + (value.RecordingEndTime - value.RecordingStartTime).ToString();
                if (!String.IsNullOrWhiteSpace(value.RecordingGPSLatitude) && !String.IsNullOrWhiteSpace(value.RecordingGPSLongitude))
                {
                    LineTwoLabel.Content = value.RecordingGPSLatitude + ", " + value.RecordingGPSLongitude;
                }
                else
                {
                    LineTwoLabel.Content = value.RecordingStartTime.ToString() + " - " + value.RecordingEndTime.ToString();
                }
                if (!String.IsNullOrWhiteSpace(value.RecordingNotes))
                {
                    LineThreeLabel.Content = value.RecordingNotes;
                }
                else
                {
                    LineThreeLabel.Content = "";
                }

                BatPassSummaryStackPanel.Children.Clear();
                if (summary != null && summary.Count > 0)
                {
                    foreach (var batType in summary)
                    {
                        BatPassSummaryControl batPassControl = new BatPassSummaryControl();
                        batPassControl.PassSummary = batType;
                        BatPassSummaryStackPanel.Children.Add(batPassControl);
                    }
                }

                LabelledSegmentListView.Items.Clear();
                foreach (var segment in value.LabelledSegments)
                {
                    LabelledSegmentControl labelledSegmentControl = new LabelledSegmentControl();
                    labelledSegmentControl.labelledSegment = segment;
                    LabelledSegmentListView.Items.Add(labelledSegmentControl);
                }
                InvalidateArrange();
                UpdateLayout();
            }
        }

        #endregion recordingItem

        private List<BatStats> summary;

        public RecordingItemControl()
        {
            InitializeComponent();
            this.DataContext = recordingItem;
        }

        internal void DeleteRecording()
        {
            if(recordingItem!= null)
            {
                String err=DBAccess.DeleteRecording(recordingItem);
                if (!String.IsNullOrWhiteSpace(err))
                {
                    MessageBox.Show(err, "Delete Recording Failed");
                }
            }
        }
    }
}