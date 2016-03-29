using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Maps.MapControl.WPF;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for MapWindow.xaml
    /// </summary>
    public partial class MapWindow : Window
    {
        private bool isDialog = false;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MapWindow"/> class. The parameter is
        ///     set true if the window is to be displayed using ShowDialog rather than Show so that
        ///     the DialogResult can be set before closing.
        /// </summary>
        /// <param name="isDialog">
        ///     if set to <c>true</c> [is dialog].
        /// </param>
        public MapWindow(bool isDialog)
        {
            this.isDialog = isDialog;
            InitializeComponent();
            mapControl.OKButton.Click += OKButton_Click;
        }

        /// <summary>
        ///     Gets or sets the coordinates of the centre of the map window
        /// </summary>
        /// <value>
        ///     The coordinates.
        /// </value>
        public Location Coordinates
        {
            get
            {
                return (mapControl.coordinates);
            }
            set
            {
                mapControl.coordinates = value;
            }
        }

        public Location lastSelectedLocation
        {
            get
            {
                return (mapControl.lastInsertedPinLocation);
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (isDialog)
            {
                this.DialogResult = true;
            }
            this.Close();
        }
    }
}