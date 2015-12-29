using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;

namespace BatRecordingManager
{
    /// <summary>
    /// Class to handle selection of input, and by derivation
    /// or selection, and output file, the names of which are
    /// made available publicly
    /// Public functions:-
    /// Select folder
    /// Select Manual Log File
    /// Create Output File
    /// </summary>
    public class FileBrowser
    {
        /// <summary>
        /// The working folder
        /// </summary>
        private string workingFolder;

        /// <summary>
        /// Gets or sets the working folder.
        /// </summary>
        /// <value>
        /// The working folder.
        /// </value>
        public string WorkingFolder
        {
            get
            {
                if (String.IsNullOrWhiteSpace(workingFolder)) { return (@""); }
                if (workingFolder.EndsWith(@"\")) { return ((string)workingFolder); }
                else { return ((string)workingFolder + @"\"); }
            }
            set { workingFolder = (string)value; }
        }

        /// <summary>
        /// The existing log file name
        /// </summary>
        private string existingLogFileName = "";

        /// <summary>
        /// Gets the header file.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        internal string[] GetHeaderFile()
        {
            if (textFileNames != null && textFileNames.Count() > 0)
            {
                foreach (var file in textFileNames)
                {
                    String[] lines = File.ReadAllLines(file);
                    if (lines[0].Contains("[COPY]"))
                    {
                        return (lines);
                    }
                }
            }
            return (null);
        }

        /// <summary>
        /// Accessor for existingLogFileName.
        /// existingLogFileName contains the fully qualified file name
        /// of a pre-existing log file.
        /// </summary>
        /// <value>
        /// The name of the existing log file.
        /// </value>
        public string ExistingLogFileName
        {
            get { return (existingLogFileName); }
            set { existingLogFileName = (string)value; }
        }

        /// <summary>
        /// The output log file name
        /// </summary>
        private string outputLogFileName = "";

        /// <summary>
        /// Accessor for outputLogFileName.
        /// outputLogFileName contains the manufactured, fully qualified
        /// path and file name to which the concatenation of log files will
        /// be written.
        /// </summary>
        /// <value>
        /// The name of the output log file.
        /// </value>
        public string OutputLogFileName
        {
            get
            {
                if (String.IsNullOrWhiteSpace(outputLogFileName))
                {
                    if (!String.IsNullOrWhiteSpace(WorkingFolder))
                    {
                        string FolderName = WorkingFolder.Substring(0, WorkingFolder.Length - 1);

                        if (FolderName.Contains(@"\"))
                        {
                            int finalSeparator;
                            finalSeparator = FolderName.LastIndexOf(@"\");
                            FolderName = FolderName.Substring(finalSeparator);
                            if (!String.IsNullOrWhiteSpace(FolderName))
                            {
                                if (FolderName.StartsWith(@"\"))
                                {
                                    FolderName = FolderName.Substring(1);
                                }
                            }
                        }

                        outputLogFileName = WorkingFolder + FolderName + ".log.txt";
                    }
                }

                return (outputLogFileName);
            }
            set { outputLogFileName = (string)value; }
        }

        /// <summary>
        /// The text file names
        /// </summary>
        private List<String> textFileNames = new List<String>();

        /// <summary>
        /// public accessor for textFileNames.
        /// textFileNames lists all the files to be processed and concatenated into
        /// a single log file.
        /// </summary>
        /// <value>
        /// The text file names.
        /// </value>
        public List<String> TextFileNames
        {
            get
            {
                if (textFileNames == null || textFileNames.Count <= 0)
                {
                    SelectLogFiles();
                }
                return (textFileNames);
            }
            set { textFileNames = (List<String>)value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileBrowser"/> class.
        /// </summary>
        public FileBrowser()
        {
        }

        /// <summary>
        /// Selects the folder.
        /// </summary>
        /// <returns></returns>
        public string SelectFolder()
        {
            string folderPath = Directory.GetCurrentDirectory();
            using (System.Windows.Forms.FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the folder containing the descriptive text files";
                dialog.ShowNewFolderButton = true;
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    folderPath = dialog.SelectedPath;
                    textFileNames.Clear();
                    var manifests = Directory.GetFiles(folderPath, "*.manifest", SearchOption.TopDirectoryOnly);
                    if (manifests != null && manifests.Count() > 0 && File.Exists(manifests[0]))
                    {
                        MessageBoxResult result = System.Windows.MessageBox.Show("Manifest file '" +
                            manifests[0] +
                            "exists in this folder.\nDo you wish to use this manifest?",
                            "Existing Manifest File",
                            MessageBoxButton.YesNoCancel);
                        if (result == MessageBoxResult.Cancel) return (null);
                        if (result == MessageBoxResult.Yes)
                        {
                            var manifestFiles = File.ReadAllLines(manifests[0]);
                            foreach (var file in manifestFiles)
                            {
                                if (File.Exists(file))
                                {
                                    textFileNames.Add(file);
                                }
                            }
                        }
                    }
                    if (textFileNames.Count <= 0)
                    {
                        foreach (string filename in Directory.GetFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly))
                        {
                            if (!filename.EndsWith(".log.txt"))
                            {
                                textFileNames.Add(filename);
                            }
                        }
                        textFileNames.Sort();
                    }
                }
                else { return (null); }
            }
            WorkingFolder = folderPath;

            return (folderPath);
        }

        /// <summary>
        /// Selects the log files.
        /// </summary>
        /// <returns></returns>
        public String SelectLogFiles()
        {
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
                dialog.Filter = "txt files|*.txt|Log files|*.log";

                dialog.Multiselect = true;
                dialog.Title = "Select one or more descriptive text files";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (dialog.FileNames.Count() > 0)
                    {
                        textFileNames.Clear();
                        foreach (var filename in dialog.FileNames)
                        {
                            textFileNames.Add(filename);
                        }
                    }
                }
            }
            if (textFileNames.Count > 0) return (textFileNames[0]);
            return ("");
        }
    }
}