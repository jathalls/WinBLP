using System;
using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for NewTagForm.xaml
    /// </summary>
    public partial class NewTagForm : Window
    {
        public String TagText;

        public NewTagForm()
        {
            InitializeComponent();
            TagText = "";
            TagTextBox.Focus();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(TagTextBox.Text))
            {
                TagText = TagTextBox.Text;
                this.DialogResult = true;
                this.Close();
            }
        }
    }
}