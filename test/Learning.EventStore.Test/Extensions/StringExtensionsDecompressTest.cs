using System;
using System.Text;
using Learning.EventStore.Extensions;
using Microsoft.VisualBasic.CompilerServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Extensions
{
    [TestClass]
    public class StringExtensionsDecompressTest
    {
        [TestMethod]
        public void DecompressesString()
        {
            const string testString = "12345678901";
            var compressedString = testString.Compress(10);

            var decompressedString = compressedString.Decompress();

            Assert.AreEqual(testString, decompressedString);
        }

        [TestMethod]
        public void ReturnsOriginalStringIfItIsNotCompressed()
        {
            const string testString = "12345678901";

            var decompressedString = testString.Decompress();

            Assert.AreEqual(testString, decompressedString);
        }

        [TestMethod]
        public void ReturnsOriginalStringIfInvalidFormat()
        {
            const string testString = "H4sI12345678901";

            var decompressedString = testString.Decompress();

            Assert.AreEqual(testString, decompressedString);
        }
    }
}
