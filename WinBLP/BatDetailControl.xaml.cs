using System;
using System.Collections.ObjectModel;
using System.Data.Linq;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for BatDetailControl.xaml
    /// </summary>
    public partial class BatDetailControl : UserControl
    {
        #region selectedBat

        /// <summary>
        ///     selectedBat Dependency Property
        /// </summary>
        public static readonly DependencyProperty selectedBatProperty =
            DependencyProperty.Register("selectedBat", typeof(Bat), typeof(BatDetailControl),
                new FrameworkPropertyMetadata((Bat)new Bat()));

        /// <summary>
        ///     Gets or sets the selectedBat property. This dependency property indicates ....
        /// </summary>
        public Bat selectedBat
        {
            get
            {
                return (Bat)GetValue(selectedBatProperty);
            }
            set
            {
                SetValue(selectedBatProperty, value);

                if (value != null)
                {
                    CommonNameTextBlock.Text = value.Name;
                    LatinNameTextBlock.Text = value.Batgenus + " " + value.BatSpecies;
                    BatTagsListView.ItemsSource = value.BatTags;
                    batNotesTextBox.Text = value.Notes;
                    if (value.BatCalls != null && value.BatCalls.Count > 0)
                    {
                        batCallControl.CallList = new ObservableCollection<Call>(DBAccess.GetCallsForBat(value));
                        selectedCallIndex = batCallControl.selectCall(0);
                        batCallControl.selectCall(selectedCallIndex);
                        BatCallLabel.Visibility = Visibility.Visible;
                        batCallControl.Visibility = Visibility.Visible;
                        //batCallControl.BatCall = value.BatCalls.First().Call;
                    }
                    else
                    {
                        selectedCallIndex = batCallControl.selectCall(-1);

                        batCallControl.CallList = null;
                        BatCallLabel.Visibility = Visibility.Hidden;
                        batCallControl.Visibility = Visibility.Hidden;
                    }
                }
                else
                {// value==null
                    CommonNameTextBlock.Text = "";
                    LatinNameTextBlock.Text = "";
                    BatTagsListView.ItemsSource = null;
                    batNotesTextBox.Text = "";
                    selectedCallIndex = -1;
                    batCallControl.CallList = null;
                    BatCallLabel.Visibility = Visibility.Hidden;
                    batCallControl.Visibility = Visibility.Hidden;
                }
            }
        }

        #endregion selectedBat

        /// <summary>
        ///     The list changed event lock
        /// </summary>
        private readonly object ListChangedEventLock = new object();

        private int _selectedCallIndex;

        /// <summary>
        ///     The list changed event
        /// </summary>
        private EventHandler ListChangedEvent;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatDetailControl"/> class.
        /// </summary>
        public BatDetailControl()
        {
            InitializeComponent();
            this.DataContext = selectedBat;
            batCallControl.SetReadOnly(true);
        }

        /// <summary>
        ///     Event raised after the List property value has changed.
        /// </summary>
        public event EventHandler ListChanged
        {
            add
            {
                lock (ListChangedEventLock)
                {
                    ListChangedEvent += value;
                }
            }
            remove
            {
                lock (ListChangedEventLock)
                {
                    ListChangedEvent -= value;
                }
            }
        }

        public int selectedCallIndex
        {
            get
            {
                return (_selectedCallIndex);
            }
            set
            {
                _selectedCallIndex = value;
                if (batCallControl != null && batCallControl.CallList != null)
                {
                    CallCountTextBox.Text = batCallControl.CallList.Count.ToString();
                    CallIndexTextBox.Text = (value + 1).ToString();
                    if (value < 1)
                    {
                        PrevCallButton.IsEnabled = false;
                    }
                    else
                    {
                        PrevCallButton.IsEnabled = true;
                    }
                    if (value >= batCallControl.CallList.Count - 1)
                    {
                        NextCallButton.IsEnabled = false;
                    }
                    else
                    {
                        NextCallButton.IsEnabled = true;
                    }
                }
                else
                {
                    CallCountTextBox.Text = "0";
                    CallIndexTextBox.Text = "-";
                    PrevCallButton.IsEnabled = false;
                    NextCallButton.IsEnabled = false;
                }
            }
        }

        /// <summary>
        ///     Raises the <see cref="ListChanged"/> event.
        /// </summary>
        /// <param name="e">
        ///     <see cref="EventArgs"/> object that provides the arguments for the event.
        /// </param>
        protected virtual void OnListChanged(EventArgs e)
        {
            EventHandler handler = null;

            lock (ListChangedEventLock)
            {
                handler = ListChangedEvent;

                if (handler == null)
                    return;
            }

            handler(this, e);
        }

        private void AddTagButton_Click(object sender, RoutedEventArgs e)
        {
            Bat thisBat = (Bat)selectedBat;
            if (thisBat == null) return;
            int sortIndex = BatTagsListView.SelectedIndex;
            NewTagForm newTagForm = new NewTagForm();
            newTagForm.ShowDialog();
            if (newTagForm.DialogResult != null && newTagForm.DialogResult.Value)
            {
                sortIndex = DBAccess.AddTag(newTagForm.TagText, thisBat.Id);
            }
            OnListChanged(new EventArgs());
            BatTagsListView.SelectedIndex = sortIndex;
        }

        private void DelTagButton_Click(object sender, RoutedEventArgs e)
        {
            BatTag tag = BatTagsListView.SelectedItem as BatTag;
            DBAccess.DeleteTag(tag);
            OnListChanged(new EventArgs());
        }

        private void EditTagButton_Click(object sender, RoutedEventArgs e)
        {
            BatTag tag = BatTagsListView.SelectedItem as BatTag;
            if (tag != null)
            {
                Bat thisBat = (Bat)(this.DataContext as DataGrid).SelectedItem;
                if (thisBat == null) return;
                int sortIndex = BatTagsListView.SelectedIndex;
                NewTagForm newTagForm = new NewTagForm();
                newTagForm.TagText = tag.BatTag1;
                newTagForm.ShowDialog();
                if (newTagForm.DialogResult != null && newTagForm.DialogResult.Value)
                {
                    tag.BatTag1 = newTagForm.TagText;
                    sortIndex = DBAccess.UpdateTag(tag);
                    //sortIndex = DBAccess.AddTag(newTagForm.TagText, thisBat.Id);
                }
                OnListChanged(new EventArgs());
                if (sortIndex >= 0 && sortIndex < BatTagsListView.Items.Count)
                {
                    BatTagsListView.SelectedIndex = sortIndex;
                }
                else
                {
                    BatTagsListView.SelectedIndex = 0;
                }
            }
        }

        private void NextCallButton_Click(object sender, RoutedEventArgs e)
        {
            selectedCallIndex = batCallControl.selectCall(selectedCallIndex + 1);
        }

        private void PrevCallButton_Click(object sender, RoutedEventArgs e)
        {
            selectedCallIndex = batCallControl.selectCall(selectedCallIndex - 1);
        }
    }

    #region BatLatinNameConverter (ValueConverter)

    /// <summary>
    /// </summary>
    public class BatLatinNameConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                Bat bat = value as Bat;
                String latinName = "";
                if (bat != null)
                {
                    latinName = bat.Batgenus + " " + bat.BatSpecies;
                }
                return latinName;
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion BatLatinNameConverter (ValueConverter)

    #region BatTagSortConverter (ValueConverter)

    /// <summary>
    /// </summary>
    public class BatTagSortConverter : IValueConverter
    {
        /// <summary>
        ///     Converts the specified value.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.

                EntitySet<BatTag> tagList = value as EntitySet<BatTag>;
                var sortedTagList = from tag in tagList
                                    orderby tag.SortIndex
                                    select tag;
                return (sortedTagList);
            }
            catch
            {
                return value;
            }
        }

        /// <summary>
        ///     Converts the back.
        /// </summary>
        /// <param name="value">
        ///     The value.
        /// </param>
        /// <param name="targetType">
        ///     Type of the target.
        /// </param>
        /// <param name="parameter">
        ///     The parameter.
        /// </param>
        /// <param name="culture">
        ///     The culture.
        /// </param>
        /// <returns>
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion BatTagSortConverter (ValueConverter)
}