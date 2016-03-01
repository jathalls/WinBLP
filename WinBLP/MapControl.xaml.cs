using Microsoft.Maps.MapControl.WPF;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for MapControl.xaml
    /// </summary>
    public partial class MapControl : UserControl
    {
        private Tuple<decimal, decimal> _coordinates;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MapControl"/> class.
        /// </summary>
        public MapControl()
        {
            InitializeComponent();
            mapControl.Focus();
        }

        /// <summary>
        ///     Gets or sets the coordinates.
        /// </summary>
        /// <value>
        ///     The coordinates.
        /// </value>
        public Tuple<decimal, decimal> coordinates
        {
            get
            {
                return (_coordinates);
            }
            set
            {
                _coordinates = value;
                mapControl.Center = new Microsoft.Maps.MapControl.WPF.Location((double)value.Item1, (double)value.Item2);
            }
        }

        /// <summary>
        ///     Adds the push pin.
        /// </summary>
        /// <param name="PinCoordinates">
        ///     The pin coordinates.
        /// </param>
        /// <param name="text">
        ///     The text.
        /// </param>
        public void AddPushPin(Tuple<double, double> PinCoordinates, String text)
        {
            Pushpin pin = new Pushpin();
            pin.Location = new Location(PinCoordinates.Item1, PinCoordinates.Item2);
            pin.Content = text;
            mapControl.Children.Add(pin);
        }
    }
}