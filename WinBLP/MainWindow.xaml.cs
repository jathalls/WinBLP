using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace WinBLPdB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The file browser
        /// </summary>
        private FileBrowser fileBrowser;

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

        /// <summary>
        /// The is saved
        /// </summary>
        private bool isSaved = false;

       
        /// <summary>
        /// The build
        /// </summary>
        private String Build;

        /// <summary>
        /// The window title
        /// </summary>
        private String windowTitle = "Bat Log File Processor - v";

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            Build = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //windowTitle = "Bat Log File Processor " + Build;
            this.Title = windowTitle + Build;
            this.InvalidateArrange();
            fileBrowser = new FileBrowser();
            fileProcessor = new FileProcessor();
            batSummary = new BatSummary();
        }

        /// <summary>
        /// Responds to selection File/OpenFiles
        /// Produces a file selection dialog and reads
        /// all the selected files.  Uses the FileBrowser
        /// class to select and store the selected file names.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void miOpen_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(fileBrowser.SelectLogFiles()))
            {
                ReadSelectedFiles();
            }
        }

        /// <summary>
        /// Reads all  the files selected through a File Open Dialog.
        /// File names are contained a fileBrowser instance which was
        /// used to select the files.
        /// Adds all the file names to a combobox and also loads the
        /// contents into a stack of Text Boxes in the left pane of the
        /// screen.
        /// </summary>
        private void ReadSelectedFiles()
        {
            if (fileBrowser.TextFileNames != null && fileBrowser.TextFileNames.Count > 0)
            {
                miProcess.IsEnabled = true;
                miSortOrder.IsEnabled = true;

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
                    OutputLocationLabel.Content = "Output File:- " + fileBrowser.OutputLogFileName;
                }
                else
                {
                    OutputLocationLabel.Content = "Output to:- " + fileBrowser.WorkingFolder;
                }
            }
            else
            {
                miSortOrder.IsEnabled = false;
                miProcess.IsEnabled = false;
                OutputLocationLabel.Content = "";
            }

            tbkOutputText.Text = "[LOG]\n";
            /*
            if (File.Exists(fileBrowser.OutputLogFileName))
            {
                tbkOutputText.Text = File.ReadAllText(fileBrowser.OutputLogFileName);
                miProcess.IsEnabled = false;
                miSave.IsEnabled = true;
            }*/
        }

        /// <summary>
        /// Handles the Click event of the miProcess control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void miProcess_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<String, BatStats> TotalBatsFound = new Dictionary<string, BatStats>();

            // process the files one by one
            if (fileBrowser.TextFileNames.Count > 0)
            {
                foreach (var filename in fileBrowser.TextFileNames)
                {
                    tbkOutputText.Text = tbkOutputText.Text + "***\n\n" + fileProcessor.ProcessFile(batSummary, filename, gpxHandler) + "\n";
                    TotalBatsFound = BatsConcatenate(TotalBatsFound, fileProcessor.BatsFound);
                }
                tbkOutputText.Text = tbkOutputText.Text + "\n#########\n\n";
                if (TotalBatsFound != null && TotalBatsFound.Count > 0)
                {
                    foreach (var bat in TotalBatsFound)
                    {
                        tbkOutputText.Text = tbkOutputText.Text +
                            FileProcessor.FormattedBatStats(bat) + "\n";
                    }
                }
            }

            //write the text to the output file
            miSave.IsEnabled = true;
            SaveOutputFile();
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
        /// Handles the Closing event of the Window control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!isSaved && !String.IsNullOrWhiteSpace(tbkOutputText.Text))
            {
                SaveOutputFile();
            }
        }

        /// <summary>
        /// Handles the Click event of the miSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void miSave_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(tbkOutputText.Text))
            {
                SaveOutputFile();
            }
        }

        /// <summary>
        /// Saves the output file.
        /// </summary>
        private void SaveOutputFile()
        {
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
        }

        /// <summary>
        /// Allows the user to select all the text files within a single folder
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void miOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(fileBrowser.SelectFolder()))
            {
                ReadSelectedFiles();
                gpxHandler = new GpxHandler(fileBrowser.WorkingFolder);
            }
        }

        /// <summary>
        /// Allows the user to change the order in which the selected files will be processed
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void miSortOrder_Click(object sender, RoutedEventArgs e)
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
        /// Quits the program
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void miExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
            App.Current.Shutdown();
        }

        /// <summary>
        /// Display the About box
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void miAbout_Click(object sender, RoutedEventArgs e)
        {
            AboutScreen about = new AboutScreen();
            about.ShowDialog();
        }

        /// <summary>
        /// Handles the Click event of the miHelp control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void miHelp_Click(object sender, RoutedEventArgs e)
        {
            HelpScreen help = new HelpScreen();
            help.ShowDialog();
        }

        /// <summary>
        /// Handles the Click event of the miEditBatList control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void miEditBatList_Click(object sender, RoutedEventArgs e)
        {
            if(batSummary!= null)
            {
                BatEditor batEditor = new BatEditor(batSummary.batReferenceDataContext);
                var result=batEditor.ShowDialog();
                if(result!=null && result.Value)
                {
                    //batSummary.RefreshBatList();
                }
            }
        }
    }
}