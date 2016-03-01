using System.Windows;

namespace BatRecordingManager
{
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static string _dbFileLocation;

        private static string _dbFileName;

        public static string dbFileLocation
        {
            get
            {
                return (_dbFileLocation);
            }
            set
            {
                _dbFileLocation = value;
            }
        }

        public static string dbFileName
        {
            get
            {
                return (_dbFileName);
            }
            set
            {
                _dbFileName = value;
            }
        }
    }
}