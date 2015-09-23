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
using System.Collections.ObjectModel;

namespace WinBLPdB
{
    /// <summary>
    /// Interaction logic for BatEditor.xaml
    /// </summary>
    public partial class BatEditor : Window
    {
        public BatReferenceDBLinqDataContext batReferenceDataContext;




        public ObservableCollection<Bat> BatList
        {
            get { return (ObservableCollection<Bat>)GetValue(BatListProperty); }
            set { SetValue(BatListProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BatList.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BatListProperty =
            DependencyProperty.Register("BatList", typeof(ObservableCollection<Bat>), typeof(BatEditor), new PropertyMetadata(new ObservableCollection<Bat>()));

    

        //public ObservableCollection<Bat> BatList;
        


        public BatEditor(BatReferenceDBLinqDataContext BatReferenceDataContext)
        {
            batReferenceDataContext = BatReferenceDataContext;

            BatList = new ObservableCollection<Bat>();
            
            

            InitializeComponent();
            this.DataContext = this;

            LoadDataToBatList();
            BatNameListBox.ItemsSource = BatList;


        }

        private void LoadDataToBatList()
        {
            int lastSelectedIndex = BatNameListBox.SelectedIndex;
            BatList.Clear();
            var bats = from bat in batReferenceDataContext.Bats
                       orderby bat.SortIndex
                       select bat;
            short i = 1;
            foreach (var bat in bats)
            {
                bat.SortIndex = i++;
            }
            batReferenceDataContext.SubmitChanges();
            BatList = new ObservableCollection<Bat>(bats.ToList<Bat>());
        }

        private void RefreshBatNameListBox(bool noNewSelection)
        {
            if (noNewSelection)
            {
                changing = true;
            }
            int lastSelectedIndex = BatNameListBox.SelectedIndex;
            
            var bats = from bat in BatList
                       orderby bat.SortIndex
                       select bat;
            short i = 1;
            foreach(var bat in bats)
            {
                bat.SortIndex = i++;
            }
            

            BatList = new ObservableCollection<Bat>(bats.ToList<Bat>());
            BatNameListBox.ItemsSource = BatList;
            BatNameListBox.SelectedIndex = lastSelectedIndex;
            
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

                CommonNameListView.ItemsSource = (BatNameListBox.SelectedItem as Bat).BatCommonNames;
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
                String errstring = ValidateBat((Bat)BatNameListBox.SelectedItem);
                if (!String.IsNullOrWhiteSpace(errstring))
                {
                    DisplayInvalidErrorMessage(errstring);
                    return;
                }

                

            }

            // OK, so the bat selected is valid, others are all untouched or checked
            // when they were deselected
            batReferenceDataContext.SubmitChanges();

            this.DialogResult = true;
            this.Close();
        }

        private void CommonNameAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(CommonNameEditBox.Text))
            {
                var bat = BatList[BatNameListBox.SelectedIndex] as Bat;
                var name = from n in bat.BatCommonNames
                           where n.BatCommonName1.ToUpper() == CommonNameEditBox.Text.ToUpper()
                           select n;
                if(name!=null && name.Count() > 0)
                {
                    return;// name in the edit box already exists for this bat
                }
                else
                {
                    BatCommonName bcn = new BatCommonName();
                    bcn.BatCommonName1 = CommonNameEditBox.Text;
                    bcn.BatID = bat.Id;
                    bat.BatCommonNames.Add(bcn);
                    
                    RefreshBatNameListBox(true);
                }

                

            }
        }

        /// <summary>
        /// Selecteds the bat. Returns the XElement from BatList for the bat
        /// corresponding to the XmlElement supplied as a parameter.
        /// </summary>
        /// <param name="selectedBat">The selected bat.</param>
        /// <returns></returns>
        private Bat SelectedBat(Bat selectedBatElement)
        {
            
            /*string batName = selectedBatElement.GetAttribute("Name");

            var selectedBat = (from bat in BatList.Descendants("Bat")
                               where bat.Attribute("Name").Value == batName
                               select bat).FirstOrDefault();*/
            return (selectedBatElement);
        }

        /// <summary>
        /// Selecteds the bat. Returns the XElement from the master BatList
        /// corresponding to the bat selected in the BatNameListBox.
        /// </summary>
        /// <returns></returns>
        private Bat SelectedBat()
        {
            Bat selectedBatElement = (Bat)BatNameListBox.SelectedItem;
            return (SelectedBat(selectedBatElement));
        }

        private void CommonNameListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CommonNameListView.SelectedItem != null)
            {
                CommonNameEditBox.Text = (CommonNameListView.SelectedItem as BatCommonName).BatCommonName1;
            }
        }

        private void CommonNameDelButton_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(CommonNameEditBox.Text))
            {
                String selectedCommonName = CommonNameEditBox.Text;
                var selectedBat = BatList[BatNameListBox.SelectedIndex] as Bat;
                var selectedElement = (from cn in selectedBat.BatCommonNames
                                      where cn.BatCommonName1 == selectedCommonName
                                      select cn);
                if(selectedElement!= null && selectedElement.Count()>0)
                {
                    selectedBat.BatCommonNames.Remove(selectedElement.First());
                    RefreshBatNameListBox(true);
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
                
                var commonNames = (from cn in batReferenceDataContext.BatCommonNames
                                   where cn.BatID == selectedBat.Id
                                   orderby cn.SortIndex
                                   select cn);
                for (int i = 0; i < commonNames.Count(); i++)
                {
                    commonNames.ElementAt(i).SortIndex = (short)i;
                }
                for(int i = 1; i < commonNames.Count(); i++)
                { 
                    if (commonNames.ElementAt(i).BatCommonName1 == selectedCommonName)
                    {
                        commonNames.ElementAt(i).SortIndex = (short)(i - 1);
                        commonNames.ElementAt(i - 1).SortIndex = (short)i;
                    }
                }
                RefreshBatNameListBox(true);
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
            if (changing) return;
            changing = true;
            ListBox senderListBox = sender as ListBox;
            if (e != null && e.RemovedItems != null && e.RemovedItems.Count > 0)
            {
                var deselected = (Bat)e.RemovedItems[0]; //is the item we are moving away from
                if (deselected != null)
                {
                    int index= BatNameListBox.Items.IndexOf(deselected);
                    
                    String errString = ValidateBat(deselected);
                    if (!String.IsNullOrWhiteSpace(errString))
                    {
                        
                        
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
        private String ValidateBat(Bat unselected)
        {
            
            Bat previousBat = SelectedBat(unselected);
            
            int NumberOfCommonNames = previousBat.BatCommonNames.Count;
            if (NumberOfCommonNames<=0){
                return("At least one Common Name required");
                
            }
            RenameBat(previousBat);
            
            if (String.IsNullOrWhiteSpace(previousBat.Batgenus)){
                return("Bat Genus required");
                
            }
            
            if (String.IsNullOrWhiteSpace(previousBat.BatSpecies)){
                return("Bat Species required");
                
            }
            
            if (previousBat.BatTags.Count<=0){
                return("Bat Tag required");
                
            }

            return (null);

        }

        private void RenameBat(Bat bat)
        {
            try
            {
                string CommonName = (from b in bat.BatCommonNames
                                     orderby b.SortIndex
                                     select b).First().BatCommonName1;

                string newName = CommonName.Trim().Replace(" ", "");
                

                if (String.IsNullOrWhiteSpace(bat.Name))
                {
                    bat.Name = newName;
                }
                batReferenceDataContext.SubmitChanges();
               
                    
                bool OldChanging = changing;
                RefreshBatNameListBox(true);
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
                var commonNames = from cn in selectedBat.BatCommonNames
                                  orderby cn.SortIndex
                                  select cn;

                for(int i = 0; i < commonNames.Count(); i++)
                {
                    commonNames.ElementAt(i).SortIndex = (short)i;
                }
                for(int i = 0; i < commonNames.Count() - 1; i++)
                {
                    if (commonNames.ElementAt(i).BatCommonName1 == selectedCommonName)
                    {
                        commonNames.ElementAt(i).SortIndex = (short)(i + 1);
                        commonNames.ElementAt(i + 1).SortIndex = (short)i;
                        break;
                    }
                }
                batReferenceDataContext.SubmitChanges();
                RefreshBatNameListBox(true);
                /*
                var commonNames = selectedBat.Descendants("BatCommonName");
                for (int i = 0; i < commonNames.Count()-1; i++)
                {
                    if (commonNames.ElementAt(i).Value == selectedCommonName)
                    {
                        var after = commonNames.ElementAt(i).NextNode;
                        var current = commonNames.ElementAt(i);
                        commonNames.ElementAt(i).Remove();
                        after.AddAfterSelf(current);
                        RefreshBatNameListBox(true);
                        break;
                    }
                }*/
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

                var selTag = (from tg in selectedBat.BatTags
                             where tg.BatTag1 == selectedTag
                             select tg).First();
                batReferenceDataContext.BatTags.DeleteOnSubmit(selTag);
                batReferenceDataContext.SubmitChanges();
                RefreshBatNameListBox(true);

                /*
                var selectedElement = from cn in selectedBat.Descendants("BatTag")
                                      where cn.Value == selectedTag
                                      select cn;
                if (selectedElement != null && selectedElement.Count() > 0)
                {
                    selectedElement.FirstOrDefault().Remove();
                    RefreshBatNameListBox(true);
                }*/

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
                var tags = from tg in selectedBat.BatTags
                           orderby tg.SortIndex
                           select tg;

                
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        if (tag.BatTag1 == IDTagEditBox.Text)
                        {
                            return;// tag already exists so we can't add it again
                        }
                    }

                    foreach(var tag in tags)
                    {
                        tag.SortIndex++;
                    }

                    BatTag newTag = new BatTag();
                    newTag.BatTag1 = IDTagEditBox.Text;
                    newTag.SortIndex = 0;
                    selectedBat.BatTags.Add(newTag);
                    batReferenceDataContext.SubmitChanges();
                }


                /*
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
                }*/
                //selectedBat.Add(new XElement("BatCommonName", CommonNameEditBox.Text));
                RefreshBatNameListBox(true);

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

                var tags = from tg in selectedBat.BatTags
                           orderby tg.SortIndex
                           select tg;

                if(tags!=null && tags.Count() > 1)
                {
                    for(int i = 0; i < tags.Count(); i++)
                    {
                        tags.ElementAt(i).SortIndex = (short)i;
                    }

                    for(int i = 0; i < tags.Count() - 1; i++)
                    {
                        if (tags.ElementAt(i).BatTag1 == IDTagEditBox.Text)
                        {
                            tags.ElementAt(i).SortIndex = (short)(i + 1);
                            tags.ElementAt(i + 1).SortIndex = (short)i;
                            break;
                        }
                    }
                    RefreshBatNameListBox(true);
                }

                /*
                var tags = selectedBat.Descendants("BatTag");
                for (int i = 0; i < tags.Count() - 1; i++)
                {
                    if (tags.ElementAt(i).Value == selectedTag)
                    {
                        var after = tags.ElementAt(i).NextNode;
                        var current = tags.ElementAt(i);
                        tags.ElementAt(i).Remove();
                        after.AddAfterSelf(current);
                        RefreshBatNameListBox(true);
                        break;
                    }
                }*/
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

                var tags = from tg in selectedBat.BatTags
                           orderby tg.SortIndex
                           select tg;

                if (tags != null && tags.Count() > 1)
                {
                    for (int i = 0; i < tags.Count(); i++)
                    {
                        tags.ElementAt(i).SortIndex = (short)i;
                    }

                    for (int i = 1; i < tags.Count() ; i++)
                    {
                        if (tags.ElementAt(i).BatTag1 == IDTagEditBox.Text)
                        {
                            tags.ElementAt(i).SortIndex = (short)(i -1);
                            tags.ElementAt(i - 1).SortIndex = (short)i;
                            break;
                        }
                    }
                    RefreshBatNameListBox(true);
                }

                /*
                var tags = selectedBat.Descendants("BatTag");
                for (int i = 1; i < tags.Count(); i++)
                {
                    if (tags.ElementAt(i).Value == selectedTag)
                    {
                        var prev = tags.ElementAt(i).PreviousNode;
                        var current = tags.ElementAt(i);
                        tags.ElementAt(i).Remove();
                        prev.AddBeforeSelf(current);
                        RefreshBatNameListBox(true);
                        break;
                    }
                }*/
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

            if (BatNameListBox.SelectedIndex > 0)
            {



                //var selectedBat = SelectedBat();

                int index = BatNameListBox.SelectedIndex;
                          


                BatList.ElementAt(index).SortIndex--;
                BatList.ElementAt(index - 1).SortIndex++;
                BatList= new ObservableCollection<Bat>(BatList.OrderBy(bat => bat.SortIndex));

                BatNameListBox.ItemsSource = BatList;
                BatNameListBox.SelectedIndex = index;

          /*      BindingExpression be = BindingOperations.GetBindingExpression(this, BatList);
                if (be != null) be.UpdateSource();*/
                

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


                int index = BatNameListBox.SelectedIndex;
                BatList.ElementAt(index).SortIndex++;
                BatList.ElementAt(index + 1).SortIndex--;
                BatList = new ObservableCollection<Bat>(BatList.OrderBy(bat => bat.SortIndex));
                BatNameListBox.ItemsSource = BatList;
                BatNameListBox.SelectedIndex = index;
                return;

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
            int index = BatNameListBox.SelectedIndex;
            
            BatList.Remove(selectedBat);
            BatList = new ObservableCollection<Bat>(BatList.OrderBy(bat => bat.SortIndex));
            BatNameListBox.ItemsSource = BatList;
            BatNameListBox.SelectedIndex = index;
        }

        private void AddRecordButton_Click(object sender, RoutedEventArgs e)
        {
            String errString = ValidateBat((Bat)BatNameListBox.SelectedItem);
            int index = BatNameListBox.SelectedIndex;
            if (String.IsNullOrWhiteSpace(errString))
            {
                Bat bat = new Bat();
                bat.Name = "bat";
                bat.Batgenus = "BatGenus";
                bat.BatSpecies = "BatSpecies";
                BatCommonName bcn = new BatCommonName();
                bcn.SortIndex = (short)-1;
                bcn.BatCommonName1 = "BatCommonName";
                
                short max = short.MinValue;
                foreach(var cn in bat.BatCommonNames)
                {
                    if (cn.SortIndex > max) max = cn.SortIndex;
                }
                bcn.SortIndex = ++max;
                bat.BatCommonNames.Add(bcn);
                BatTag bt = new BatTag();
                bt.BatTag1 = "BatTag";
                max = short.MinValue;
                foreach(var tg in bat.BatTags)
                {
                    if (tg.SortIndex.Value > max) max = tg.SortIndex.Value;
                }
                bt.SortIndex = ++max;
                
                bat.BatTags.Add(bt);

                int? maxi = int.MaxValue;
                foreach(var b in BatList)
                {
                    if (b.SortIndex > maxi) maxi = b.SortIndex;
                }
                bat.SortIndex = maxi.Value + 1;
                BatList.Add(bat);
                BatList = new ObservableCollection<Bat>(BatList.OrderBy(newbat => newbat.SortIndex));
                BatNameListBox.ItemsSource = BatList;
                BatNameListBox.SelectedItem = bat;

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
            selectedBat.BatSpecies = SpeciesTextBlock.Text;
            batReferenceDataContext.SubmitChanges();
            //selectedBat.Descendants("BatSpecies").FirstOrDefault().Value = SpeciesTextBlock.Text;
            RefreshBatNameListBox(true);
        }

        private void GenusTextBlock_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(GenusTextBlock.Text))
            {

                return;
            }
            var selectedBat = SelectedBat();
            selectedBat.Batgenus = GenusTextBlock.Text;
            batReferenceDataContext.SubmitChanges();
            //selectedBat.Descendants("BatGenus").FirstOrDefault().Value = GenusTextBlock.Text;
            RefreshBatNameListBox(true);
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
