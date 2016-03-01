using System;
using System.Collections.Generic;
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

        private bool isProcessingFolder = false;

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
        ///     Processes the folder. Called when a new folder is to be dealt with. If there is a
        ///     manifext file, copies the file names from that into the file list. if not, then
        ///     copies names of all .txt files except .log.txt files. if there are still just 0 or 1
        ///     file in the list, try to extract comment files from a log file
        /// </summary>
        /// <param name="folder">
        ///     The folder.
        /// </param>
        /// <returns>
        ///     </returns>
        public string ProcessFolder(String folder)
        {
            string folderPath = workingFolder;
            if (!String.IsNullOrWhiteSpace(folder))// can't do anything without a folder to read files from
            {
                if (Directory.Exists(folder)) // still can't do anything if the folder doesn't exist (in which case return null)
                {
                    folderPath = folder;
                    textFileNames.Clear();
                    var manifests = Directory.GetFiles(folderPath, "*.manifest", SearchOption.TopDirectoryOnly);
                    if (manifests != null && manifests.Count() > 0 && File.Exists(manifests[0])) // we have a manifest
                    {// so add the files in the manifest tot he files list
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
                    if (textFileNames.Count <= 0) // if we have no files yet - no or empty manifest
                    {//get all the text files in the folder
                        var files = from file in Directory.GetFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly)
                                    orderby file
                                    select file;
                        textFileNames.Clear();
                        foreach (string filename in files)
                        {
                            if (!filename.EndsWith(".log.txt"))// except for .log.txt files
                            {
                                textFileNames.Add(filename);
                            }
                        }
                    }
                    if (textFileNames.Count <= 1)
                    {
                        if (!isProcessingFolder)
                        {
                            isProcessingFolder = true;// to prevent call to this function from ExtractCommentFilesFromLogFile
                            // recursing. It can recurse once but after that will be blocked
                            folderPath = ExtractCommentFilesFromLogFile(folder);
                            isProcessingFolder = false;
                        }
                    }
                }
                else
                {
                    isProcessingFolder = false;
                    folderPath = null;
                }
            }
            WorkingFolder = folderPath;
            isProcessingFolder = false;
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
        ///     Extracts the comment files from log file. there ar no or just one text file in the
        ///     selected folder except for .log.txt files. This function identifies a .log.txt file
        ///     with the same name as the folder and splits it into several separate files.
        ///     Everything up to the first occurrence of a .wav filename is extracted into a .txt
        ///     file with the same name as the folder - a new header file. For each .wav file in the
        ///     folder which does not have a matching .txt file, the .log.txt file will be split at
        ///     the first occurrence of the .wav file name and the contents as far the next .wav
        ///     file name will be copied to a .txt file with the same name as the .wav file. Finally
        ///     ProcessFolder is called and it's result returned.
        /// </summary>
        private string ExtractCommentFilesFromLogFile(String folder)
        {
            if (!folder.EndsWith(@"\"))
            {
                folder = folder + @"\";
            }
            string logFileText = "";
            String folderName = GetFoldernameFromPath(folder);
            if (File.Exists(folder + folderName + ".log.txt"))
            {
                logFileText = File.ReadAllText(folder + folderName + ".log.txt");
            }
            if (string.IsNullOrWhiteSpace(logFileText))
            {
                var logFiles = Directory.GetFiles(folder, "*.log.txt");
                if (logFiles == null || logFiles.Count() <= 0)
                {
                    return (folder);
                }
                string biggestFile = "";
                long fileSize = long.MinValue;
                foreach (var file in logFiles)
                {
                    FileInfo f = new FileInfo(file);
                    if (f.Length > fileSize)
                    {
                        fileSize = f.Length;
                        biggestFile = file;
                    }
                }
                if (String.IsNullOrWhiteSpace(biggestFile))
                {
                    return (folder);
                }
                logFileText = File.ReadAllText(biggestFile);
                if (string.IsNullOrWhiteSpace(logFileText))
                {
                    return (folder);
                }
            }
            if (!File.Exists(folder + folderName + ".txt"))
            {
                var sw = File.AppendText(folder + folderName + ".txt");
                int index = logFileText.IndexOf(".wav");
                if (index > 0)
                {
                    sw.Write(logFileText.Substring(0, index));
                    sw.WriteLine();
                }
                else
                {
                    sw.Write(folderName);
                    sw.WriteLine();
                }

                sw.Close();// Header file is complete
            }

            var wavFiles = Directory.GetFiles(folder, "*.wav");
            if (wavFiles != null && wavFiles.Count() > 0)
            {
                foreach (var file in wavFiles)
                {
                    string matchingTextFile = file.Substring(0, file.Length - 3) + "txt";
                    if (!File.Exists(matchingTextFile))
                    {
                        string wavFileName = file.Substring(file.LastIndexOf(@"\") + 1);
                        var sw = File.AppendText(matchingTextFile);

                        int index1 = logFileText.IndexOf(wavFileName);
                        int index2 = -1;
                        if (index1 >= 0)
                        {
                            string rest = logFileText.Substring(index1);
                            string rest2 = rest.Substring(wavFileName.Length);
                            index2 = rest2.IndexOf(".wav") + wavFileName.Length;
                            if (index2 > 0)
                            {
                                try
                                {
                                    string outStr = rest.Substring(0, index2);
                                    sw.Write(outStr);
                                }
                                catch (Exception)
                                {
                                    sw.Write(rest);
                                }
                            }
                            else
                            {
                                sw.Write(rest);
                            }
                        }
                        sw.WriteLine();
                        sw.Close();
                    }
                }
            }

            ProcessFolder(folder);

            return (folder);
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
        ///     Gets the foldername from path.
        /// </summary>
        /// <param name="folder">
        ///     The folder.
        /// </param>
        /// <returns>
        ///     </returns>
        private string GetFoldernameFromPath(string folder)
        {
            if (folder.EndsWith(@"\"))
            {
                folder = folder.Substring(0, folder.Length - 1);
                int index = folder.LastIndexOf(@"\");

                if (index >= 0 && index < folder.Length)
                {
                    folder = folder.Substring(index + 1);
                }
            }
            return (folder);
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