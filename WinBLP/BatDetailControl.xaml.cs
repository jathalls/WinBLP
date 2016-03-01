using System;
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
        /// <summary>
        ///     The list changed event lock
        /// </summary>
        private readonly object ListChangedEventLock = new object();

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
            BatTagButtonBar.AddButton.Click += AddButton_Click;
            BatTagButtonBar.DeleteButton.Click += DeleteButton_Click;
            BatTagButtonBar.MoveDownButton.Click += MoveDownButton_Click;
            BatTagButtonBar.MoveUpButton.Click += MoveUpButton_Click;
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

        /// <summary>
        ///     Handles the Click event of the AddButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Bat thisBat = (Bat)(this.DataContext as ListView).SelectedItem;
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

        /// <summary>
        ///     Handles the Click event of the DeleteButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            BatTag tag = BatTagsListView.SelectedItem as BatTag;
            DBAccess.DeleteTag(tag);
            OnListChanged(new EventArgs());
        }

        /// <summary>
        ///     Handles the Click event of the MoveDownButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatTagsListView.SelectedItem != null && BatTagsListView.SelectedIndex < BatTagsListView.Items.Count - 1)
            {
                BatTag tag = BatTagsListView.SelectedItem as BatTag;
                DBAccess.MoveTagDown(tag);
                int newIndex = BatTagsListView.SelectedIndex++;

                OnListChanged(new EventArgs());
                BatTagsListView.SelectedIndex = newIndex;
            }
        }

        /// <summary>
        ///     Handles the Click event of the MoveUpButton control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatTagsListView.SelectedItem != null)
            {
                BatTag tag = BatTagsListView.SelectedItem as BatTag;
                DBAccess.MoveTagUp(tag);
                int newIndex = BatTagsListView.SelectedIndex--;

                OnListChanged(new EventArgs());
                BatTagsListView.SelectedIndex = newIndex;
            }
        }
    }

    #region BatLatinNameConverter (ValueConverter)

    /// <summary>
    ///     </summary>
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
        ///     </returns>
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
        ///     </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion BatLatinNameConverter (ValueConverter)

    #region BatTagSortConverter (ValueConverter)

    /// <summary>
    ///     </summary>
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
        ///     </returns>
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
        ///     </returns>
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion BatTagSortConverter (ValueConverter)
}