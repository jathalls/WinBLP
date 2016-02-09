using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace BatRecordingManager
{
    /// <summary>
    ///     Class to handle selection of input, and by derivation or selection, and output file, the
    ///     names of which are made available publicly Public functions:- Select folder Select
    ///     Manual Log File Create Output File
    /// </summary>
    public class FileBrowser
    {
        /// <summary>
        ///     The root folder
        /// </summary>
        public string rootFolder = "";

        /// <summary>
        ///     The wav file folders
        /// </summary>
        public ObservableCollection<String> wavFileFolders = new ObservableCollection<String>();

        /// <summary>
        ///     The existing log file name
        /// </summary>
        private string existingLogFileName = "";

        /// <summary>
        ///     The output log file name
        /// </summary>
        private string outputLogFileName = "";

        /// <summary>
        ///     The text file names
        /// </summary>
        private ObservableCollection<String> textFileNames = new ObservableCollection<String>();

        /// <summary>
        ///     The working folder
        /// </summary>
        private string workingFolder;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FileBrowser"/> class.
        /// </summary>
        public FileBrowser()
        {
        }

        /// <summary>
        ///     Accessor for existingLogFileName. existingLogFileName contains the fully qualified
        ///     file name of a pre-existing log file.
        /// </summary>
        /// <value>
        ///     The name of the existing log file.
        /// </value>
        public string ExistingLogFileName
        {
            get { return (existingLogFileName); }
            set { existingLogFileName = (string)value; }
        }

        /// <summary>
        ///     Gets or sets the name of the header file.
        /// </summary>
        /// <value>
        ///     The name of the header file.
        /// </value>
        public String headerFileName { get; set; }

        /// <summary>
        ///     Accessor for outputLogFileName. outputLogFileName contains the manufactured, fully
        ///     qualified path and file name to which the concatenation of log files will be written.
        /// </summary>
        /// <value>
        ///     The name of the output log file.
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
            set
            {
                outputLogFileName = (string)value;
            }
        }

        /// <summary>
        ///     public accessor for textFileNames. textFileNames lists all the files to be processed
        ///     and concatenated into a single log file.
        /// </summary>
        /// <value>
        ///     The text file names.
        /// </value>
        public ObservableCollection<String> TextFileNames
        {
            get
            {
                if (textFileNames == null || textFileNames.Count <= 0)
                {
                    SelectLogFiles();
                }
                return (textFileNames);
            }
            set
            {
                textFileNames = (ObservableCollection<String>)value;
            }
        }

        /// <summary>
        ///     Gets or sets the working folder.
        /// </summary>
        /// <value>
        ///     The working folder.
        /// </value>
        public string WorkingFolder
        {
            get
            {
                if (String.IsNullOrWhiteSpace(workingFolder)) { return (@""); }
                if (workingFolder.EndsWith(@"\")) { return ((string)workingFolder); }
                else { return ((string)workingFolder + @"\"); }
            }
            set
            {
                workingFolder = (string)value;
            }
        }

        /// <summary>
        ///     Pops the wav folder. Returns the next wav file folder from the list and removes it
        ///     from the list.
        /// </summary>
        /// <returns>
        ///     </returns>
        public String PopWavFolder()
        {
            String result = "";
            if (wavFileFolders != null && wavFileFolders.Count > 0)
            {
                result = wavFileFolders[0];
                wavFileFolders.Remove(result);
                workingFolder = result;
            }
            return (result);
        }

        /// <summary>
        ///     Processes the folder.
        /// </summary>
        /// <param name="folder">
        ///     The folder.
        /// </param>
        /// <returns>
        ///     </returns>
        public string ProcessFolder(String folder)
        {
            string folderPath = workingFolder;
            if (!String.IsNullOrWhiteSpace(folder))
            {
                if (Directory.Exists(folder))
                {
                    folderPath = folder;
                    textFileNames.Clear();
                    var manifests = Directory.GetFiles(folderPath, "*.manifest", SearchOption.TopDirectoryOnly);
                    if (manifests != null && manifests.Count() > 0 && File.Exists(manifests[0]))
                    {
                        textFileNames.Clear();
                        var manifestFiles = File.ReadAllLines(manifests[0]);
                        foreach (var file in manifestFiles)
                        {
                            if (File.Exists(file))
                            {
                                textFileNames.Add(file);
                            }
                        }
                    }
                    if (textFileNames.Count <= 0)
                    {
                        var files = from file in Directory.GetFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly)
                                    orderby file
                                    select file;
                        textFileNames.Clear();
                        foreach (string filename in files)
                        {
                            if (!filename.EndsWith(".log.txt"))
                            {
                                textFileNames.Add(filename);
                            }
                        }
                    }
                }
                else { return (null); }
            }
            WorkingFolder = folderPath;

            return (folderPath);
        }

        /// <summary>
        ///     Selects the folder.
        /// </summary>
        /// <returns>
        ///     </returns>
        public string SelectFolder()
        {
            string folderPath = Directory.GetCurrentDirectory();
            using (System.Windows.Forms.FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select the folder containing the .wav and descriptive text files";
                dialog.ShowNewFolderButton = true;
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    folderPath = dialog.SelectedPath;
                    ProcessFolder(folderPath);
                }
            }
            return (folderPath);
        }

        /// <summary>
        ///     Selects the log files.
        /// </summary>
        /// <returns>
        ///     </returns>
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

        /// <summary>
        ///     Selects the root folder using the FolderBrowserDialog and puts all the folders with
        ///     both .wav and .txt files
        /// </summary>
        /// <returns>
        ///     </returns>
        public string SelectRootFolder()
        {
            string folderPath = Directory.GetCurrentDirectory();
            using (System.Windows.Forms.FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                wavFileFolders.Clear();
                dialog.Description = "Select the folder containing the descriptive text/wav files";
                dialog.ShowNewFolderButton = true;
                dialog.RootFolder = Environment.SpecialFolder.MyComputer;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    folderPath = dialog.SelectedPath;

                    rootFolder = dialog.SelectedPath;

                    wavFileFolders = GenerateFolderList(rootFolder);
                }
            }
            return (rootFolder);
        }

        /// <summary>
        ///     Selects the wav file. Uses the OpenFileDialog to allow the user to select a single
        ///     .wav file and returns places that name in as the only entrey in the textFileNames
        ///     list and also returns the name
        /// </summary>
        /// <returns>
        ///     </returns>
        public String SelectWavFile()
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
                dialog.Filter = "wav files|*.wav";

                dialog.Multiselect = false;
                dialog.Title = "Select one  recording .wav file";
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

        /// <summary>
        ///     Gets the header file.
        /// </summary>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal string[] GetHeaderFile()
        {
            if (textFileNames != null && textFileNames.Count() > 0)
            {
                foreach (var file in textFileNames)
                {
                    String[] lines = File.ReadAllLines(file);
                    if (lines[0].Contains("[COPY]"))
                    {
                        headerFileName = file;
                        return (lines);
                    }
                }
            }
            return (null);
        }

        /// <summary>
        ///     Gets the unqualified filename from the TextFileNames list at the specified index.
        /// </summary>
        /// <param name="index">
        ///     The index of the filename to extract
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        internal string GetUnqualifiedFilename(int index)
        {
            string result = "";
            if (index >= 0 && textFileNames != null && textFileNames.Count > index)
            {
                string qualifiedname = textFileNames[index];
                int lastSeperator = qualifiedname.LastIndexOf('\\');
                if (lastSeperator >= 0)
                {
                    result = qualifiedname.Substring(lastSeperator);
                }
            }
            return (result);
        }

        /// <summary>
        ///     Determines whether [contains wav and txt files] [the specified root folder].
        /// </summary>
        /// <param name="baseFolder">
        ///     The base folder.
        /// </param>
        /// <param name="ext">
        ///     the file extension to be matched
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private bool ContainsFilesOfType(string baseFolder, string ext)
        {
            if (!String.IsNullOrWhiteSpace(baseFolder))
            {
                var files = Directory.EnumerateFiles(baseFolder);
                if (files != null && files.Count() > 0)
                {
                    var wavFiles = from file in files
                                   where file.ToUpper().EndsWith(ext.ToUpper())
                                   select file;
                    if (wavFiles != null && wavFiles.Count() > 0)
                    {
                        return (true);
                    }
                }
            }
            return (false);
        }

        /// <summary>
        ///     Generates a list of folders with wav files beneath the rootFolder supplied. The
        ///     Directory tree is traversed and each folder containing wav files is added to the
        ///     list, but once such a folder is identified its child folders are not included in the search.
        /// </summary>
        /// <param name="rootFolder">
        ///     The root folder.
        /// </param>
        /// <returns>
        ///     </returns>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private ObservableCollection<String> GenerateFolderList(string rootFolder)
        {
            ObservableCollection<String> wavFolders = new ObservableCollection<String>();
            GetWavFileFolder(rootFolder, ref wavFolders);

            return (wavFolders);
        }

        /// <summary>
        ///     Gets the wav file folder by recursive search of the directory tree adding
        ///     appropriate folders to the list passed by reference.
        /// </summary>
        /// <param name="rootFolder">
        ///     The root folder.
        /// </param>
        /// <param name="wavFolders">
        ///     The wav folders.
        /// </param>
        /// <exception cref="System.NotImplementedException">
        ///     </exception>
        private void GetWavFileFolder(string rootFolder, ref ObservableCollection<String> wavFolders)
        {
            if (ContainsFilesOfType(rootFolder, ".wav") && ContainsFilesOfType(rootFolder, ".txt"))
            {
                wavFolders.Add(rootFolder);
                return;
            }
            var children = Directory.EnumerateDirectories(rootFolder);
            if (children != null || children.Count() > 0)
            {
                foreach (var child in children)
                {
                    GetWavFileFolder(child, ref wavFolders);
                }
            }
        }
    }
}