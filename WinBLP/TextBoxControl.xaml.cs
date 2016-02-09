using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for ImportControl.xaml
    /// </summary>
    public partial class ImportControl : UserControl
    {
        /// <summary>
        ///     The current session identifier
        /// </summary>
        public int CurrentSessionId = -1;

        /// <summary>
        ///     The current session tag
        /// </summary>
        public string CurrentSessionTag = "";

        /// <summary>
        ///     The bat summary
        /// </summary>
        private BatSummary batSummary;

        /// <summary>
        ///     The file processor
        /// </summary>
        private FileProcessor fileProcessor;

        /// <summary>
        ///     The GPX handler
        /// </summary>
        private GpxHandler gpxHandler;

        /// <summary>
        ///     The session for folder
        /// </summary>
        private RecordingSession sessionForFolder;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ImportControl"/> class.
        /// </summary>
        public ImportControl()
        {
            InitializeComponent();
            fileBrowser = new FileBrowser();
            batSummary = new BatSummary();
            fileProcessor = new FileProcessor();
        }

        /// <summary>
        ///     The file browser
        /// </summary>
        public FileBrowser fileBrowser { get; set; }

        /// <summary>
        ///     Processes the files. fileBrowser.TextFileNames contains a list of .txt files in the
        ///     folder that is to be processed. The .txt files are label files or at least in a
        ///     compatible format similar to that produced by Audacity. There may also be a header
        ///     file which contains information about the recording session which will be generated
        ///     from this file set. The header file should start with the tag [COPY].
        ///     fileProcessor.ProcessFile does the work on each file in turn.
        /// </summary>
        public void ProcessFiles()
        {
            Dictionary<String, BatStats> TotalBatsFound = new Dictionary<string, BatStats>();

            // process the files one by one
            if (fileBrowser.TextFileNames.Count > 0)
            {
                if (sessionForFolder != null && sessionForFolder.Id > 0)
                {
                    tbkOutputText.Text = sessionForFolder.ToFormattedString();
                }
                foreach (var filename in fileBrowser.TextFileNames)
                {
                    if (!String.IsNullOrWhiteSpace(fileBrowser.headerFileName) && filename == fileBrowser.headerFileName)
                    {
                        // skip this file if it has been identified as the header data file, since
                        // the information should have been included as the session record header
                        // and this would be a duplicate.
                    }
                    else
                    {
                        tbkOutputText.Text = tbkOutputText.Text + "***\n\n" + fileProcessor.ProcessFile(batSummary, filename, gpxHandler, CurrentSessionId) + "\n";
                        TotalBatsFound = BatsConcatenate(TotalBatsFound, fileProcessor.BatsFound);
                    }
                }
                tbkOutputText.Text = tbkOutputText.Text + "\n#########\n\n";
                if (TotalBatsFound != null && TotalBatsFound.Count > 0)
                {
                    foreach (var bat in TotalBatsFound)
                    {
                        bat.Value.batCommonName = bat.Key;
                        tbkOutputText.Text += Tools.GetFormattedBatStats(bat.Value, false) + "\n";

                        //tbkOutputText.Text = tbkOutputText.Text +
                        // FileProcessor.FormattedBatStats(bat) + "\n";
                    }
                }
            }
        }

        /// <summary>
        ///     Reads all the files selected through a File Open Dialog. File names are contained a
        ///     fileBrowser instance which was used to select the files. Adds all the file names to
        ///     a combobox and also loads the contents into a stack of Text Boxes in the left pane
        ///     of the screen.
        /// </summary>
        public String ReadSelectedFiles()
        {
            String outputLocation = "";
            if (fileBrowser.TextFileNames != null && fileBrowser.TextFileNames.Count > 0)
            {
                File.Create(fileBrowser.OutputLogFileName);
                if (dpMMultiWindowPanel.Children.Count > 0)
                {
                    foreach (var child in dpMMultiWindowPanel.Children)
                    {
                        (child as TextBox).Clear();
                    }
                    dpMMultiWindowPanel.Children.Clear();
                }
                ObservableCollection<TextBox> textFiles = new ObservableCollection<TextBox>();
                foreach (var file in fileBrowser.TextFileNames)
                {
                    TextBox tb = new TextBox();
                    tb.AcceptsReturn = true; ;
                    tb.AcceptsTab = true;
                    tb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
                    if (File.Exists(file))
                    {
                        try
                        {
                            StreamReader sr = File.OpenText(file);
                            string firstline = sr.ReadLine();
                            sr.Close();
                            if (!(firstline.Contains("[LOG]") || firstline.Contains("***")))
                            {
                                //if (!file.EndsWith(".log.txt"))
                                //{
                                tb.Text = file + @"
    " + File.ReadAllText(file);
                                dpMMultiWindowPanel.Children.Add(tb);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                        }
                    }
                }
                if (!String.IsNullOrWhiteSpace(fileBrowser.OutputLogFileName))
                {
                    outputLocation = "Output File:- " + fileBrowser.OutputLogFileName;
                }
                else
                {
                    outputLocation = "Output to:- " + fileBrowser.WorkingFolder;
                }
            }
            else
            {
                outputLocation = "";
            }

            tbkOutputText.Text = "[LOG]\n";
            return (outputLocation);
        }

        /// <summary>
        ///     Saves the output file.
        /// </summary>
        public bool SaveOutputFile()
        {
            bool isSaved = false;
            String ofn = fileBrowser.OutputLogFileName;
            if (!String.IsNullOrWhiteSpace(tbkOutputText.Text))
            {
                if (MessageBox.Show("Save Output File?", "Save", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    if (File.Exists(fileBrowser.OutputLogFileName))
                    {
                        if (MessageBox.Show
                            ("Overwrite existing\n" + fileBrowser.OutputLogFileName +
                            "?", "Overwrite File", MessageBoxButton.YesNo) == MessageBoxResult.No)
                        {
                            int index = 1;
                            ofn = fileBrowser.OutputLogFileName.Substring(0, fileBrowser.OutputLogFileName.Length - 4) + "." + index;

                            while (File.Exists(ofn + ".txt"))
                            {
                                index++;
                                ofn = ofn.Substring(0, ofn.LastIndexOf('.'));
                                ofn = ofn + "." + (index);
                            }
                        }
                        else
                        {
                            File.Delete(fileBrowser.OutputLogFileName);
                            ofn = fileBrowser.OutputLogFileName;
                        }
                    }
                    else
                    {
                        ofn = fileBrowser.OutputLogFileName;
                    }
                    File.WriteAllText(ofn, tbkOutputText.Text);
                    ofn = ofn.Substring(0, ofn.Length - 8) + ".manifest";

                    File.WriteAllLines(ofn, fileBrowser.TextFileNames);
                    isSaved = true;
                }
            }
            return (isSaved);
        }

        /// <summary>
        ///     Opens the folder.
        /// </summary>
        internal void OpenFolder()
        {
            if (!String.IsNullOrWhiteSpace(fileBrowser.SelectFolder()))
            {
                ReadFolder();
            }
        }

        internal void ReadFolder()

        {
            if (fileBrowser != null && !String.IsNullOrWhiteSpace(fileBrowser.WorkingFolder))
            {
                ReadSelectedFiles();
                gpxHandler = new GpxHandler(fileBrowser.WorkingFolder);
                sessionForFolder = GetNewRecordingSession(fileBrowser);
                RecordingSessionForm sessionForm = new RecordingSessionForm();
                sessionForm.SetRecordingSession(sessionForFolder);
                if (sessionForm.ShowDialog() ?? false)
                {
                    sessionForFolder = sessionForm.GetRecordingSession();
                    DBAccess.UpdateRecordingSession(sessionForFolder);
                    CurrentSessionTag = sessionForFolder.SessionTag;
                    CurrentSessionId = DBAccess.GetRecordingSession(CurrentSessionTag).Id;
                }
            }
        }

        internal void SortFileOrder()
        {
            if (fileBrowser != null && fileBrowser.TextFileNames.Count > 1)
            {
                FileOrderDialog fod = new FileOrderDialog();
                fod.Populate(fileBrowser.TextFileNames);
                bool? result = fod.ShowDialog();
                if (result != null && result.Value)
                {
                    fileBrowser.TextFileNames = fod.GetFileList();
                    ReadSelectedFiles();
                }
            }
        }

        /// <summary>
        ///     Batses the concatenate.
        /// </summary>
        /// <param name="TotalBatsFound">
        ///     The total bats found.
        /// </param>
        /// <param name="NewBatsFound">
        ///     The new bats found.
        /// </param>
        /// <returns>
        ///     </returns>
        private Dictionary<string, BatStats> BatsConcatenate(Dictionary<string, BatStats> TotalBatsFound, Dictionary<string, BatStats> NewBatsFound)
        {
            if (TotalBatsFound == null || NewBatsFound == null) return (TotalBatsFound);
            if (NewBatsFound.Count > 0)
            {
                foreach (var bat in NewBatsFound)
                {
                    if (TotalBatsFound.ContainsKey(bat.Key))
                    {
                        TotalBatsFound[bat.Key].Add(bat.Value);
                    }
                    else
                    {
                        TotalBatsFound.Add(bat.Key, bat.Value);
                    }
                }
            }
            return (TotalBatsFound);
        }

        private RecordingSession GetNewRecordingSession(FileBrowser fileBrowser)
        {
            RecordingSession newSession = new RecordingSession();

            newSession = SessionManager.PopulateSession(newSession, fileBrowser);
            return (newSession);
        }

        private void ImportFolderButton_Click(object sender, RoutedEventArgs e)
        {
            fileBrowser = new FileBrowser();
            fileBrowser.SelectRootFolder();
            NextFolderButton.IsEnabled = true;
            NextFolderButton_Click(sender, e);
        }

        private void NextFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (fileBrowser.wavFileFolders != null && fileBrowser.wavFileFolders.Count > 0)
            {
                fileBrowser.ProcessFolder(fileBrowser.PopWavFolder());
                ReadFolder();
                SortFileOrderButton.IsEnabled = true;
                ProcessFilesButton.IsEnabled = true;
                FilesToProcessLabel.Content = fileBrowser.wavFileFolders.Count + " Folders to Process";
                if (fileBrowser.wavFileFolders.Count <= 1)
                {
                    SelectFoldersButton.IsEnabled = false;
                }
                else
                {
                    SelectFoldersButton.IsEnabled = true;
                }
                if (fileBrowser.wavFileFolders.Count > 0)
                {
                    NextFolderButton.IsEnabled = true;
                }
                else
                {
                    NextFolderButton.IsEnabled = false;
                }
            }
            else
            {
                SortFileOrderButton.IsEnabled = false;
                ProcessFilesButton.IsEnabled = false;
                SelectFoldersButton.IsEnabled = false;
                NextFolderButton.IsEnabled = false;
            }
        }

        private void ProcessFilesButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessFiles();
        }

        private void SelectFoldersButton_Click(object sender, RoutedEventArgs e)
        {
            if (fileBrowser != null && fileBrowser.wavFileFolders != null && fileBrowser.wavFileFolders.Count > 1)
            {
                FolderSelectionDialog fsd = new FolderSelectionDialog();
                fsd.FolderList = fileBrowser.wavFileFolders;
                fsd.ShowDialog();
                if (fsd.DialogResult ?? false)
                {
                    fileBrowser.wavFileFolders = fsd.FolderList;
                    FilesToProcessLabel.Content = fileBrowser.wavFileFolders.Count + " Folders to Process";
                }
            }
        }

        private void SortFileOrderButton_Click(object sender, RoutedEventArgs e)
        {
            SortFileOrder();
        }
    }
}