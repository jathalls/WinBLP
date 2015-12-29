using System.Windows;
using System.Windows.Controls;

namespace BatRecordingManager
{
    /// <summary>
    /// Interaction logic for ButtonBarControl.xaml
    /// </summary>
    public partial class ButtonBarControl : UserControl
    {
        public ButtonBarControl()
        {
            InitializeComponent();
        }

        public Button AddCustomButton(string label)
        {
            Button newButton = new Button();
            newButton.Style = (Style)FindResource("ButtonStyle");
            newButton.Content = label;
            ButtonPanel.Children.Add(newButton);
            return (newButton);
        }
    }
}