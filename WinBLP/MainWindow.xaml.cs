using System;
using System.IO;
using System.Windows;

using System.Windows.Input;

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
        //private bool isSaved = true;

        /// <summary>
        ///     The window title
        /// </summary>
        private String windowTitle = "Bat Log Manager - v";

        /// <summary>
        ///     Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            System.Windows.Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            InitializeComponent();

            Build = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //windowTitle = "Bat Log File Processor " + Build;
            this.Title = windowTitle + " " + Build;

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
            about.version.Content = "v " + Build;
            about.ShowDialog();
        }

        /// <summary>
        ///     Handles the Click event of the miBatReference control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miBatReference_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
            BatRecordingListDetailControl.Visibility = Visibility.Hidden;
            recordingSessionListControl.Visibility = Visibility.Hidden;
            importControl.Visibility = Visibility.Hidden;
            batListControl.Visibility = Visibility.Visible;

            this.InvalidateArrange();
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        ///     Handles the Click event of the miBatSearch control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miBatSearch_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            batListControl.Visibility = Visibility.Hidden;
            importControl.Visibility = Visibility.Hidden;
            recordingSessionListControl.Visibility = Visibility.Hidden;
            BatRecordingListDetailControl.Visibility = Visibility.Visible;
            BatRecordingListDetailControl.BatStatisticsList = DBAccess.GetBatStatistics();
            BatRecordingListDetailControl.RefreshData();

            this.InvalidateArrange();
            this.UpdateLayout();
            Mouse.OverrideCursor = null;
        }

        private void miCreateDatabase_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.InitialDirectory = DBAccess.GetWorkingDatabaseLocation();
            dialog.FileName = "_BatReferenceDB.mdf";
            dialog.DefaultExt = ".mdf";
            var result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                int index = 1;
                while (File.Exists(dialog.FileName))
                {
                    dialog.FileName = dialog.FileName.Substring(0, dialog.FileName.Length - 4) + index.ToString() + ".mdf";
                }
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    string err = DBAccess.CreateDatabase(dialog.FileName);
                    if (!String.IsNullOrWhiteSpace(err))
                    {
                        MessageBox.Show(err, "Unable to create database");
                    }
                    else
                    {
                        err = DBAccess.SetDatabase(dialog.FileName);
                        if (!String.IsNullOrWhiteSpace(err))
                        {
                            MessageBox.Show(err, "Unable to set new DataContext for selected Database");
                        }

                        recordingSessionListControl.RefreshData();
                        BatRecordingListDetailControl.RefreshData();
                        batListControl.RefreshData();
                        miRecordingSearch_Click(sender, e);
                    }
                }
                catch (Exception) { }
                finally
                {
                    Mouse.OverrideCursor = null;
                }
            }
        }

        /// <summary>
        ///     Handles the Click event of the miDatabase control. Allows the user to select an
        ///     alternative .mdf database file with the name BatReferenceDB but in an alternative
        ///     location. Selection of a different filename will be rejected in case the database
        ///     structure is different. The location of the selected file will be stired in the
        ///     global static App.dbFileLocation variable whence it can be referenced by the
        ///     DBAccess static functions.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miDatabase_Click(object sender, RoutedEventArgs e)
        {
            string WorkingFolder = DBAccess.GetWorkingDatabaseLocation();
            if (String.IsNullOrWhiteSpace(WorkingFolder) || !Directory.Exists(WorkingFolder))
            {
                App.dbFileLocation = "";
                WorkingFolder = DBAccess.GetWorkingDatabaseLocation();
            }
            using (System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog())
            {
                if (!String.IsNullOrWhiteSpace(WorkingFolder))
                {
                    dialog.InitialDirectory = WorkingFolder;
                }
                else
                {
                    WorkingFolder = Directory.GetCurrentDirectory();
                    dialog.InitialDirectory = WorkingFolder;
                }
                dialog.Filter = "mdf files|*.mdf";

                dialog.Multiselect = false;
                dialog.Title = "Select An Alternative BatReferenceDB.mdf database file";
                dialog.DefaultExt = ".mdf";

                dialog.FileName = "*.mdf";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (!dialog.FileName.EndsWith("BatReferenceDB.mdf"))
                    {
                        System.Windows.MessageBox.Show(@"Only database file names ending with BatReferenceDB.mdf will have the correct structure.
                            Please reselect");
                    }
                    else
                    {
                        DBAccess.SetDatabase(dialog.FileName);
                        recordingSessionListControl.RefreshData();
                        BatRecordingListDetailControl.RefreshData();
                        miRecordingSearch_Click(sender, e);
                    }
                }
            }
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
            string helpfile = @"Bat Recording Manager.chm";
            if (File.Exists(helpfile))
            {
                System.Windows.Forms.Help.ShowHelp(null, helpfile);
            }
            /*
            HelpScreen help = new HelpScreen();
            help.ShowDialog();*/
        }

        /// <summary>
        ///     Handles the Click event of the miNewLogFile control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miNewLogFile_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            BatRecordingListDetailControl.Visibility = Visibility.Hidden;
            recordingSessionListControl.Visibility = Visibility.Hidden;
            batListControl.Visibility = Visibility.Hidden;
            importControl.Visibility = Visibility.Visible;
            this.InvalidateArrange();
            this.UpdateLayout();
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        ///     Handles the Click event of the miRecordingSearch control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="RoutedEventArgs"/> instance containing the event data.
        /// </param>
        private void miRecordingSearch_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            BatRecordingListDetailControl.Visibility = Visibility.Hidden;
            batListControl.Visibility = Visibility.Hidden;
            importControl.Visibility = Visibility.Hidden;
            recordingSessionListControl.Visibility = Visibility.Visible;
            recordingSessionListControl.RefreshData();
            this.InvalidateArrange();
            this.UpdateLayout();
            Mouse.OverrideCursor = null;
        }

        private void miSetToDefaultDatabase_Click(object sender, RoutedEventArgs e)
        {
            DBAccess.SetDatabase(null);
            recordingSessionListControl.RefreshData();
            BatRecordingListDetailControl.RefreshData();
            miRecordingSearch_Click(sender, e);
        }

        /// <summary>
        ///     Handles the SessionAction event of the SessionsAndRecordings control.
        /// </summary>
        /// <param name="sender">
        ///     The source of the event.
        /// </param>
        /// <param name="e">
        ///     The <see cref="SessionActionEventArgs"/> instance containing the event data.
        /// </param>
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
            DBAccess.CloseDatabase();
            /*
            if (!isSaved)
            {
                isSaved = importControl.SaveOutputFile();
            }*/
        }
    }
}