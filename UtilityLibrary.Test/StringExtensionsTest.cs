using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ZipPicViewUWP.Utility.Test
{
    [TestClass]
    public class StringExtensionsTest
    {
        [TestMethod]
        public void TestEllipsesShorter()
        {
            Assert.AreEqual("Hello", "Hello".Ellipses(10));
        }

        [TestMethod]
        public void TestEllipsesLonger()
        {
            Assert.AreEqual("He … other", "Hello World, Long Time no see ... brother".Ellipses(10));
        }

        [TestMethod]
        public void TestExtractFileNameNoDirectory()
        {
            Assert.AreEqual("Hello", "Hello".ExtractFilename());
        }

        [TestMethod]
        public void TestExtractFileNameForwardSlash()
        {
            Assert.AreEqual("Hello", "/hell/Hello".ExtractFilename());
        }

        [TestMethod]
        public void TestExtractFileNameBackSlash()
        {
            Assert.AreEqual("Hello", @"\hell\Hello".ExtractFilename());
        }
    }
}
