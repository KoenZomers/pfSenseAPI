using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

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
            _pfSense = new pfSense(new Uri(ConfigurationManager.AppSettings["pfSenseBaseAddress"]), ConfigurationManager.AppSettings["pfSenseUsername"], ConfigurationManager.AppSettings["pfSensePassword"]);
        }

        /// <summary>
        /// Calls some page on pfSense without authenticating first to ensure we get the SessionNotAuthenticatedException
        /// </summary>
        /// <returns></returns>
        [ExpectedException(typeof(Exceptions.SessionNotAuthenticatedException))]
        [TestMethod]
        public async Task UnauthenticatedTestMethod()
        {
            await _pfSense.GetRrdSummary();
        }

        /// <summary>
        /// Gets the raw contents of a specific page
        /// </summary>
        [TestMethod]
        public async Task RawGetUrlTestMethod()
        {
            await _pfSense.Authenticate();
            var content = await _pfSense.GetPageContent("/status_rrd_summary.php");

            Assert.IsFalse(string.IsNullOrEmpty(content));
        }

        /// <summary>
        /// Gets this months data use
        /// </summary>
        [TestMethod]
        public async Task GetThisMonthsDataUsageTestMethod()
        {
            await _pfSense.Authenticate();
            var dataUsage = await _pfSense.GetThisMonthsDataUse();

            Assert.IsTrue(dataUsage.Total > 0);
        }

        /// <summary>
        /// Gets last months data use
        /// </summary>
        [TestMethod]
        public async Task GetLastMonthsDataUsageTestMethod()
        {
            await _pfSense.Authenticate();
            var dataUsage = await _pfSense.GetLastMonthsDataUse();

            Assert.IsTrue(dataUsage.Total > 0);
        }

        /// <summary>
        /// Gets the RRD Summary with this and last months data use
        /// </summary>
        [TestMethod]
        public async Task GetRrdSummaryTestMethod()
        {
            await _pfSense.Authenticate();
            var dataUsage = await _pfSense.GetRrdSummary();

            Assert.IsNotNull(dataUsage.DataUseLastMonth);
            Assert.IsNotNull(dataUsage.DataUseThisMonth);
            Assert.IsTrue(dataUsage.DataUseLastMonth.Total > 0);
            Assert.IsTrue(dataUsage.DataUseThisMonth.Total > 0);
        }
    }
}
