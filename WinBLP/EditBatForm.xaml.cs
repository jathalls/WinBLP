using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for EditBatForm.xaml
    /// </summary>
    public partial class EditBatForm : Window
    {
        #region newBat

        /// <summary>
        /// newBat Dependency Property
        /// </summary>
        public static readonly DependencyProperty newBatProperty =
            DependencyProperty.Register("newBat", typeof(Bat), typeof(EditBatForm),
                new FrameworkPropertyMetadata(new Bat()));

        /// <summary>
        /// Gets or sets the newBat property.  This dependency property
        /// indicates ....
        /// </summary>
        public Bat newBat
        {
            get { return (Bat)GetValue(newBatProperty); }
            set
            {
                SetValue(newBatProperty, value);
                this.DataContext = newBat;
                
            }
        }



        #endregion newBat




        /// <summary>
        /// Initializes a new instance of the <see cref="EditBatForm"/> class.
        /// </summary>
        public EditBatForm()
        {
            newBat = new Bat();
            newBat.Name = "Unknown";
            newBat.Batgenus = "Unknown";
            newBat.BatSpecies = "unknown";
            BatTag bt = new BatTag();
            bt.BatTag1 = "bat";
            bt.BatID = newBat.Id;
            newBat.BatTags.Add(bt);
            InitializeComponent();
            AddNewTagButton.IsEnabled = true;
            DeleteTagButton.IsEnabled = true;
            this.DataContext = newBat;
        }

        

        private void AddNewTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (newBat == null) newBat = new Bat();
            if (newBat.BatTags == null) newBat.BatTags = new EntitySet<BatTag>();
            if (String.IsNullOrWhiteSpace(TagEditBox.Text))
            {
                return;
            }
            //TextBox senderTextBox = sender as TextBox;
            if (newBat.BatTags != null && newBat.BatTags.Count > 0)
            {
                var matchingTags = from tg in newBat.BatTags
                                   where tg.BatTag1 == TagEditBox.Text
                                   select tg;
                if (matchingTags != null && matchingTags.Count() > 0)
                {
                    return;// tag in the edit box is already in the tag list
                }
                else
                {
                    AddTag(TagEditBox.Text);
                }
            }
            else
            {
                AddTag(TagEditBox.Text);
            }
            this.InvalidateArrange();
            this.UpdateLayout();
        }

        private void AddTag(string text)
        {
            if (newBat == null) newBat = new Bat();
            if (newBat.BatTags == null) newBat.BatTags = new EntitySet<BatTag>();
            if (!String.IsNullOrWhiteSpace(text))
            {
                Bat bat = newBat;
                BatTag newTag = new BatTag();
                newTag.BatID = newBat.Id;
                newTag.BatTag1 = text;
                bat.BatTags.Add(newTag);

                newBat = null;
                newBat = bat;
                BatTagList.ItemsSource = newBat.BatTags;
            }
        }

        private void DeleteTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (newBat == null) newBat = new Bat();
            if (newBat.BatTags == null) newBat.BatTags = new EntitySet<BatTag>();
            if (BatTagList.SelectedItem != null)
            {
                BatTag selectedTag = BatTagList.SelectedItem as BatTag;
                if (newBat.BatTags.Contains(selectedTag))
                {
                    newBat.BatTags.Remove(selectedTag);
                }
            }
            BatTagList.ItemsSource = newBat.BatTags;
        }

        private void EditBatFormOKButton_Click(object sender, RoutedEventArgs e)
        {
            newBat.BatSpecies = BatSpeciesTextBlock.Text;
            newBat.Batgenus = BatGenusTextBlock.Text;
            newBat.Notes = BatNotesTextBlock.Text;
            string error = DBAccess.ValidateBat(newBat);
            if (String.IsNullOrWhiteSpace(error))
            {
                this.DialogResult = true;

                this.Close();
            }
            else
            {
                MessageBox.Show(error);
            }
        }

        private void TagEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
        }
        /*
        private void CommonNameTextBlock_LostFocus(object sender, RoutedEventArgs e)
        {
            String newCN = CommonNameTextBlock.Text;
            if (!String.IsNullOrWhiteSpace(newCN))
            {
                if (newBat.BatCommonNames.Count() > 0)
                {
                    var matchedCount = (from cn in newBat.BatCommonNames
                                        where cn.BatCommonName1 == newCN
                                        select cn).Count();
                    if (matchedCount <= 0)
                    {
                        BatCommonName newBatCN = new BatCommonName();
                        newBatCN.BatCommonName1 = newCN;
                        newBatCN.BatID = newBat.Id;
                        newBat.BatCommonNames.Insert(0, newBatCN);
                        InvalidateArrange();
                    }
                }
            }
        }*/
    }

    
}