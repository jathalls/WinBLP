using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace WinBLP
{
    /// <summary>
    /// BatSummary represents a single bat species and accumulates
    /// data about passes that included that species.
    /// It is initialised with appropriate names and tags from
    /// an external 'BatData' file which is associated with the
    /// program and can be edited in atext editor.
    /// </summary>
    internal class BatSummary
    {
        /// <summary>
        /// The xe bat library
        /// </summary>
        private XElement xeBatLibrary = null;
        private String FileLocation = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatSummary"/> class.
        /// </summary>
        public BatSummary()
        {
            try
            {
                XElement newXMLData = null;
                XElement editableXMLData = null;

                string newReferenceFileLocation = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Echolocation\WinBLP\BatReferenceXMLFile.xml");

                string editableReferenceFileLocation = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Echolocation\WinBLP\EditableBatReferenceXMLFile.xml");

                string targetFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Echolocation\WinBLP\");
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                if (!File.Exists(editableReferenceFileLocation) && !File.Exists(editableReferenceFileLocation))
                {
                    // virgin system so copy the source XML file if possible

                    if (File.Exists(@".\BatReferenceXMLFile.xml"))
                    {
                        File.Copy(@".\BatReferenceXMLFile.xml", newReferenceFileLocation);
                    }
                }

                if (File.Exists(newReferenceFileLocation))
                {
                    newXMLData = XElement.Load(newReferenceFileLocation);
                }

                string backupFile = editableReferenceFileLocation + ".bak";
                if (File.Exists(editableReferenceFileLocation) && File.Exists(backupFile))
                {
                    File.Delete(backupFile);
                    File.Copy(editableReferenceFileLocation, backupFile);
                }

                if (File.Exists(editableReferenceFileLocation))
                {// we have an existing, possibly edited version of the XML file
                    editableXMLData = XElement.Load(editableReferenceFileLocation);
                    xeBatLibrary = editableXMLData;
                    if (newXMLData != null)
                    {
                        xeBatLibrary = MergeXML(editableXMLData, newXMLData);
                        xeBatLibrary.Save(editableReferenceFileLocation);
                    }
                }
                else
                {// no editableXML file exists, so use the new data and save as
                    //   as new editableXML file
                    xeBatLibrary = newXMLData;
                    if (xeBatLibrary != null)
                    {
                        xeBatLibrary.Save(editableReferenceFileLocation);
                    }
                }

                // finally get rid of the update file if it is there
                if (File.Exists(newReferenceFileLocation))
                {
                    File.Delete(newReferenceFileLocation);
                }

                FileLocation = editableReferenceFileLocation;

                if (xeBatLibrary != null)
                {
                    var tags =
                        from bat in xeBatLibrary.Descendants("BatTag")
                        select (bat.Value);
                    Console.WriteLine(editableReferenceFileLocation + " Linqed ");
                    foreach (var tag in tags)
                    {
                        Console.WriteLine(tag.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// merges two BatLibrary XML files.  All data in the editable version is to be
        /// retained, but new data from the nex version is added.  Existing data in the
        /// editable version may be overwritten by equivalent data in the new version.
        /// </summary>
        /// <param name="editableXMLData">The editable XML data.</param>
        /// <param name="newXMLData">The new XML data.</param>
        /// <returns></returns>
        private XElement MergeXML(XElement editableXMLData, XElement newXMLData)
        {
            XElement XResult = new XElement("BatLibrary");
            foreach (var bat in editableXMLData.Descendants("Bat"))
            {
                var newBat = (from nb in newXMLData.Descendants("Bat")
                              where nb.Attribute("Name").Value == bat.Attribute("Name").Value
                              select nb);
                if (newBat == null || newBat.Count() <= 0)
                {
                    XResult.Add(bat);
                }
                else
                {
                    XResult.Add(MergeBats(bat, newBat.FirstOrDefault()));
                }
            }

            foreach (var bat in newXMLData.Descendants("Bat"))
            {
                var editableBat = (from eb in editableXMLData.Descendants("Bat")
                                   where eb.Attribute("Name").Value == bat.Attribute("Name").Value
                                   select eb);
                if (editableBat == null || editableBat.Count() <= 0) // no matching bats, so add it
                {
                    XResult.Add(bat);
                }
            }
            return (XResult);
        }

        /// <summary>
        /// Given two XElements of type "Bat", creates a new XElement witht he same
        /// Name, genus, Species, Common Name and all unique BatTags.
        /// If Names are taken from the second supplied parameter
        /// </summary>
        /// <param name="bat">The bat.</param>
        /// <param name="newBat">The new bat.</param>
        /// <returns></returns>
        private XElement MergeBats(XElement bat, XElement newBat)
        {
            XElement XResult = new XElement("Bat");
            XResult.Add(new XAttribute("Name", newBat.Attribute("Name").Value));
            XResult.Add(newBat.Descendants("BatGenus").FirstOrDefault());
            XResult.Add(newBat.Descendants("BatSpecies").FirstOrDefault());
            XResult.Add(newBat.Descendants("BatCommonName").FirstOrDefault());
            var alltags = (newBat.Descendants("BatTag").Concat(bat.Descendants("BatTag")));
            Debug.WriteLine(alltags.Count());

            var tags = (from t in alltags select t).GroupBy(x => x.Value).Select(x => x.First());

            /*tags = tags.Distinct<XElement>(XElement.DeepEquals);
            Debug.WriteLine(tags.Count());*/

            /*var tags = newBat.Descendants("BatTag").ToList();
            foreach (var tag in bat.Descendants("BatTag"))
            {
                if (!tags.Contains(tag))
                {
                    tags.Add(tag);
                }
            }*/
            Debug.WriteLine(tags.Count());
            foreach (var tag in tags)
            {
                XResult.Add(tag);
            }

            return (XResult);
        }

        /// <summary>
        /// Given the comment portion relating to a bat pass, searches the
        /// string for a recognizable BatTag and returns the details of
        /// the corresponding Bat as an XElement extracted from the BatLibrary
        /// </summary>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        public List<XElement> getBatElement(String description)
        {
            List<XElement> matchingBats = new List<XElement>();

            if (String.IsNullOrWhiteSpace(description)) return (null);
            if (xeBatLibrary == null) return (null);

            foreach (XElement bat in xeBatLibrary.Descendants("Bat"))
            {
                if (TagMatch(bat, description))
                {
                    matchingBats.Add(bat);
                }
            }
            return (matchingBats);
        }

        /// <summary>
        /// given a "Bat" XElement and a description string, checks to see
        /// if any of the BatTags in Bat are contained in the string.
        /// </summary>
        /// <param name="bat">The bat.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        private bool TagMatch(XElement bat, string description)
        {
            var tags = from b in bat.Descendants("BatTag")
                       select (b.Value);
            if (tags == null) return (false);
            foreach (var tag in tags)
            {
                if (!tag.ToUpper().StartsWith(tag))
                {
                    if (description.ToUpper().Contains(tag.ToUpper()))
                    {
                        return (true);
                    }
                }
                else // tag provided is all upper case, so description must be upper case too
                {
                    if (description.Contains(tag))
                    {
                        return (true);
                    }
                }
            }
            return (false);
        }

        public XElement getBatList()
        {
            return (xeBatLibrary);
        }

        internal string getFileLocation()
        {
            return (FileLocation);
        }

        /// <summary>
        /// Refreshes the bat list.
        /// Reloads the XML bat reference data from the known file location
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        internal void RefreshBatList()
        {
            if (!String.IsNullOrWhiteSpace(FileLocation))
            {
                if (File.Exists(FileLocation))
                {
                    xeBatLibrary = XElement.Load(FileLocation);
                }
            }
        }
    }

    
}