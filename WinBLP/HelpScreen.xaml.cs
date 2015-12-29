using System;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for HelpScreen.xaml
    /// </summary>
    public partial class HelpScreen : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HelpScreen"/> class.
        /// </summary>
        public HelpScreen()
        {
            InitializeComponent();
            FillHelpScreen();
        }

        /// <summary>
        /// Fills the help screen.
        /// </summary>
        private void FillHelpScreen()
        {
            String text;
            text = @"
WinBLP - Windows Bat Log File Processor
by Justin A T Halls
(c)2015

Introduction
WinBLP is a simple program designed to concatenate text log files documenting
a number of recordings of bat vocalizations. Although it may be used for other
purposes.
The program assumes that the recordings are a collection of .WAV files held in
a common folder, and that they have been analysed using Audacity to add comments
to a label track for each recording, which has then been exported as a text file.
This program will then concatenate all the text files contained within a folder to
create a single log file.  in doing so it adds additional timing information about
the duration of the individual files and the specific comments within them.
In particular it identifies references to bat names in the comments and presents
summary information for each file, and for the entire collection of files relating
to each identified bat species.
The bat species, and the tags used to link individual comments to a types of bat, are
defined in a separate .XML reference file which be be edited with an XML file editor
or a simple text editor as long as the XML formatting rules are maintained.

Installation
The program requires the .NET Framework version 4 in order to run.  Other than that the
.ZIP file should be unpacked to a suitable location from which the .EXE file can be run
directly.  All required support files are included in the ZIP file.

Startup
When the program is first run it will show a blank window and a menu with two items -
File and Help.
The Help menu has two items in it - Help and About.  About produces a small information
window and Help displays this text.
The File menu has the following items:-
Open Files - opens individual text files to be processed
Open Folder - opens a folder containing text files to be processed
Modify File Order - Allows modification of the order in which the selected files will be
    processed, and allows additional files to be added to the list or removed from it
Process - Concatenates the selected text files and summarises useful information into a log file
Save - Although 'Process' saves the resulting log file, this allows the log file to
    be explicitly saved
Exit - Closes the program

When the initial files have been selected with either Open Files or Open Folder, the name
and location of the prospective output file will be displayed next to the menu.

File Selection
The set of files to be processed can be selected with either Open Files or Open Folder.
The folder containing the selected files will be the destination folder for the output
log file.  Any existing log file (i.e. a file ending .log.txt) will be ignored and will
be overwritten by the new log file.  For this reason it is inadvisable to edit the
generated .log.txt file.
When Open Folder is used, all .txt files in the folder will be processed.  Any file
in which the first line is '[COPY]' will be copied directly to the log file with no
further processing.  Any file starting '[SKIP]' or '[LOG]' will be ignored.
It is advisable to generate a header file, starting with [COPY] and containing overall
information about the recordings, location, equipment and operator etc.
Each of the selected files will be opened and the contents displayed in a series of windows
down the left hand side of the main screen, where thay can be reviewed before processing.

MERGE Filter
A file with a header line [COPY] may also, optionally, contain a section starting with a line
consisting solely of [MERGE].  The following lines will be individually appended to sequential
processed files, just after the GPS co-ordinates (if any).
The suggested way of using these features is to create a text file which describes the current
survey, operators, equipment etc. which will begin with a line [COPY] so that the program will
not attempt to process the information, but will simply copy this directly into the log file.
It is suggested that this should be the first file in the file list and the Modify File Order
process described below can be used to ensure that this is the case.
After running the program, the log file will (if a GPX file was present) contain co-ordinates
for the start and end of each recorded segment.  The start co-ordinates can be copied and pasted into map.bing.com
in order to identify the location on a map or aerial imagery.  Then a [MERGE] section can be
appended to the header text file with a one-line description of the location for each wav file
segment.  If the program is then re-run the log file will not only contain the GPS co-ordinates
for each segment but a helpful description.  If a GPX file is not available the descriptions may
be taken from written notes taken at the time, or from audio notes included in the recordings.

Modify File Order
This item opens a new window listing all the fully qualified path and file names of the
selected files in the order in which they will be processed.
At the bottom of the window are buttons which can be used to move selected files up or
down the list in order to adjust the sequence in which they are processed.
The DEL button will remove a file from the list and ADD allows additional files to be
added to the list - the additional files do not need to be in the same directory as the
original selection.
the OK button confirms the selection and closes the window - the files displayed in
the main window will be reloaded to reflect the modified ordering.
When the files are processed the modified file list is saved to a .manifest file.  If
Open Folder finds a .manifest file in the selected folder it will ask if you want to reload
the files as listed in the manifest.

Process
This concatenates the files and adds additional extracted information.
The log file starts with a [LOG] header line to prevent recursive processing.
Each file's contents are separated by a line of 3 asterisks '***', folowed
by a line with the fully qualified name of the text file.  If there is a .wav
file with the same name and location, then the file name will be followed by
the duration of the .wav file, calculated from the time contained in the file
name and the time stamp on the file.  This timinag assumes a file name in the format
ttt_yyyymmdd_hhmmss, where ttt is any text, yyyymmdd is the date of the recording
and hhmmss is the time the recording started.
Labels exported by Audacity have the form:-
ss.fffff ss.ffff ttt where ss.ffff is the time offset into the recording of the start
or end of the commented section, and ttt is the textual label or comment.
Lines in this format will be converted in minutes and seconds and the curation of
the label - e.g.
2'13.502 - 2'17.82 = 0'3.580	Soprano Pip
Manually generated labels with a start time, hyphen, end time and comment will also
be processed into the same format wherever possible.
Bat tags, defined in the bat reference file, will be looked for in the comments.
Following the individual comments the recognised bats wil be listed, with the number
of passes (actually the number of recognised comments), minimum duration, maximum
duration, mean duration and the total duration.
If a comment contains multiple bat tags then the duration of theat comment will be
ascribed to each of the recognised species regardless of the actual content within
the .wav file.
After each of the individual files have been processsed there will be a line of ###
following which the total number of passes and duration for each species will be given.
The .log.txt file will be automatically saved at the end of the processing pass.

GPS
If there is a .gpx file in the selected folder then additional GPS location information
will be included in the .log.txt file.  The positions nearest to the start time of
each file and the end time of each file will be logged after the filename.  The
.gpx positions are formatted so that they can be copied directly into
http://map.bing.com in order to identify the precise location of the recording.
The .gpx file is not required.

Bat Reference File
Bat reference information is contained in BatReferenceXMLFile.xml
The first line must be left intact and all other tags are contained in <BatLibrary>
section.
A typical Bat reference is:-
<Bat>
    <BatGenus>Pipistrellus</BatGenus>
    <BatSpecies>pygmaeus</BatSpecies>
    <BatCommonName>Soprano Pipistrelle</BatCommonName>
    <BatTag>Soprano Pip</BatTag>
    <BatTag>P55</BatTag>
    <BatTag>Soprano pipistrelle</BatTag>
    <BatTag>Soprano</BatTag>
  </Bat>
Genus and Species identify the correct latin name for the bat.  The species may
be just 'sp.' to allow for poorly defined species such as a P50 bat.
The common name is the full common name for the bat if known.
The various BatTag entries define the text in a comment which will mark that
comment as relating to this bat.  Any of the BatTags may be present.  If the
BatTag is all upper case then it will only match upper case text in the comment,
otherwise BatTags are not case sensitive.  thus BLE can be used to identify a
Brown Long eared bat but the word 'probable' will not be taken as a reference to
a bat.
The current reference file is not comprehensive and should be added to as
required by the user.
A default reference file is supplied in the setup process and is copied to
%AppData%\Roaming\Echolocation\WinBLP\ named BatReferenceXMLFile.xml
The first time the program is run this will be renamed to EditableBatReferenceXMLFile.xml
This file may be modified and added to using a text editor or a special XML
editor such as XMLNotepad.
When an updated version of the program is installed the previous version must
first be uninstalled.  This will remove any file called BatReferenceFile.xml
but will not remove EditableBatReferenceFile.xml.  When the program is run the
newly supplied reference file will be merged with the editable reference file.  New
<Bat> entries in either file will be copied to the new reference file.  <Bat> antries
with the same name will be copied from the new reference file, but all <BatTag> entries
will be taken from both files.

The Bat reference file may be examined and modified using the 'File/Edit Bat reference File'
command.  A window will be opened displaying all the bats referenced in the file by
their 'name' tags, in the left hand column.  The name tag is usually the Common Name
of the bat with all spaces removed and should be unique in the list.
Selecting a bat will display the details of that bat in the right hand pane.  The width
of the two panes may be adjusted by dragging the border between them.
The right hand pane has sections for the bat's common name, latin name, and the tags used to
identify it in the descriptive labels created in Audacity.
Genus and species are simple editable text boxes, and must not be empty.  If the precise species
is not known use 'sp.'.
Several Common Names are allowed, but only the first one in the list will be used in the program.
New names can be added by typing in the white text box and clicking the ADD button.  Names can be
deleted by selecting them in the list and clicking the DEL button.  The order of the names can be
modified by selecting a name and using the UP and DOWN buttons.
Id Tags can be added deleted and have their order changed in the same way.  The tags a
re checked against the descriptive text in order and the search stops as sonn as a match is made.
Tags and names cannot be edited - a new entry has to be created and the old one deleted.
There must be at least one Common Name and one Tag.
The order of the bats in the list may also be changed using the UP and DOWN buttons at
the bottom of the page.  Selecting a bat and clicking the DEL button will remove it from
the list.  Clicking the ADD button will create a new bat called 'bat', which can then be edited
as described above.
Clicking the OK button will save the modified bat list to the Bat Reference File, and load
it into the program.  Clicking CANCEL will close the window and discard all changes to the file.

";
            HelpText.Text = text;
        }
    }
}