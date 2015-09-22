﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Management.Automation.Runspaces;
using Microsoft.SharePoint.Client;
using System.Linq;

namespace OfficeDevPnP.PowerShell.Tests
{
    [TestClass]
    public class ContentTypeTests
    {
        private const string CTName1 = "UnitTestCT1";
        private const string CTName2 = "UnitTestCT2";
        private const string CTName3 = "UnitTestCT3";
        private const string CTName4 = "UnitTestCT4";

        private const string ListName = "UnitTestCTList";
        private List ctList;
        private ContentType ctTest;

        [TestInitialize]
        public void Initialize()
        {
            using (var ctx = TestCommon.CreateClientContext())
            {
                if (ctx.Web.ListExists(ListName))
                {
                    ctx.Web.Lists.GetByTitle(ListName).DeleteObject();
                    ctx.ExecuteQueryRetry();
                }
                ctList = ctx.Web.CreateList(ListTemplateType.GenericList, ListName, false, true, "lists/testlistctname", true);

                if (ctx.Web.ContentTypeExistsByName(CTName2))
                {
                    var ct = ctx.Web.GetContentTypeByName(CTName2);
                    ct.DeleteObject();
                    ctx.ExecuteQueryRetry();
                }
                ctx.Web.CreateContentType(CTName2, null, "UnitTestCT");
            }



        }
        [TestCleanup]
        public void CleanUp()
        {
            using (var ctx = TestCommon.CreateClientContext())
            {
                var ct = ctx.Web.GetContentTypeByName(CTName2);
                if (ct != null)
                {
                    if (ctList.ContentTypeExistsByName(CTName2))
                    {
                        ctList.RemoveContentTypeByName(CTName2);
                    }

                    ct.DeleteObject();
                    ctx.ExecuteQueryRetry();
                }

                var ct2 = ctx.Web.GetContentTypeByName(CTName1);
                if (ct2 != null)
                {
                    if (ctList.ContentTypeExistsByName(CTName1))
                    {
                        ctList.RemoveContentTypeByName(CTName1);
                    }

                    ct2.DeleteObject();
                    ctx.ExecuteQueryRetry();
                }



                var list = ctx.Web.GetListByTitle(ListName);
                if (list != null)
                {
                    list.DeleteObject();
                    ctx.ExecuteQueryRetry();
                }
            }
        }

        [TestMethod]
        public void AddContentTypeTest()
        {
            using (var scope = new PSTestScope(true))
            {
                var results = scope.ExecuteCommand("Add-SPOContentType",
                    new CommandParameter("Name", CTName4),
                    new CommandParameter("Group", "UnitTestCTGroup"));

                Assert.IsTrue(results.Any());
                Assert.IsTrue(results[0].BaseObject.GetType() == typeof(Microsoft.SharePoint.Client.ContentType));

                using (var ctx = TestCommon.CreateClientContext())
                {
                    var ct = ctx.Web.GetContentTypeByName(CTName4);
                    ct.DeleteObject();
                    ctx.ExecuteQueryRetry();
                }
            }
        }

        [TestMethod]
        public void AddContentTypeToListTest()
        {
            using (var scope = new PSTestScope(true))
            {
                var results = scope.ExecuteCommand("Add-SPOContentTypeToList",
                    new CommandParameter("ContentType", CTName2),
                    new CommandParameter("List", ListName));

                Assert.IsFalse(results.Any());

                Assert.IsTrue(ctList.ContentTypeExistsByName(CTName2));


            }
        }

        [TestMethod]
        public void AddFieldToContentTypeTest()
        {
            using (var scope = new PSTestScope(true))
            {
                using (var ctx = TestCommon.CreateClientContext())
                {
                    var results = scope.ExecuteCommand("Add-SPOFieldToContentType",
                        new CommandParameter("ContentType", CTName2),
                        new CommandParameter("Field", "Nickname"));

                    Assert.IsFalse(results.Any());


                    var ct = ctx.Web.GetContentTypeByName(CTName2);

                    if (ct != null)
                    {
                        var fields = ct.EnsureProperty(f => f.Fields);
                        Assert.IsNotNull(fields.FirstOrDefault(f => f.Title == "Nickname"));
                    }
                    else
                    {
                        Assert.Fail("Content type not found");
                    }

                }
            }
        }

        [TestMethod]
        public void GetContentTypeTest()
        {
            using (var scope = new PSTestScope(true))
            {

                var results = scope.ExecuteCommand("Get-SPOContentType",
                    new CommandParameter("Identity", CTName2));

                Assert.IsTrue(results.Any());

                Assert.IsTrue(results[0].BaseObject.GetType() == typeof(Microsoft.SharePoint.Client.ContentType));

            }
        }

        [TestMethod]
        public void RemoveContentTypeTest()
        {
            using (var scope = new PSTestScope(true))
            {
                var results = scope.ExecuteCommand("Add-SPOContentType",
                    new CommandParameter("Name", CTName3),
                    new CommandParameter("Group", "UnitTestCTGroup"));

                Assert.IsTrue(results.Count > 0);
                Assert.IsTrue(results[0].BaseObject.GetType() == typeof(Microsoft.SharePoint.Client.ContentType));


                results = scope.ExecuteCommand("Remove-SPOContentType",
                    new CommandParameter("Identity", CTName3),
                    new CommandParameter("Force"));

                using (var ctx = TestCommon.CreateClientContext())
                {
                    var ct = ctx.Web.GetContentTypeByName(CTName3);

                    Assert.IsNull(ct);
                }

            }
        }

        [TestMethod]
        public void SetDefaultContentTypeToListTest()
        {
            using (var scope = new PSTestScope(true))
            {
                if (!ctList.ContentTypeExistsByName(CTName2))
                {
                    ctList.AddContentTypeToListByName(CTName2, false, true);
                }

                var results = scope.ExecuteCommand("Set-SPODefaultContentTypeToList",
                  new CommandParameter("List", ListName),
                  new CommandParameter("ContentType", CTName2));

                ctList.RefreshLoad();
                ctList.Context.Load(ctList, l => l.ContentTypes.Include(c => c.Name));
                ctList.Context.ExecuteQueryRetry();

                var name = ctList.ContentTypes[0].Name;
                Assert.IsTrue(name == CTName2);
            }
        }
    }
}