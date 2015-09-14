using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Workflow;
using System.Xml.Linq;
using System.Xml;
using System.IO;

namespace WinBLP
{
    /// <summary>
    /// Interaction logic for BatEditor.xaml
    /// </summary>
    public partial class BatEditor : Window
    {


        public XElement BatList { get; set; }
        public System.Xml.XmlDocument batListDocument { get; set; }
        public String BatReferenceFileLocation
        {
            get; set;
        } = @".\BatReferenceXMLFile.xml";

        public BatEditor(XElement content, String batFileLocation)
        {
            BatList = content;
            if (!String.IsNullOrWhiteSpace(batFileLocation))
            {
                BatReferenceFileLocation = batFileLocation;
            }
            batListDocument = new XmlDocument();
            using (var xmlReader = BatList.CreateReader())
            {
                batListDocument.Load(xmlReader);
            }

            InitializeComponent();

            batData.Document = batListDocument;
            
        }

        private void ReloadXmlDocument(bool noNewSelection)
        {
            if (noNewSelection)
            {
                changing = true;
            }
            int lastSelectedIndex = BatNameListBox.SelectedIndex;
            using (var xmlReader = BatList.CreateReader())
            {
                batListDocument.Load(xmlReader);
            }
            try
            {
                if (lastSelectedIndex < BatNameListBox.Items.Count)
                {
                    if (lastSelectedIndex >= 0)
                    {
                        BatNameListBox.SelectedIndex = lastSelectedIndex;
                    }
                    else
                    {
                        BatNameListBox.SelectedIndex = 0;
                    }
                }
                else
                {
                    BatNameListBox.SelectedIndex = BatNameListBox.Items.Count - 1;
                }
            }
            catch (Exception) { }

        }

        /// <summary>
        /// Handles the Click event of the OKButton control.
        /// Checks the validity of the currently selected bat - if not OK
        /// displays a message box and does nothing else.
        /// If OK, returns with a true result - the caller has the
        /// responsibility for retrieving the modified BatList and
        /// using it.  This function saves the modified list to the 
        /// specified file name and location if present.  It does not
        /// query a file overwrite but will create a .bak version of
        /// the old data.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatNameListBox.SelectedIndex >= 0)
            {
                String errstring = ValidateBat((XmlElement)BatNameListBox.SelectedItem);
                if (!String.IsNullOrWhiteSpace(errstring))
                {
                    DisplayInvalidErrorMessage(errstring);
                    return;
                }

                

            }

            // OK, so the bat selected is valid, others are all untouched or checked
            // when they were deselected
            if (!String.IsNullOrWhiteSpace(BatReferenceFileLocation))
            {
                if (File.Exists(BatReferenceFileLocation))
                {
                    String BackupName = BatReferenceFileLocation.Substring(0, BatReferenceFileLocation.Length - 4) + ".bak";
                    
                    File.Copy(BatReferenceFileLocation,BackupName,true );
                    File.Delete(BatReferenceFileLocation);
                    BatList.Save(BatReferenceFileLocation);
                }
            }

            this.DialogResult = true;
            this.Close();
        }

        private void CommonNameAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(CommonNameEditBox.Text))
            {

                var selectedBat = SelectedBat();
                var names = selectedBat.Descendants("BatCommonName");
                XElement nullName = null;
                foreach (var name in names)
                {
                    if (String.IsNullOrWhiteSpace(name.Value))
                    {
                        nullName = name;
                    }else if (name.Value == CommonNameEditBox.Text)
                    {
                        return;
                    }
                }
                if (nullName != null) nullName.Remove();
                var batCommonNames = selectedBat.DescendantsAndSelf("BatCommonName");
                if (batCommonNames == null || batCommonNames.Count() <= 0 )
                {
                    selectedBat.Add(new XElement("BatCommonName", CommonNameEditBox.Text));
                }
                else {
                    batCommonNames.LastOrDefault().AddAfterSelf(new XElement("BatCommonName", CommonNameEditBox.Text));
                }
                //selectedBat.Add(new XElement("BatCommonName", CommonNameEditBox.Text));
                ReloadXmlDocument(true);

            }
        }

        /// <summary>
        /// Selecteds the bat. Returns the XElement from BatList for the bat
        /// corresponding to the XmlElement supplied as a parameter.
        /// </summary>
        /// <param name="selectedBat">The selected bat.</param>
        /// <returns></returns>
        private XElement SelectedBat(XmlElement selectedBatElement)
        {
            string batName = selectedBatElement.GetAttribute("Name");

            var selectedBat = (from bat in BatList.Descendants("Bat")
                               where bat.Attribute("Name").Value == batName
                               select bat).FirstOrDefault();
            return (selectedBat);
        }

        /// <summary>
        /// Selecteds the bat. Returns the XElement from the master BatList
        /// corresponding to the bat selected in the BatNameListBox.
        /// </summary>
        /// <returns></returns>
        private XElement SelectedBat()
        {
            XmlElement selectedBatElement = (XmlElement)BatNameListBox.SelectedItem;
            return (SelectedBat(selectedBatElement));
        }

        private void CommonNameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CommonNameListView.SelectedItem != null)
            {
                CommonNameEditBox.Text = ((XmlElement)(CommonNameListView.SelectedItem)).InnerText;
            }
        }

        private void CommonNameDelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(CommonNameEditBox.Text))
            {
                String selectedCommonName = CommonNameEditBox.Text;
                var selectedBat = SelectedBat();
                var selectedElement = from cn in selectedBat.Descendants("BatCommonName")
                                      where cn.Value == selectedCommonName
                                      select cn;
                if(selectedElement!= null && selectedElement.Count()>0)
                {
                    selectedElement.FirstOrDefault().Remove();
                    ReloadXmlDocument(true);
                }

            }
        }

        /// <summary>
        /// Handles the Click event of the CommonNameUpButton control.
        /// Moves the selected item up in the list if it is not already
        /// at the top.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void CommonNameUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(CommonNameEditBox.Text))
            {
                String selectedCommonName = CommonNameEditBox.Text;
                var selectedBat = SelectedBat();
                var commonNames = selectedBat.Descendants("BatCommonName");
                for(int i = 1; i < commonNames.Count(); i++)
                {
                    if (commonNames.ElementAt(i).Value == selectedCommonName)
                    {
                        var prev=commonNames.ElementAt(i).PreviousNode;
                        var current = commonNames.ElementAt(i);
                        commonNames.ElementAt(i).Remove();
                        prev.AddBeforeSelf(current);
                        ReloadXmlDocument(true);
                        break;
                    }
                }
            }
        }

        
        private bool changing = false;
        /// <summary>
        /// Handles the SelectionChanged event of the BatNameListBox control.
        /// Ensures that the old bat was properly filled out - if not it
        /// sets a flag to prevent recursion and resets the selected index
        /// to the old value.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void BatNameListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            ListBox senderListBox = sender as ListBox;
            if (e != null && e.RemovedItems != null && e.RemovedItems.Count > 0)
            {
                var deselected = (XmlElement)e.RemovedItems[0];
                if (!changing && deselected != null)
                {
                    int index= BatNameListBox.Items.IndexOf(deselected);
                    changing = true;
                    String errString = ValidateBat(deselected);
                    if (!String.IsNullOrWhiteSpace(errString))
                    {
                        
                        changing = true;
                        BatNameListBox.SelectedIndex = index;
                        DisplayInvalidErrorMessage(errString);
                        changing = false;
                        return;
                    }
                }
            }


            CommonNameEditBox.Text = "";
            IDTagEditBox.Text = "";
            changing = false;
            
        }

        /// <summary>
        /// Validates the bat.  Checks that CommonName, Genus, Species and Tag
        /// are all present.  If not throws up a message box, and returns FALSE
        /// </summary>
        /// <param name="unselected">The unselected.</param>
        /// <returns></returns>
        private String ValidateBat(XmlElement unselected)
        {
            
            XElement previousBat = SelectedBat(unselected);
            var child = previousBat.Descendants("BatCommonName");
            if (child==null || child.Count()<=0 || String.IsNullOrWhiteSpace(child.First().Value)){
                return("At least one Common Name required");
                
            }
            RenameBat(previousBat);
            child = previousBat.Descendants("BatGenus");
            if (child == null || child.Count() <= 0 || String.IsNullOrWhiteSpace(child.First().Value)){
                return("Bat Genus required");
                
            }
            child = previousBat.Descendants("BatSpecies");
            if (child == null || child.Count() <= 0 || String.IsNullOrWhiteSpace(child.First().Value)){
                return("Bat Species required");
                
            }
            child = previousBat.Descendants("BatTag");
            if (child == null || child.Count() <= 0 || String.IsNullOrWhiteSpace(child.First().Value)){
                return("Bat Tag required");
                
            }

            return (null);

        }

        private void RenameBat(XElement bat)
        {
            try
            {
                string CommonName = bat.Descendants("BatCommonName").First().Value;
                string newName = CommonName.Trim().Replace(" ", "");
                string checkname = newName;
               
                    if (bat.Attribute("Name") == null)
                    {
                        bat.Add(new XAttribute("Name", newName));

                    }
                    else
                    {
                        bat.Attribute("Name").Value = newName;
                    }
                bool OldChanging = changing;
                ReloadXmlDocument(true);
                changing = OldChanging;
                   
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Invalids the bat.  Throws a message box warning of an invalid bat
        /// descriptor using the provided string.
        /// </summary>
        /// <param name="v">The v.</param>
        private void DisplayInvalidErrorMessage(string v)
        {
            bool OldChanging = changing;
            changing = true;
            
            
            MessageBox.Show(v, "All fields must be completed", MessageBoxButton.OK);
            changing = OldChanging;
        }

        /// <summary>
        /// Handles the Click event of the CommonNameDownButton control.
        /// Moves the selected item down in the common names list if it
        /// is not already at the bottom
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void CommonNameDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(CommonNameEditBox.Text))
            {
                String selectedCommonName = CommonNameEditBox.Text;
                var selectedBat = SelectedBat();
                var commonNames = selectedBat.Descendants("BatCommonName");
                for (int i = 0; i < commonNames.Count()-1; i++)
                {
                    if (commonNames.ElementAt(i).Value == selectedCommonName)
                    {
                        var after = commonNames.ElementAt(i).NextNode;
                        var current = commonNames.ElementAt(i);
                        commonNames.ElementAt(i).Remove();
                        after.AddAfterSelf(current);
                        ReloadXmlDocument(true);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the TagDelButton control.
        /// Deletes the selected tag ID
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void TagDelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(IDTagEditBox.Text))
            {
                String selectedTag = IDTagEditBox.Text;
                var selectedBat = SelectedBat();
                var selectedElement = from cn in selectedBat.Descendants("BatTag")
                                      where cn.Value == selectedTag
                                      select cn;
                if (selectedElement != null && selectedElement.Count() > 0)
                {
                    selectedElement.FirstOrDefault().Remove();
                    ReloadXmlDocument(true);
                }

            }
        }

        /// <summary>
        /// Handles the Click event of the TagAddButton control.
        /// Adds the text in the edit box as a new TagID
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void TagAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(IDTagEditBox.Text))
            {

                var selectedBat = SelectedBat(); // returns NULL !!!!!!!!!!!!!!!!!!!!!!!!!
                var tags = selectedBat.Descendants("BatTag");
                XElement nullTag = null;
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        if (String.IsNullOrWhiteSpace(tag.Value))
                        {
                            nullTag = tag;
                        }else if (tag.Value == IDTagEditBox.Text)
                        {
                            return;
                        }
                    }
                }
                if(nullTag!= null)
                {
                    nullTag.Remove();
                }
                var existingTags = selectedBat.DescendantsAndSelf("BatTag");
                if (existingTags == null || existingTags.Count() <= 0)
                {
                    selectedBat.Add(new XElement("BatTag", IDTagEditBox.Text));
                }
                else
                {

                    existingTags.LastOrDefault().AddAfterSelf(new XElement("BatTag", IDTagEditBox.Text));
                }
                //selectedBat.Add(new XElement("BatCommonName", CommonNameEditBox.Text));
                ReloadXmlDocument(true);

            }
        }

        /// <summary>
        /// Handles the Click event of the TagDownButton control.
        /// Moves the selected tag ID down one place in the list if
        /// it is not already at the bottom
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void TagDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(IDTagEditBox.Text))
            {
                String selectedTag = IDTagEditBox.Text;
                var selectedBat = SelectedBat();
                var tags = selectedBat.Descendants("BatTag");
                for (int i = 0; i < tags.Count() - 1; i++)
                {
                    if (tags.ElementAt(i).Value == selectedTag)
                    {
                        var after = tags.ElementAt(i).NextNode;
                        var current = tags.ElementAt(i);
                        tags.ElementAt(i).Remove();
                        after.AddAfterSelf(current);
                        ReloadXmlDocument(true);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the TagUpButton control.
        /// Moves the selected item up one place in the list if it 
        /// not already at the top
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void TagUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(IDTagEditBox.Text))
            {
                String selectedTag = IDTagEditBox.Text;
                var selectedBat = SelectedBat();
                var tags = selectedBat.Descendants("BatTag");
                for (int i = 1; i < tags.Count(); i++)
                {
                    if (tags.ElementAt(i).Value == selectedTag)
                    {
                        var prev = tags.ElementAt(i).PreviousNode;
                        var current = tags.ElementAt(i);
                        tags.ElementAt(i).Remove();
                        prev.AddBeforeSelf(current);
                        ReloadXmlDocument(true);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the BatTagsListView control.
        /// Copies the selected tag text to the edit box.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs" /> instance containing the event data.</param>
        private void BatTagsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BatTagsListView.SelectedItem != null)
            {
                IDTagEditBox.Text = ((XmlElement)(BatTagsListView.SelectedItem)).InnerText;
            }
        }

        /// <summary>
        /// Handles the Click event of the MoveUpRecordButton control.
        /// Moves the selected bat name up one place inthe list unless
        /// it is already at the top.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void MoveUpRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatNameListBox.SelectedIndex>0)
            {
                
                var selectedBat = SelectedBat();
                var bats = BatList.Descendants("Bat");
                for (int i = 1; i < bats.Count(); i++)
                {
                    if (bats.ElementAt(i).Attribute("Name") == selectedBat.Attribute("Name"))
                    {
                        var prev = bats.ElementAt(i).PreviousNode;
                        var current = bats.ElementAt(i);
                        bats.ElementAt(i).Remove();
                        prev.AddBeforeSelf(current);
                        ReloadXmlDocument(true);
                        if (BatNameListBox.SelectedIndex > 0)
                        {
                            BatNameListBox.SelectedIndex--;
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the MoveDownRecordButton control.
        /// Moves the selected bat name down in the list, unless it is already
        /// at the bottom
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void MoveDownRecordButton_Click(object sender, RoutedEventArgs e)
        {
            if (BatNameListBox.SelectedIndex>=0)
            {
                
                var selectedBat = SelectedBat();
                var bats = BatList.Descendants("Bat");
                for (int i = 0; i < bats.Count() - 1; i++)
                {
                    if (bats.ElementAt(i).Attribute("Name") == selectedBat.Attribute("Name"))
                    {
                        var after = bats.ElementAt(i).NextNode;
                        var current = bats.ElementAt(i);
                        bats.ElementAt(i).Remove();
                        after.AddAfterSelf(current);
                        ReloadXmlDocument(true);
                        if (BatNameListBox.SelectedIndex < BatNameListBox.Items.Count - 1)
                        {
                            BatNameListBox.SelectedIndex++;
                        }
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the DelRecordButton control.
        /// Deletes the selected bat from the list of bats.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void DelRecordButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedBat = SelectedBat();
            selectedBat.Remove();
            ReloadXmlDocument(true);
        }

        private void AddRecordButton_Click(object sender, RoutedEventArgs e)
        {
            String errString = ValidateBat((XmlElement)BatNameListBox.SelectedItem);
            if (String.IsNullOrWhiteSpace(errString))
            {
                XElement blankBat = new XElement("Bat",
                    new XElement("BatCommonName"),
                    new XElement("BatGenus"),
                    new XElement("BatSpecies", ""),
                    new XElement("BatTag"));
                blankBat.Add(new XAttribute("Name", "bat"));
                // var x = BatList.Descendants("BatLibrary");
                // var y = x.FirstOrDefault();
                //y.AddFirst(blankBat);
                BatList.Add(blankBat);
                ReloadXmlDocument(true);
                changing = true;
                BatNameListBox.SelectedIndex = BatNameListBox.Items.Count - 1;
            }
            else
            {
                DisplayInvalidErrorMessage(errString);
            }
        }

        /// <summary>
        /// Handles the LostFocus event of the SpeciesTextBlock control.
        /// Forces the bat list to be updated with the contents of the text
        /// box just in case it has been changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void SpeciesTextBlock_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(SpeciesTextBlock.Text))
            {
               
                return;
            }
            var selectedBat = SelectedBat();
            selectedBat.Descendants("BatSpecies").FirstOrDefault().Value = SpeciesTextBlock.Text;
            ReloadXmlDocument(true);
        }

        private void GenusTextBlock_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(GenusTextBlock.Text))
            {

                return;
            }
            var selectedBat = SelectedBat();
            selectedBat.Descendants("BatGenus").FirstOrDefault().Value = GenusTextBlock.Text;
            ReloadXmlDocument(true);
        }

        /// <summary>
        /// Handles the LostFocus event of the CommonNameEditBox control.
        /// If the box is not empty, and the text in the box is not already
        /// in the list of Common Names, then the text is added as a new common name
        /// by calling the 'ADD' button handler automatically.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs" /> instance containing the event data.</param>
        private void CommonNameEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommonNameAddButton_Click(this, new RoutedEventArgs());
        }

        private void IDTagEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TagAddButton_Click(this, new RoutedEventArgs());
        }
    }
}
