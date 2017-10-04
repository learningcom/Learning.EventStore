using System;
using System.Collections.Generic;
using System.Text;
using Learning.EventStore.Extensions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Learning.EventStore.Test.Extensions
{
    [TestClass]
    public class StringExtensionsCompressTest
    {
        [TestMethod]
        public void CompressesStringIfSizeIsGreaterThanThresholdAndNotAlreadyCompressed()
        {
            const string testString = "12345678901";

            var result = testString.Compress(10);

            Assert.IsTrue(result.IsCompressed());
        }

        [TestMethod]
        public void DoesNotCompressStringIfSizeIsLessThanThresholdAndNotAlreadyCompressed()
        {
            const string testString = "123456789";

            var result = testString.Compress(10);

            Assert.IsFalse(result.IsCompressed());
            Assert.AreEqual(testString, result);
        }

        [TestMethod]
        public void DoesNotCompressStringIfSizeIGreaterThanThresholdAndAlreadyCompressed()
        {
            const string testString = "H4sI12345678901";

            var result = testString.Compress(10);

            Assert.IsTrue(result.IsCompressed());
            Assert.AreEqual(testString, result);
        }
    }
}
