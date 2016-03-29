using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

            SortedBatList = DBAccess.GetSortedBatList();
        }

        internal void RefreshData()
        {
            SortedBatList = DBAccess.GetSortedBatList();
            /*          var view = CollectionViewSource.GetDefaultView(sortedBatListView.ItemsSource);
                      if (view != null) view.Refresh();*/
        }

        private void AddBatButton_Click(object sender, RoutedEventArgs e)
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

            int index = BatsDataGrid.SelectedIndex;
            SortedBatList = DBAccess.GetSortedBatList();
            BatsDataGrid.SelectedIndex = index;
            bdc.BatTagsListView.SelectedIndex = tagIndex;
        }

        private void BatListGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void BatsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid bsdg = sender as DataGrid;
            if (e.AddedItems == null || e.AddedItems.Count <= 0) return;
            Bat selected = e.AddedItems[0] as Bat;
            if (e.RemovedItems != null && e.RemovedItems.Count > 0)
            {
                Bat previous = e.RemovedItems[0] as Bat;
                if (previous == selected) return;
            }
            // therefore we have a selected item which is different from the previously selected item
            batDetailControl.selectedBat = selected;
        }

        private void DelBatButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatsDataGrid.SelectedItem != null)
            {
                Bat selectedBat = BatsDataGrid.SelectedItem as Bat;
                int index = BatsDataGrid.SelectedIndex;
                DBAccess.DeleteBat(selectedBat);
                SortedBatList = DBAccess.GetSortedBatList();
                BatsDataGrid.SelectedIndex = index < SortedBatList.Count() ? index : SortedBatList.Count() - 1;
            }
        }

        private void EditBatButton_Click(object sender, RoutedEventArgs e)
        {
            EditBatForm batEditingForm = new EditBatForm();
            if (BatsDataGrid.SelectedItem == null) return;
            batEditingForm.newBat = BatsDataGrid.SelectedItem as Bat;
            batEditingForm.ShowDialog();
            if (batEditingForm.DialogResult ?? false)
            {
                int index = BatsDataGrid.SelectedIndex;
                DBAccess.MergeBat(batEditingForm.newBat);
                SortedBatList = DBAccess.GetSortedBatList();
                if (index > 0 && index < SortedBatList.Count)
                {
                    BatsDataGrid.SelectedIndex = index;
                }
            }
        }
    }
}