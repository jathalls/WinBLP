using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
    ///     Interaction logic for FolderSelectionDialog.xaml
    /// </summary>
    public partial class FolderSelectionDialog : Window
    {
        #region FolderList

        /// <summary>
        ///     FolderList Dependency Property
        /// </summary>
        public static readonly DependencyProperty FolderListProperty =
            DependencyProperty.Register("FolderList", typeof(ObservableCollection<String>), typeof(FolderSelectionDialog),
                new FrameworkPropertyMetadata((ObservableCollection<String>)new ObservableCollection<String>()));

        /// <summary>
        ///     Gets or sets the FolderList property. This dependency property indicates ....
        /// </summary>
        public ObservableCollection<String> FolderList
        {
            get { return (ObservableCollection<String>)GetValue(FolderListProperty); }
            set { SetValue(FolderListProperty, value); }
        }

        #endregion FolderList

        public FolderSelectionDialog()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            FileBrowser browser = new FileBrowser();
            browser.SelectFolder();
            if (browser.WorkingFolder != null && !String.IsNullOrWhiteSpace(browser.WorkingFolder))
            {
                if (Directory.Exists(browser.WorkingFolder))
                {
                    FolderList.Add(browser.WorkingFolder);
                }
            }
        }

        private void AddFolderTreeButton_Click(object sender, RoutedEventArgs e)
        {
            FileBrowser browser = new FileBrowser();
            browser.SelectRootFolder();
            if (browser.wavFileFolders != null && browser.wavFileFolders.Count > 0)
            {
                var combinedList = ((FolderList.Concat(browser.wavFileFolders)).Distinct());
                FolderList = (ObservableCollection<String>)combinedList;
            }
        }

        private void ButtonDeleteFolder_Click(object sender, RoutedEventArgs e)
        {
            Button thisButton = sender as Button;
            String ItemToDelete = ((thisButton.Parent as Grid).Children[1] as TextBox).Text;
            FolderList.Remove(ItemToDelete);

            ICollectionView view = CollectionViewSource.GetDefaultView(FolderListView.ItemsSource);
            view.Refresh();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void OnListViewItemFocused(object sender, RoutedEventArgs e)
        {
            ListViewItem lvi = sender as ListViewItem;
            lvi.IsSelected = true;
        }

        private void OnTextBoxFocused(object sender, RoutedEventArgs e)
        {
            TextBox segmentTextBox = sender as TextBox;
            Button myDelButton = ((segmentTextBox.Parent as Grid).Children[0] as Button);
            myDelButton.Visibility = Visibility.Visible;
        }
    }
}