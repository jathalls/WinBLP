using BatRecordingManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace TestBatRecordingManager
{
    [TestClass]
    public class DBAccessTests
    {
        private Bat bat;
        private int numberOfBatsInDatabase = 0;

        [TestMethod]
        public void TestDBAccess_GetNamedBat()
        {
            Setup();
            Bat namedBat = DBAccess.GetNamedBat("TestName");

            Assert.IsNotNull(namedBat);

            Assert.AreEqual(1, namedBat.BatTags.Count);

            Assert.AreEqual("TestTag", namedBat.BatTags[0].BatTag1);

            Teardown();
        }

        [TestMethod]
        public void TestDBAccess_InsertBat()
        {
            Setup();
            Bat extraBat = CreateStandardTestBat();

            extraBat.Batgenus = "TestGenus2";
            extraBat.BatSpecies = "TestSpecies2";
            extraBat.Name = extraBat.Batgenus.Trim() + extraBat.BatSpecies.Trim();
            string err = DBAccess.InsertBat(extraBat);
            if (!String.IsNullOrWhiteSpace(err))
            {
                Assert.Fail("Insert error generated:-" + err);
            }
            Bat retrievedBat = DBAccess.GetNamedBat(extraBat.Name);
            Assert.IsNotNull(retrievedBat);
            Assert.AreEqual(extraBat.Name, retrievedBat.Name);
            Assert.AreEqual(extraBat.Batgenus, retrievedBat.Batgenus);
            Assert.AreEqual(extraBat.BatSpecies, retrievedBat.BatSpecies);

            Teardown();
        }

        [TestMethod]
        public void TestDBAccess_MergeBat_Merge()
        {
            Setup();
            Bat modifiedBat = CreateStandardTestBat();

            BatTag bt = new BatTag();
            bt.BatTag1 = "Test Tag Two";
            bt.SortIndex = (short)modifiedBat.BatTags.Count;
            modifiedBat.BatTags.Add(bt);
            modifiedBat.Notes = modifiedBat.Notes + " with additional text";
            string err = DBAccess.MergeBat(modifiedBat);
            Assert.IsTrue(String.IsNullOrWhiteSpace(err), err);

            //Assert.AreEqual(bat.BatCommonNames.Count() + 1, modifiedBat.BatCommonNames.Count(),"BatCommonNames.Count");
            //Assert.AreEqual(bat.BatTags.Count() + 1, modifiedBat.BatTags.Count(),"BatTags.Count");

            Bat retrievedBat = DBAccess.GetNamedBat(modifiedBat.Name);
            Assert.IsNotNull(retrievedBat, "No Bat Retrieved:-" + modifiedBat.Name);

            Assert.AreEqual(bat.BatTags.Count() + 1, retrievedBat.BatTags.Count(), "Bat Tags Count");
            Assert.AreEqual("Test Tag Two", retrievedBat.BatTags.Last<BatTag>().BatTag1, "New bat tag");
            Assert.AreEqual((short)bat.BatTags.Count(), retrievedBat.BatTags.Last<BatTag>().SortIndex, "Last Tag Sort Index");

            Teardown();
        }

        private Bat CreateStandardTestBat()
        {
            Bat bat = new Bat();

            BatTag tag = new BatTag();
            tag.BatTag1 = "TestTag";
            tag.SortIndex = 0;
            tag.BatID = bat.Id;
            bat.BatTags.Add(tag);
            bat.Batgenus = "TestGenus";
            bat.BatSpecies = "TestSpecies";
            bat.Notes = "Test Notes";
            bat.SortIndex = 0;
            bat.Name = "TestName";
            return (bat);
        }

        private void DeleteAllTestBats(BatReferenceDBLinqDataContext dc)
        {
            try
            {
                //                dc.Bats.DeleteOnSubmit(bat);
                //                dc.SubmitChanges();
                //string testName = "TestName";
                var testBats = from b in dc.Bats
                               where b.Name.StartsWith("Test")
                               select b;

                if (testBats != null && testBats.Count() > 0)
                {
                    foreach (var b in testBats)
                    {
                        var tags = from t in dc.BatTags
                                   where t.BatID == b.Id
                                   select t;
                        dc.BatTags.DeleteAllOnSubmit(tags);

                        dc.SubmitChanges();
                    }

                    dc.Bats.DeleteAllOnSubmit(testBats);
                    dc.SubmitChanges();
                }
            }
            catch (Exception ex)
            {
                Assert.Fail("Teardown: " + ex.Message);
            }
        }

        private void Setup()
        {
            try
            {
                bat = CreateStandardTestBat();
                BatReferenceDBLinqDataContext dc = new BatReferenceDBLinqDataContext();
                DeleteAllTestBats(dc);
                numberOfBatsInDatabase = dc.Bats.Count();
                dc.Bats.InsertOnSubmit(bat);
                dc.SubmitChanges();
                Assert.IsNotNull(bat);
                Assert.AreNotEqual(-1, bat.Id);
            }
            catch (Exception ex)
            {
                Assert.Fail("Setup:- " + ex.Message);
            }
        }

        private void Teardown()
        {
            BatReferenceDBLinqDataContext dc = new BatReferenceDBLinqDataContext();
            DeleteAllTestBats(dc);
            Assert.AreEqual(numberOfBatsInDatabase, dc.Bats.Count());
        }
    }
}