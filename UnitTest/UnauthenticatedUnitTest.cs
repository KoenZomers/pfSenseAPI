using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace KoenZomers.pfSense.Api.UnitTest
{
    [TestClass]
    public class UnauthenticatedUnitTest
    {
        /// <summary>
        /// pfSense instance to use in the unit tests to communicate with pfSense without authenticating
        /// </summary>
        private pfSense _pfSense;

        /// <summary>
        /// Instantiate the pfSense instance to be used in all tests
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            _pfSense = new pfSense(new Uri("https://192.168.0.1"), string.Empty, string.Empty);
        }

        /// <summary>
        /// Calls some page on pfSense without authenticating first to ensure we get the SessionNotAuthenticatedException
        /// </summary>
        /// <returns></returns>
        [ExpectedException(typeof(Exceptions.SessionNotAuthenticatedException))]
        [TestMethod]
        public async Task GetRrdSummaryTest()
        {
            await _pfSense.GetRrdSummary();
        } 
    }
}
