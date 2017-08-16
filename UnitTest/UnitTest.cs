using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace KoenZomers.pfSense.Api.UnitTest
{
    [TestClass]
    public class UnitTest
    {
        /// <summary>
        /// pfSense instance to use in the unit tests to communicate with pfSense
        /// </summary>
        private pfSense _pfSense;

        /// <summary>
        /// Instantiate the pfSense instance to be used in all tests
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _pfSense = new pfSense(ConfigurationManager.AppSettings["pfSenseBaseAddress"], ConfigurationManager.AppSettings["pfSenseUsername"], ConfigurationManager.AppSettings["pfSensePassword"]);
        }

        /// <summary>
        /// Gets the raw contents of a specific page
        /// </summary>
        [TestMethod]
        public void RawGetUrlTestMethod()
        {
            var content = _pfSense.GetPageContent("/status_rrd_summary.php");

            Assert.IsFalse(string.IsNullOrEmpty(content));
        }

        /// <summary>
        /// Gets this months data use
        /// </summary>
        [TestMethod]
        public void GetThisMonthsDataUsageTestMethod()
        {
            var dataUsage = _pfSense.GetThisMonthsDataUse();

            Assert.IsTrue(dataUsage.Total > 0);
        }

        /// <summary>
        /// Gets last months data use
        /// </summary>
        [TestMethod]
        public void GetLastMonthsDataUsageTestMethod()
        {
            var dataUsage = _pfSense.GetLastMonthsDataUse();

            Assert.IsTrue(dataUsage.Total > 0);
        }

        /// <summary>
        /// Gets the RRD Summary with this and last months data use
        /// </summary>
        [TestMethod]
        public void GetRrdSummaryTestMethod()
        {
            var dataUsage = _pfSense.GetRrdSummary();

            Assert.IsNotNull(dataUsage.DataUseLastMonth);
            Assert.IsNotNull(dataUsage.DataUseThisMonth);
            Assert.IsTrue(dataUsage.DataUseLastMonth.Total > 0);
            Assert.IsTrue(dataUsage.DataUseThisMonth.Total > 0);
        }
    }
}
