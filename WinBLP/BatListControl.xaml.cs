using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for BatListControl.xaml
    /// </summary>
    public partial class BatListControl : UserControl
    {
        #region SortedBatList

        /// <summary>
        ///     SortedBatList Dependency Property
        /// </summary>
        public static readonly DependencyProperty SortedBatListProperty =
            DependencyProperty.Register("SortedBatList", typeof(ObservableCollection<Bat>), typeof(BatListControl),
                new FrameworkPropertyMetadata((ObservableCollection<Bat>)new ObservableCollection<Bat>()));

        /// <summary>
        ///     Gets or sets the SortedBatList property. This dependency property indicates ....
        /// </summary>
        public ObservableCollection<Bat> SortedBatList
        {
            get { return (ObservableCollection<Bat>)GetValue(SortedBatListProperty); }
            set { SetValue(SortedBatListProperty, value); }
        }

        #endregion SortedBatList

        //private BatSummary batSummary;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatListControl"/> class.
        /// </summary>
        public BatListControl()
        {
            InitializeComponent();
            this.DataContext = this;

            //batSummary = new BatSummary();

            batDetailControl.ListChanged += BatDetailControl_ListChanged;
            Button editButton = BatListButtonBar.AddCustomButton("EDIT", 1, "EditBatButton");
            if (editButton != null)
            {
                editButton.Click += EditButton_Click;
            }
            BatListButtonBar.AddButton.Click += AddButton_Click;
            BatListButtonBar.DeleteButton.Click += DeleteButton_Click;
            BatListButtonBar.MoveDownButton.Click += MoveDownButton_Click;
            BatListButtonBar.MoveUpButton.Click += MoveUpButton_Click;
            SortedBatList = DBAccess.GetSortedBatList();
        }

        internal void RefreshData()
        {
            SortedBatList = DBAccess.GetSortedBatList();
            var view = CollectionViewSource.GetDefaultView(sortedBatListView.ItemsSource);
            if (view != null) view.Refresh();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            EditBatForm batEditingForm = new EditBatForm();
            batEditingForm.newBat = new Bat();
            batEditingForm.ShowDialog();
            if (batEditingForm.DialogResult != null && batEditingForm.DialogResult.Value)
            {
                DBAccess.InsertBat(batEditingForm.newBat);
                SortedBatList = DBAccess.GetSortedBatList();
            }
        }

        private void BatDetailControl_ListChanged(object sender, EventArgs e)
        {
            BatDetailControl bdc = sender as BatDetailControl;

            int tagIndex = bdc.BatTagsListView.SelectedIndex;

            int index = sortedBatListView.SelectedIndex;
            SortedBatList = DBAccess.GetSortedBatList();
            sortedBatListView.SelectedIndex = index;
            bdc.BatTagsListView.SelectedIndex = tagIndex;
        }

        private void BatListGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (sortedBatListView.SelectedItem != null)
            {
                Bat selectedBat = sortedBatListView.SelectedItem as Bat;
                int index = sortedBatListView.SelectedIndex;
                DBAccess.DeleteBat(selectedBat);
                SortedBatList = DBAccess.GetSortedBatList();
                sortedBatListView.SelectedIndex = index < SortedBatList.Count() ? index : SortedBatList.Count() - 1;
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            EditBatForm batEditingForm = new EditBatForm();
            if (sortedBatListView.SelectedItem == null) return;
            batEditingForm.newBat = sortedBatListView.SelectedItem as Bat;
            batEditingForm.ShowDialog();
            if (batEditingForm.DialogResult != null && batEditingForm.DialogResult.Value)
            {
                DBAccess.MergeBat(batEditingForm.newBat);
                SortedBatList = DBAccess.GetSortedBatList();
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (sortedBatListView.SelectedItem != null)
            {
                int originalLocation = sortedBatListView.SelectedIndex;
                DBAccess.MoveBat(sortedBatListView.SelectedItem as Bat, 1);
                SortedBatList = DBAccess.GetSortedBatList();
                sortedBatListView.SelectedIndex = originalLocation < SortedBatList.Count - 1
                    ? sortedBatListView.SelectedIndex + 1 : SortedBatList.Count - 1;
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (sortedBatListView.SelectedItem != null)
            {
                int originalLocation = sortedBatListView.SelectedIndex;
                DBAccess.MoveBat(sortedBatListView.SelectedItem as Bat, -1);
                SortedBatList = DBAccess.GetSortedBatList();
                this.InvalidateArrange();
                this.UpdateLayout();
                sortedBatListView.SelectedIndex = originalLocation > 0 ? originalLocation - 1 : 0;
            }
        }
    }
}