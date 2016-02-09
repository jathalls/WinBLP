using System;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        ///     The build
        /// </summary>
        private String Build;

        /// <summary>
        ///     The is saved
        /// </summary>
        private bool isSaved = false;

        /// <summary>
        ///     The window title
        /// </summary>
        private String windowTitle = "Bat Log Manager - v";

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            InitializeComponent();

            Build = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //windowTitle = "Bat Log File Processor " + Build;
            this.Title = windowTitle + Build;
            this.InvalidateArrange();

            BatRecordingListDetailControl.sessionsAndRecordings.SessionAction += SessionsAndRecordings_SessionAction;
            miRecordingSearch_Click(this, new RoutedEventArgs());
        }

        /// <summary>
        ///     Display the About box
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutScreen about = new AboutScreen();
            about.ShowDialog();
        }

        private void miBatReference_Click(object sender, RoutedEventArgs e)
        {
            BatRecordingListDetailControl.Visibility = Visibility.Hidden;
            recordingSessionListControl.Visibility = Visibility.Hidden;
            importControl.Visibility = Visibility.Hidden;
            batListControl.Visibility = Visibility.Visible;

            this.InvalidateArrange();
        }

        private void miBatSearch_Click(object sender, RoutedEventArgs e)
        {
            batListControl.Visibility = Visibility.Hidden;
            importControl.Visibility = Visibility.Hidden;
            recordingSessionListControl.Visibility = Visibility.Hidden;
            BatRecordingListDetailControl.Visibility = Visibility.Visible;
            BatRecordingListDetailControl.BatStatisticsList = DBAccess.GetBatStatistics();
            BatRecordingListDetailControl.RefreshData();

            this.InvalidateArrange();
            this.UpdateLayout();
        }

        /// <summary>
        ///     Handles the Click event of the miEditBatList control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miEditBatList_Click(object sender, RoutedEventArgs e)
        {
            /*
            if(batSummary!= null)
            {
                BatEditor batEditor = new BatEditor(batSummary.batReferenceDataContext);
                var result=batEditor.ShowDialog();
                if(result!=null && result.Value)
                {
                    //batSummary.RefreshBatList();
                }
            }*/
        }

        /// <summary>
        ///     Quits the program
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();

            //App.Current.Shutdown();
            //Environment.Exit(0);
        }

        /// <summary>
        ///     Handles the Click event of the miHelp control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpScreen help = new HelpScreen();
            help.ShowDialog();
        }

        private void miNewLogFile_Click(object sender, RoutedEventArgs e)
        {
            BatRecordingListDetailControl.Visibility = Visibility.Hidden;
            recordingSessionListControl.Visibility = Visibility.Hidden;
            batListControl.Visibility = Visibility.Hidden;
            importControl.Visibility = Visibility.Visible;
            this.InvalidateArrange();
            this.UpdateLayout();
        }

        /// <summary>
        ///     Responds to selection File/OpenFiles Produces a file selection dialog and reads all
        ///     the selected files. Uses the FileBrowser class to select and store the selected file names.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(importControl.fileBrowser.SelectLogFiles()))
            {
                miProcess.IsEnabled = true;
                miSortOrder.IsEnabled = true;
                OutputLocationLabel.Content = importControl.ReadSelectedFiles();
                if (String.IsNullOrWhiteSpace(OutputLocationLabel.Content as String))
                {
                    miSortOrder.IsEnabled = false;
                    miProcess.IsEnabled = false;
                }
            }
        }

        /// <summary>
        ///     Allows the user to select all the text files within a single folder
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            importControl.IsEnabled = true;
            importControl.Visibility = Visibility.Visible;
            importControl.OpenFolder();
            miSortOrder.IsEnabled = true;
            miProcess.IsEnabled = true;
        }

        /// <summary>
        ///     Handles the Click event of the miProcess control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miProcess_Click(object sender, RoutedEventArgs e)
        {
            //write the text to the output file
            try
            {
                if (importControl.fileBrowser.TextFileNames.Count > 0)
                {
                    importControl.ProcessFiles();
                    miSave.IsEnabled = true;
                    isSaved = importControl.SaveOutputFile();
                }
            }
            catch (Exception) { }
        }

        private void miRecordingSearch_Click(object sender, RoutedEventArgs e)
        {
            BatRecordingListDetailControl.Visibility = Visibility.Hidden;
            batListControl.Visibility = Visibility.Hidden;
            importControl.Visibility = Visibility.Hidden;
            recordingSessionListControl.Visibility = Visibility.Visible;
            recordingSessionListControl.RefreshData();
            this.InvalidateArrange();
            this.UpdateLayout();
        }

        /// <summary>
        ///     Handles the Click event of the miSave control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miSave_Click(object sender, RoutedEventArgs e)
        {
            if (!isSaved)
            {
                isSaved = importControl.SaveOutputFile();
            }
        }

        /// <summary>
        ///     Allows the user to change the order in which the selected files will be processed
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miSortOrder_Click(object sender, RoutedEventArgs e)
        {
            importControl.SortFileOrder();
        }

        private void SessionsAndRecordings_SessionAction(object sender, SessionActionEventArgs e)
        {
            miRecordingSearch_Click(this, new RoutedEventArgs());
            recordingSessionListControl.Select(e.recordingSession);
        }

        /// <summary>
        ///     Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the
        ///     event data.
        /// </param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!isSaved)
            {
                isSaved = importControl.SaveOutputFile();
            }
        }
    }
}