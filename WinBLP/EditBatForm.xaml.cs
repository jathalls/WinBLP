using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Linq;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for EditBatForm.xaml
    /// </summary>
    public partial class EditBatForm : Window
    {
        #region newBat

        /// <summary>
        ///     newBat Dependency Property
        /// </summary>
        public static readonly DependencyProperty newBatProperty =
            DependencyProperty.Register("newBat", typeof(Bat), typeof(EditBatForm),
                new FrameworkPropertyMetadata(new Bat()));

        /// <summary>
        ///     Gets or sets the newBat property. This dependency property indicates ....
        /// </summary>
        public Bat newBat
        {
            get
            {
                return (Bat)GetValue(newBatProperty);
            }
            set
            {
                SetValue(newBatProperty, value);
                if (batCallControl != null)
                {
                    var rawCallList = DBAccess.GetCallsForBat(value);
                    if (rawCallList != null)
                    {
                        batCallControl.CallList = new ObservableCollection<Call>(DBAccess.GetCallsForBat(value));
                    }
                    else
                    {
                        batCallControl.CallList = new ObservableCollection<Call>(new List<Call>());
                    }
                    SelectedCallIndex = batCallControl.selectCall(0);
                    if (batCallControl != null && batCallControl.CallList != null && SelectedCallIndex >= 0)
                    {
                        //CallIndexTextBox.Text = SelectedCallIndex.ToString();
                        TotalCallsTextBox.Text = batCallControl.CallList.Count.ToString();
                        NumberOfCallsStackPanel.Visibility = Visibility.Visible;
                        PrevNextButtonBarStackPanel.Visibility = Visibility.Visible;
                        if (batCallControl.CallList.Count() > SelectedCallIndex + 1)
                        {
                            NextCallButton.IsEnabled = true;
                        }
                        PrevCallButton.IsEnabled = false;
                        DeleteCallButton.IsEnabled = true;
                        batCallControl.SetReadOnly(false);
                    }
                    else
                    {
                        NumberOfCallsStackPanel.Visibility = Visibility.Hidden;
                        PrevNextButtonBarStackPanel.Visibility = Visibility.Hidden;
                        DeleteCallButton.IsEnabled = false;
                        batCallControl.SetReadOnly(true);
                    }

                    this.DataContext = newBat;
                }
            }
        }

        #endregion newBat

        private int _SelectedCallIndex;

        /// <summary>
        ///     Initializes a new instance of the <see cref="EditBatForm"/> class.
        /// </summary>
        public EditBatForm()
        {
            newBat = new Bat();
            newBat.Name = "Unknown";
            newBat.Batgenus = "Unknown";
            newBat.BatSpecies = "unknown";
            newBat.Notes = "";
            BatTag bt = new BatTag();
            bt.BatTag1 = "bat";
            bt.BatID = newBat.Id;
            newBat.BatTags.Add(bt);
            InitializeComponent();
            AddNewTagButton.IsEnabled = true;
            DeleteTagButton.IsEnabled = true;
            this.DataContext = newBat;
            //batCallControl.SetReadOnly(false);
        }

        public int SelectedCallIndex
        {
            get
            {
                return (_SelectedCallIndex);
            }
            set
            {
                _SelectedCallIndex = value;
                int maxIndex = batCallControl.CallList.Count() - 1;
                if (value <= 0)
                {
                    PrevCallButton.IsEnabled = false;
                }
                else
                {
                    PrevCallButton.IsEnabled = true;
                }
                if (value >= maxIndex)
                {
                    NextCallButton.IsEnabled = false;
                }
                else
                {
                    NextCallButton.IsEnabled = true;
                }
                CallIndexTextBox.Text = (SelectedCallIndex + 1).ToString();
                TotalCallsTextBox.Text = batCallControl.CallList.Count.ToString();
            }
        }

        private void AddCallButton_Click(object sender, RoutedEventArgs e)
        {
            String buttonLabel = AddCallButton.Content as String;
            if (buttonLabel == "Add")
            {
                SelectedCallIndex = batCallControl.AddNew();
                batCallControl.SetReadOnly(false);
                AddCallButton.Content = "Save";
                DeleteCallButton.Content = "Cancel";
                PrevCallButton.IsEnabled = false;
                NextCallButton.IsEnabled = false;
            }
            else if (buttonLabel == "Save")
            {
                AddCallButton.Content = "Add";
                DeleteCallButton.Content = "Del";
                SelectedCallIndex = batCallControl.selectCall(batCallControl.CallList.Count - 1);
            }
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
            var view = CollectionViewSource.GetDefaultView(BatTagList.ItemsSource);
            if (view != null) view.Refresh();
        }

        private void AddTag(string text)
        {
            if (String.IsNullOrWhiteSpace(text)) return;
            BatTag tag = DBAccess.GetTag(text);
            if (tag != null)
            {
                MessageBox.Show("Tag <" + text + "> is already in use by " + tag.Bat.Name, "Duplicate Tag");
                return;
            }
            if (newBat == null) newBat = new Bat();
            if (newBat.BatTags == null) newBat.BatTags = new EntitySet<BatTag>();
            if (!String.IsNullOrWhiteSpace(text))
            {
                BatTag newTag = new BatTag();
                newTag.BatID = newBat.Id;
                newTag.BatTag1 = text;
                newBat.BatTags.Add(newTag);

                this.DataContext = newBat;

                var view = CollectionViewSource.GetDefaultView(BatTagList.ItemsSource);
                if (view != null) view.Refresh();
            }
        }

        private void DeleteCallButton_Click(object sender, RoutedEventArgs e)
        {
            String buttonLabel = DeleteCallButton.Content as string;
            if (buttonLabel == "Del")
            {
                SelectedCallIndex = batCallControl.DeleteCall(SelectedCallIndex);
                if (SelectedCallIndex <= 0) PrevCallButton.IsEnabled = false;
                if (SelectedCallIndex >= batCallControl.CallList.Count - 1) NextCallButton.IsEnabled = false;
                //CallIndexTextBox.Text = SelectedCallIndex.ToString();
                TotalCallsTextBox.Text = batCallControl.CallList.Count.ToString();
            }
            else if (buttonLabel == "Cancel")
            {
                SelectedCallIndex = batCallControl.DeleteCall(batCallControl.CallList.Count - 1);

                AddCallButton.Content = "Add";
                DeleteCallButton.Content = "Del";
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

            this.DataContext = newBat;
            var view = CollectionViewSource.GetDefaultView(BatTagList.ItemsSource);
            if (view != null) view.Refresh();
        }

        private void EditBatFormOKButton_Click(object sender, RoutedEventArgs e)
        {
            newBat.BatSpecies = BatSpeciesTextBlock.Text;
            newBat.Batgenus = BatGenusTextBlock.Text;
            newBat.Notes = BatNotesTextBlock.Text;
            newBat.Name = CommonNameTextBox.Text;
            string error = DBAccess.ValidateBat(newBat);
            if (String.IsNullOrWhiteSpace(error))
            {
                DBAccess.UpdateBat(newBat, batCallControl.CallList);
                this.DialogResult = true;

                this.Close();
            }
            else
            {
                MessageBox.Show(error);
            }
        }

        private void NextCallButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCallIndex = batCallControl.selectCall(SelectedCallIndex + 1);
            if (SelectedCallIndex <= 0) PrevCallButton.IsEnabled = false;
            if (SelectedCallIndex >= batCallControl.CallList.Count - 1) NextCallButton.IsEnabled = false;
            //CallIndexTextBox.Text = SelectedCallIndex.ToString();
        }

        private void PrevCallButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCallIndex = batCallControl.selectCall(SelectedCallIndex - 1);
            if (SelectedCallIndex <= 0) PrevCallButton.IsEnabled = false;
            if (SelectedCallIndex >= batCallControl.CallList.Count - 1) NextCallButton.IsEnabled = false;
            //CallIndexTextBox.Text = SelectedCallIndex.ToString();
        }

        private void TagEditBox_LostFocus(object sender, RoutedEventArgs e)
        {
        }
    }
}