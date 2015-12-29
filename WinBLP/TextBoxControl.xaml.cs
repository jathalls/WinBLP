using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for TextBoxControl.xaml
    /// </summary>
    public partial class TextBoxControl : UserControl
    {
        /// <summary>
        /// The file browser
        /// </summary>
        public FileBrowser fileBrowser { get; set; }

        /// <summary>
        /// The file processor
        /// </summary>
        private FileProcessor fileProcessor;

        /// <summary>
        /// The GPX handler
        /// </summary>
        private GpxHandler gpxHandler;

        /// <summary>
        /// The bat summary
        /// </summary>
        private BatSummary batSummary;

        public int CurrentSessionId = -1;
        public string CurrentSessionTag = "";

        public TextBoxControl()
        {
            InitializeComponent();
            fileBrowser = new FileBrowser();
            batSummary = new BatSummary();
            fileProcessor = new FileProcessor();
        }

        /// <summary>
        /// Reads all  the files selected through a File Open Dialog.
        /// File names are contained a fileBrowser instance which was
        /// used to select the files.
        /// Adds all the file names to a combobox and also loads the
        /// contents into a stack of Text Boxes in the left pane of the
        /// screen.
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
                List<TextBox> textFiles = new List<TextBox>();
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

        public void ProcessFiles()
        {
            Dictionary<String, BatStats> TotalBatsFound = new Dictionary<string, BatStats>();

            // process the files one by one
            if (fileBrowser.TextFileNames.Count > 0)
            {
                foreach (var filename in fileBrowser.TextFileNames)
                {
                    tbkOutputText.Text = tbkOutputText.Text + "***\n\n" + fileProcessor.ProcessFile(batSummary, filename, gpxHandler, CurrentSessionId) + "\n";
                    TotalBatsFound = BatsConcatenate(TotalBatsFound, fileProcessor.BatsFound);
                }
                tbkOutputText.Text = tbkOutputText.Text + "\n#########\n\n";
                if (TotalBatsFound != null && TotalBatsFound.Count > 0)
                {
                    foreach (var bat in TotalBatsFound)
                    {
                        bat.Value.batCommonName = bat.Key;
                        tbkOutputText.Text += Tools.GetFormattedBatStats(bat.Value,false) + "\n";

                        //tbkOutputText.Text = tbkOutputText.Text +
                        // FileProcessor.FormattedBatStats(bat) + "\n";
                    }
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
        /// Opens the folder.
        /// </summary>
        internal void OpenFolder()
        {
            if (!String.IsNullOrWhiteSpace(fileBrowser.SelectFolder()))
            {
                ReadSelectedFiles();
                gpxHandler = new GpxHandler(fileBrowser.WorkingFolder);
                RecordingSession sessionForFolder = GetNewRecordingSession(fileBrowser);
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

        private RecordingSession GetNewRecordingSession(FileBrowser fileBrowser)
        {
            RecordingSession newSession = new RecordingSession();

            newSession = SessionManager.PopulateSession(newSession, fileBrowser);
            return (newSession);
        }

        /// <summary>
        /// Batses the concatenate.
        /// </summary>
        /// <param name="TotalBatsFound">The total bats found.</param>
        /// <param name="NewBatsFound">The new bats found.</param>
        /// <returns></returns>
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

        /// <summary>
        /// Saves the output file.
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
    }
}