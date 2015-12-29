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
using System.Windows.Shapes;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for RecordingForm.xaml
    /// </summary>
    public partial class RecordingForm : Window
    {
        #region recording

        /// <summary>
        /// recording Dependency Property
        /// </summary>
        public static readonly DependencyProperty recordingProperty =
            DependencyProperty.Register("recording", typeof(Recording), typeof(RecordingForm),
                new FrameworkPropertyMetadata((Recording)new Recording()));

        /// <summary>
        /// Gets or sets the recording property.  This dependency property 
        /// indicates ....
        /// </summary>
        public Recording recording
        {
            get { return (Recording)GetValue(recordingProperty); }
            set {
                SetValue(recordingProperty, value);
                LabelledSegmentsStackPanel.Children.Clear();
                if(value.LabelledSegments!=null && value.LabelledSegments.Count() > 0)
                {
                    foreach(var segment in value.LabelledSegments)
                    {
                        LabelledSegmentControl lsc = new LabelledSegmentControl();
                        lsc.labelledSegment = segment;
                        LabelledSegmentsStackPanel.Children.Add(lsc);
                    }
                }
            }
        }

        #endregion



        public RecordingForm()
        {
            
            InitializeComponent();
            this.DataContext = recording;
            
        }
    }
}
