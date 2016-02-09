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
    ///     Interaction logic for LabelledSegmentForm.xaml
    /// </summary>
    public partial class LabelledSegmentForm : Window
    {
        #region labelledSegment

        /// <summary>
        ///     labelledSegment Dependency Property
        /// </summary>
        public static readonly DependencyProperty labelledSegmentProperty =
            DependencyProperty.Register("labelledSegment", typeof(LabelledSegment), typeof(LabelledSegmentForm),
                new FrameworkPropertyMetadata((LabelledSegment)new LabelledSegment()));

        /// <summary>
        ///     Gets or sets the labelledSegment property. This dependency property indicates ....
        /// </summary>
        public LabelledSegment labelledSegment
        {
            get
            {
                LabelledSegment result = (LabelledSegment)GetValue(labelledSegmentProperty);
                result.StartOffset = Tools.ConvertDoubleToTimeSpan(StartOffsetDoubleUpDown.Value);
                result.EndOffset = Tools.ConvertDoubleToTimeSpan(EndOffsetDoubleUpDown.Value);
                result.Comment = CommentTextBox.Text;
                return (result);
            }
            set
            {
                SetValue(labelledSegmentProperty, value);
                StartOffsetDoubleUpDown.Value = value.StartOffset.TotalSeconds;
                EndOffsetDoubleUpDown.Value = value.EndOffset.TotalSeconds;
                CommentTextBox.Text = value.Comment;
            }
        }

        #endregion labelledSegment

        public LabelledSegmentForm()
        {
            InitializeComponent();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }
    }
}