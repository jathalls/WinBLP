﻿using System;
using System.Data.Linq;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for BatDetailControl.xaml
    /// </summary>
    public partial class BatDetailControl : UserControl
    {
        public BatDetailControl()
        {
            InitializeComponent();
            BatTagButtonBar.AddButton.Click += AddButton_Click;
            BatTagButtonBar.DeleteButton.Click += DeleteButton_Click;
            BatTagButtonBar.MoveDownButton.Click += MoveDownButton_Click;
            BatTagButtonBar.MoveUpButton.Click += MoveUpButton_Click;
        }

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

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            BatTag tag = BatTagsListView.SelectedItem as BatTag;
            DBAccess.DeleteTag(tag);
            OnListChanged(new EventArgs());
        }

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

        private void BatTagsListView_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private readonly object ListChangedEventLock = new object();
        private EventHandler ListChangedEvent;

        /// <summary>
        /// Event raised after the <see cref="Text" /> property value has changed.
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
        /// Raises the <see cref="ListChanged" /> event.
        /// </summary>
        /// <param name="e"><see cref="EventArgs" /> object that provides the arguments for the event.</param>
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
    }

    #region BatLatinNameConverter (ValueConverter)

    public class BatLatinNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                // Here's where you put the code do handle the value conversion.
                Bat bat = value as Bat;
                String latinName = bat.Batgenus + " " + bat.BatSpecies;
                return latinName;
            }
            catch
            {
                return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion BatLatinNameConverter (ValueConverter)

    

    #region BatTagSortConverter (ValueConverter)

    public class BatTagSortConverter : IValueConverter
    {
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

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Not implemented
            return null;
        }
    }

    #endregion BatTagSortConverter (ValueConverter)
}