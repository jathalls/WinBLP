using System;
using System.Collections.Generic;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for FileOrderDialog.xaml
    /// </summary>
    public partial class FileOrderDialog : Window
    {
        //private List<String> fileList;

        #region fileList

        /// <summary>
        /// fileList Dependency Property
        /// </summary>
        public static readonly DependencyProperty fileListProperty =
            DependencyProperty.Register("fileList", typeof(List<String>), typeof(FileOrderDialog),
                new FrameworkPropertyMetadata((new List<String>())));

        /// <summary>
        /// Gets or sets the fileList property.  This dependency property
        /// indicates ....
        /// </summary>
        public List<String> fileList
        {
            get { return (List<String>)GetValue(fileListProperty); }
            set { SetValue(fileListProperty, value); }
        }

        #endregion fileList

        /// <summary>
        /// Default Constructor
        /// </summary>
        public FileOrderDialog()
        {
            InitializeComponent();
            DataContext = this;
            fileList = new List<string>();
            FileListBox.ItemsSource = new List<String>();
        }

        /// <summary>
        /// Populates the list box with the supplied list of strings
        /// </summary>
        /// <param name="list"></param>
        internal void Populate(List<string> list)
        {
            fileList = list;
        }

        /// <summary>
        /// Returns the list of strings displayed in the dialog
        /// </summary>
        /// <returns></returns>
        internal List<string> GetFileList()
        {
            return (fileList);
        }

        /// <summary>
        /// Responds to the OK button by closing the dialog and returning true
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        /// <summary>
        /// Causes the selected file name to be deleted from the file list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DELButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedIndex >= 0 && FileListBox.SelectedItem != null)
            {
                if (fileList.Contains((String)FileListBox.SelectedItem))
                {
                    fileList.Remove((String)FileListBox.SelectedItem);
                }
            }
            FileListBox.Items.Refresh();
        }

        /// <summary>
        /// Moves the selected item one place up in the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UPButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedIndex >= 0 && FileListBox.SelectedItem != null)
            {
                if (FileListBox.SelectedIndex > 0)
                {
                    String temp = fileList[FileListBox.SelectedIndex];
                    fileList[FileListBox.SelectedIndex] = fileList[FileListBox.SelectedIndex - 1];
                    fileList[FileListBox.SelectedIndex - 1] = temp;
                    FileListBox.Items.Refresh();
                }
            }
        }

        /// <summary>
        /// Moves the selected item down one place in the list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DOWNButton_Click(object sender, RoutedEventArgs e)
        {
            if (FileListBox.SelectedIndex >= 0 && FileListBox.SelectedItem != null)
            {
                if (FileListBox.SelectedIndex < FileListBox.Items.Count - 1)
                {
                    String temp = fileList[FileListBox.SelectedIndex];
                    fileList[FileListBox.SelectedIndex] = fileList[FileListBox.SelectedIndex + 1];
                    fileList[FileListBox.SelectedIndex + 1] = temp;
                    FileListBox.Items.Refresh();
                }
            }
        }

        /// <summary>
        /// Allows the user to select additional files to add to the existing
        /// file list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ADDButton_Click(object sender, RoutedEventArgs e)
        {
            FileBrowser additionalFileBrowser = new FileBrowser();
            additionalFileBrowser.SelectLogFiles();
            if (additionalFileBrowser.TextFileNames != null && additionalFileBrowser.TextFileNames.Count > 0)
            {
                // some additional names have been chosen
                foreach (var file in additionalFileBrowser.TextFileNames)
                {
                    if (!fileList.Contains(file))
                    {
                        fileList.Add(file);
                    }
                }
            }
        }
    }
}