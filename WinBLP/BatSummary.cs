using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace WinBLPdB
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
        //private XElement xeBatLibrary = null;
        public BatReferenceDBLinqDataContext batReferenceDataContext = null;
        private String FileLocation = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatSummary"/> class.
        /// </summary>
        public BatSummary()
        {
            try
            {
                
                string DBFileName = "BatReferenceDB.mdf";
                string workingDatabaseLocation= Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Echolocation\WinBLP\");
                if (!Directory.Exists(workingDatabaseLocation))
                {
                    Directory.CreateDirectory(workingDatabaseLocation);
                }

                //One off routine for transition from XML to DB

                batReferenceDataContext = new BatReferenceDBLinqDataContext(@"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=" + workingDatabaseLocation + DBFileName + @";Integrated Security=False;Connect Timeout=30");
                if (batReferenceDataContext == null) return;

                if (!batReferenceDataContext.DatabaseExists())
                {
                    batReferenceDataContext.CreateDatabase();
                }

 

                if (File.Exists(workingDatabaseLocation+"EditableBatReferenceXMLFile.xml"))
                {
                    copyXMLDataToDatabase(workingDatabaseLocation + "EditableBatReferenceXMLFile.xml", batReferenceDataContext);
                    if(File.Exists(workingDatabaseLocation + "EditableBatReferenceXMLFile.xml.bak"))
                    {
                        File.Delete(workingDatabaseLocation + "EditableBatReferenceXMLFile.xml.bak");
                    }
                    File.Move(workingDatabaseLocation + "EditableBatReferenceXMLFile.xml", workingDatabaseLocation + "EditableBatReferenceXMLFile.xml.bak");
                }

                



            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        /// <summary>
        /// Copies the XML data to database.
        /// </summary>
        /// <param name="xmlFile">The the XML data source file</param>
        /// <param name="batReferenceDataContext">The bat reference data context.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void copyXMLDataToDatabase(string xmlFile, BatReferenceDBLinqDataContext batReferenceDataContext)
        {
            var xmlBats = XElement.Load(xmlFile).Descendants("Bat");
            short i = 0;
            foreach(XElement bat in xmlBats)
            {
                MergeBatToDB(bat, batReferenceDataContext,i++);
            }
        }

        /// <summary>
        /// Merges the bat to database.
        /// </summary>
        /// <param name="bat">The bat.</param>
        /// <param name="batReferenceDataContext">The bat reference data context.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void MergeBatToDB(XElement bat, BatReferenceDBLinqDataContext batReferenceDataContext,short i)
        {
            try
            {
                Bat newBat = null;
                if (batReferenceDataContext == null) return;
                if (bat == null) return;
                bool isNew = false;
                if (batReferenceDataContext.Bats.Count() <= 0)
                {
                    isNew = true;
                    newBat = new Bat();
                    newBat.Id = -1;
                }
                else
                {
                    var newBats = (from dBbat in batReferenceDataContext.Bats
                                   where dBbat.Name == bat.Attribute("Name").Value
                                   select dBbat);
                    if (newBats != null && newBats.Count() > 0)
                    {
                        newBat = newBats.FirstOrDefault();
                    }
                }
                if (newBat == null || newBat.Id < 0)
                {
                    newBat = new Bat();
                    newBat.Id = -1;
                    isNew = true;
                }
                newBat.Name = bat.Attribute("Name").Value;
                newBat.Batgenus = bat.Descendants("BatGenus").FirstOrDefault().Value;
                newBat.BatSpecies = bat.Descendants("BatSpecies").FirstOrDefault().Value;
                newBat.SortIndex = i;
                if (isNew)
                {
                    batReferenceDataContext.Bats.InsertOnSubmit(newBat);
                }
                batReferenceDataContext.SubmitChanges();
                var newCommonNames = bat.Descendants("BatCommonName");
                if (newCommonNames != null && newCommonNames.Count() > 0)
                {
                    short index = 0;
                    foreach (var name in newCommonNames)
                    {
                        BatCommonName bcn = new BatCommonName();
                        bcn.BatCommonName1 = name.Value;
                        bcn.BatID = newBat.Id;
                        bcn.SortIndex = index++;
                        newBat.BatCommonNames.Add(bcn);
                    }
                }

                var newTags = bat.Descendants("BatTag");
                if (newTags != null && newTags.Count() > 0)
                {
                    short index = 0;
                    foreach (var tag in newTags)
                    {
                        BatTag bt = new BatTag();
                        bt.BatTag1 = tag.Value;
                        bt.BatID = newBat.Id;
                        bt.SortIndex = index++;
                        newBat.BatTags.Add(bt);
                    }
                }

           /*     if (isNew)
                {
                    batReferenceDataContext.Bats.InsertOnSubmit(newBat);
                }*/
                batReferenceDataContext.SubmitChanges();
                //int thisBatID = newBat.Id;

                //MergeCommonNames(bat, thisBatID, batReferenceDataContext);


                //MergeTags(bat, thisBatID, batReferenceDataContext);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex);
            }

            


        }

        /// <summary>
        /// Merges the common names contained in the XElement Bat into the database
        /// whose DataContext is provided linked to the bat entry ID
        /// </summary>
        /// <param name="bat">The bat.</param>
        /// <param name="thisBatID">The this bat identifier.</param>
        /// <param name="batReferenceDataContext">The bat reference data context.</param>
        private void MergeCommonNames(XElement bat, int thisBatID, BatReferenceDBLinqDataContext batReferenceDataContext)
        {
            var newCommonNames = bat.Descendants("BatCommonName");
            var oldCommonNames = from name in batReferenceDataContext.BatCommonNames
                                 where name.BatID == thisBatID
                                 select name.BatCommonName1;
            var oldCommonNamesAsList = oldCommonNames.ToList();
            foreach (string newname in newCommonNames)
            {
                if (!oldCommonNamesAsList.Contains(newname))
                {
                    BatCommonName bcn = new BatCommonName();
                    bcn.BatCommonName1 = newname;
                    bcn.BatID = thisBatID;
                    batReferenceDataContext.BatCommonNames.InsertOnSubmit(bcn);
                    batReferenceDataContext.SubmitChanges();
                }
            }
        }

        /// <summary>
        /// Merges the tags contained in the XElement Bat into the database
        /// whose DataContext is provided linked to the bat entry ID
        /// </summary>
        /// <param name="bat">The bat.</param>
        /// <param name="thisBatID">The this bat identifier.</param>
        /// <param name="batReferenceDataContext">The bat reference data context.</param>
        private void MergeTags(XElement bat,int thisBatID, BatReferenceDBLinqDataContext batReferenceDataContext)
        {
            var newTags = bat.Descendants("BatTag");
            var oldTags = from tag in batReferenceDataContext.BatTags
                          where tag.BatID == thisBatID
                          select tag.BatTag1;
            var oldTagsAsList = oldTags.ToList();
            foreach (string tag in newTags)
            {
                if (!oldTagsAsList.Contains(tag))
                {
                    BatTag bt = new BatTag();
                    bt.BatTag1 = tag;
                    bt.BatID = thisBatID;
                    batReferenceDataContext.BatTags.InsertOnSubmit(bt);
                    batReferenceDataContext.SubmitChanges();
                }
            }
        }

        /// <summary>
        /// Merges the databases.  Adds newly defined bats and BatTags and BatCommonNames
        /// but allows the existing database to retain existing additional Bats, Battags
        /// and BatCommonNames.  In the event of a clash the existing data is retained.
        /// Bats with e new Name are considered to be new bats.
        /// </summary>
        /// <param name="v1">The workingDBFile.</param>
        /// <param name="v2">The newDBFile.</param>
        /// <exception cref="NotImplementedException"></exception>
        private void MergeDatabases(string workingDBFile, string newDBFile)
        {
            Console.WriteLine("Database " + newDBFile + " not merged into " + workingDBFile);
        }

  /*      /// <summary>
        /// merges two BatLibrary XML files.  All data in the editable version is to be
        /// retained, but new data from the new version is added.  Existing data in the
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
        */

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
        public List<Bat> getBatElement(String description)
        {

            
            List<Bat> matchingBats = new List<Bat>();

            if (String.IsNullOrWhiteSpace(description)) return (null);

            if (batReferenceDataContext == null) return (null);

            foreach(var tag in batReferenceDataContext.BatTags)
            {
                if (!tag.BatTag1.ToUpper().StartsWith(tag.BatTag1))
                {
                    if (description.ToUpper().Contains(tag.BatTag1.ToUpper()))
                    {
                        matchingBats.Add(tag.Bat);
                    }
                    else
                    {
                        if (description.Contains(tag.BatTag1))
                        {
                            matchingBats.Add(tag.Bat);
                        }
                    }
                }
            }

            /*if (xeBatLibrary == null) return (null);

            foreach (XElement bat in xeBatLibrary.Descendants("Bat"))
            {
                if (TagMatch(bat, description))
                {
                    matchingBats.Add(bat);
                }
            }*/
            return (matchingBats);
        }

        /// <summary>
        /// Converts the bat to xelement.  Takes a Bat reference from the database and
        /// formats the database information related to that bat into a single
        /// XElelement
        /// </summary>
        /// <param name="bat">The bat.</param>
        /// <returns></returns>
        public XElement ConvertBatToXelement(Bat bat)
        {
            XElement result = new XElement("Bat");
            result.Add(new XAttribute("Name", bat.Name));
            result.Add(new XElement("BatGenus", bat.Batgenus));
            result.Add(new XElement("BatSpecies", bat.BatSpecies));
            var commonNames = from cn in batReferenceDataContext.BatCommonNames
                              where cn.BatID == bat.Id
                              select cn.BatCommonName1;
            foreach(var name in commonNames)
            {
                result.Add(new XElement("BatCommonName", name));
            }
            var tags = from tag in batReferenceDataContext.BatTags
                       where tag.BatID == bat.Id
                       select tag.BatTag1;
            foreach(var tag in tags)
            {
                result.Add(new XElement("BatTag", tag));
            }

            return (result);

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

            XElement xeBatLibrary = new XElement("BatLibrary");
            foreach(var bat in batReferenceDataContext.Bats)
            {
                xeBatLibrary.Add(ConvertBatToXelement(bat));
            }
            return (xeBatLibrary);

        }

        internal string getFileLocation()
        {
            return (FileLocation);
        }

        
    }

    
}