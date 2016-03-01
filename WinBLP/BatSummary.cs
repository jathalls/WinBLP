using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BatRecordingManager
{
    /// <summary>
    ///     BatSummary represents a single bat species and accumulates data about passes that
    ///     included that species. It is initialised with appropriate names and tags from an
    ///     external 'BatData' file which is associated with the program and can be edited in atext editor.
    /// </summary>
    internal class BatSummary
    {
        /// <summary>
        ///     The xe bat library
        /// </summary>
        //private XElement xeBatLibrary = null;

        //private String FileLocation = null;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BatSummary"/> class.
        /// </summary>
        public BatSummary()
        {
            DBAccess.InitializeDatabase();
        }

        /// <summary>
        ///     Given the comment portion relating to a bat pass, searches the string for a
        ///     recognizable BatTag and returns the details of the corresponding Bat as an XElement
        ///     extracted from the BatLibrary
        /// </summary>
        /// <param name="description">
        ///     The description.
        /// </param>
        /// <returns>
        ///     </returns>
        public ObservableCollection<Bat> getBatElement(String description)
        {
            ObservableCollection<Bat> matchingBats = new ObservableCollection<Bat>();

            matchingBats = DBAccess.GetDescribedBats(description);

            return (matchingBats);
        }

        /*
        internal string getFileLocation()
        {
            return (FileLocation);
        }*/
    }
}