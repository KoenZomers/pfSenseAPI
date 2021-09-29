using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace KoenZomers.pfSense.Api.UnitTest
{
    [TestClass]
    public class AuthenticatedUnitTest
    {
        /// <summary>
        /// pfSense instance to use in the unit tests to communicate with pfSense using an authenticated session
        /// </summary>
        private pfSense _pfSense;

        /// <summary>
        /// Instantiate the pfSense instance to be used in all tests
        /// </summary>
        [TestInitialize]
        public async Task TestInitialize()
        {
            // Ensure the configuration file can be found
            Assert.IsFalse(ConfigurationManager.AppSettings.Count == 0, "Application configuration file not found");

            _pfSense = new pfSense(new Uri(ConfigurationManager.AppSettings["pfSenseBaseAddress"]), ConfigurationManager.AppSettings["pfSenseUsername"], ConfigurationManager.AppSettings["pfSensePassword"]);
            await _pfSense.Authenticate();
        }

        /// <summary>
        /// Gets the raw contents of a specific page
        /// </summary>
        [TestMethod]
        public async Task RawGetUrlTestMethod()
        {
            var content = await _pfSense.GetPageContent("/status_rrd_summary.php");

            Assert.IsFalse(string.IsNullOrEmpty(content));
        }

        /// <summary>
        /// Gets this months data use
        /// </summary>
        [TestMethod]
        public async Task GetThisMonthsDataUsageTestMethod()
        {
            var dataUsage = await _pfSense.GetThisMonthsDataUse();

            Assert.IsTrue(dataUsage.Total > 0);
        }

        /// <summary>
        /// Gets last months data use
        /// </summary>
        [TestMethod]
        public async Task GetLastMonthsDataUsageTestMethod()
        {
            var dataUsage = await _pfSense.GetLastMonthsDataUse();

            Assert.IsTrue(dataUsage.Total > 0);
        }

        /// <summary>
        /// Gets the RRD Summary with this and last months data use
        /// </summary>
        [TestMethod]
        public async Task GetRrdSummaryTestMethod()
        {
            var dataUsage = await _pfSense.GetRrdSummary();

            Assert.IsNotNull(dataUsage.DataUseLastMonth);
            Assert.IsNotNull(dataUsage.DataUseThisMonth);
            Assert.IsTrue(dataUsage.DataUseLastMonth.Total > 0);
            Assert.IsTrue(dataUsage.DataUseThisMonth.Total > 0);
        }
    }
}
