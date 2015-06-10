using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using KoenZomers.pfSense.Api.Entities;

namespace KoenZomers.pfSense.Api
{
    public class pfSense
    {
        #region Properties

        /// <summary>
        /// Address of the pfSense server this instance will communicate with. I.e. https://192.168.0.1
        /// </summary>
        public string ServerAddress { get; private set; }

        /// <summary>
        /// Username used for communicating with pfSense
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Password used for communicating with pfSense
        /// </summary>
        public string Password { get; private set; }

        #endregion

        #region Fields

        /// <summary>
        /// Cookie container used to persist cookies throughout the session with the pfSense server
        /// </summary>
        private readonly CookieContainer _cookieContainer;

        /// <summary>
        /// Boolean indicating if the current instance already has an authenticated connection to pfSense
        /// </summary>
        private bool _isSessionAuthenticated;

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiate a new pfSense API instance to a specific pfSense server
        /// </summary>
        /// <param name="serverAddress">Address of the pfSense server this instance will communicate with</param>
        /// <param name="username">Username used for communicating with pfSense</param>
        /// <param name="password">Password used for communicating with pfSense</param>
        public pfSense(string serverAddress, string username, string password)
        {
            ServerAddress = serverAddress;
            Username = username;
            Password = password;

            _cookieContainer = new CookieContainer();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Authenticates to pfSense
        /// </summary>
        private void Authenticate()
        {
            // Verify if we already have authenticated
            if (_isSessionAuthenticated) return;

            // Create a session on the pfSense webserver
            var loginPageContents = HttpUtility.GetPageContents(ServerAddress, _cookieContainer);

            // Use a regular expression to fetch the anti cross site scriping token from the HTML
            var xssToken = Regex.Match(loginPageContents, "<input.+?type=['\"]hidden['\"].+?name=['\"]_+?csrf_magic['\"] value=['\"](?<xsstoken>.*?)['\"].+?/>", RegexOptions.IgnoreCase);

            // Verify that the anti XSS token was found
            if (!xssToken.Success)
            {
                xssToken = Regex.Match(loginPageContents, "var.*?csrfMagicToken.*?=.*?\"(?<xsstoken>.*?)\"");
            }

            // Authenticate the session
            var authenticationResult = HttpUtility.AuthenticateViaUrlEncodedFormMethod(string.Concat(ServerAddress, "/index.php"),
                                                                                       new Dictionary<string, string>
                                                                                       {
                                                                                            {"__csrf_magic", xssToken.Groups["xsstoken"].Value },
                                                                                            { "usernamefld", Username }, 
                                                                                            { "passwordfld", Password }, 
                                                                                            { "login", "Login" }
                                                                                       },
                                                                                       _cookieContainer);

            // Verify if the username/password combination was valid by examining the server response
            if (authenticationResult.Contains("Username or Password incorrect"))
            {
                throw new ApplicationException("ERROR: Credentials incorrect");
            }

            _isSessionAuthenticated = true;
        }

        /// <summary>
        /// Internal method to parse last months data use from the provided page contents
        /// </summary>
        /// <param name="pageContent">Raw page contents of the RRD Summary page</param>
        /// <returns>Parsed data use of last month</returns>
        private DataUse GetLastMonthsDataUse(string pageContent)
        {
            // Parse the content using a RegEx
            var regExMatch = Regex.Match(pageContent, @"Last Month:.*?<td>In</td><td.*?>(?<in>\d+).*?</td>.*?<td>Out</td><td.*?>(?<out>\d+).*?</td>.*?<td>Total</td><td.*?>(?<total>\d+).*</td>", RegexOptions.Singleline);

            // Copy in the parsed data into the entity
            var dataUse = new DataUse
            {
                In = regExMatch.Groups["in"].Success ? (int?)int.Parse(regExMatch.Groups["in"].Value) : null,
                Out = regExMatch.Groups["out"].Success ? (int?)int.Parse(regExMatch.Groups["out"].Value) : null,
                Total = regExMatch.Groups["total"].Success ? (int?)int.Parse(regExMatch.Groups["total"].Value) : null
            };
            return dataUse;
        }

        /// <summary>
        /// Internal method to parse this months data use from the provided page contents
        /// </summary>
        /// <param name="pageContent">Raw page contents of the RRD Summary page</param>
        /// <returns>Parsed data use of this month</returns>
        private DataUse GetThisMonthsDataUse(string pageContent)
        {
            // Parse the content using a RegEx
            var regExMatch = Regex.Match(pageContent, @"This Month.*?<td>In</td><td.*?>(?<in>\d+).*?</td>.*?<td>Out</td><td.*?>(?<out>\d+).*?</td>.*?<td>Total</td><td.*?>(?<total>\d+).*</td>", RegexOptions.Singleline);

            // Copy in the parsed data into the entity
            var dataUse = new DataUse
            {
                In = regExMatch.Groups["in"].Success ? (int?)int.Parse(regExMatch.Groups["in"].Value) : null,
                Out = regExMatch.Groups["out"].Success ? (int?)int.Parse(regExMatch.Groups["out"].Value) : null,
                Total = regExMatch.Groups["total"].Success ? (int?)int.Parse(regExMatch.Groups["total"].Value) : null
            };
            return dataUse;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Retrieves the contents of a pfSense page
        /// </summary>
        /// <param name="serverRelativeUrl">Server relative url of the page to retrieve</param>
        /// <returns>Raw page contents as a string</returns>
        public string GetPageContent(string serverRelativeUrl)
        {
            // Authenticate the session, if needed
            Authenticate();

            // Get the contents of the page
            var pageContent = HttpUtility.GetPageContents(string.Concat(ServerAddress, serverRelativeUrl), _cookieContainer);
            return pageContent;
        }

        /// <summary>
        /// Gets the data use on the pfSense server of this month. Requires the RRD Summary package to be installed on the pfSense server.
        /// </summary>
        /// <returns>DataUse entity with the statistics of this month</returns>
        public DataUse GetThisMonthsDataUse()
        {
            // Get the contents of the RRD Summary page and return the parsed result
            var pageContent = GetPageContent("/status_rrd_summary.php");
            return GetThisMonthsDataUse(pageContent);
        }

        /// <summary>
        /// Gets the data use on the pfSense server of last month. Requires the RRD Summary package to be installed on the pfSense server.
        /// </summary>
        /// <returns>DataUse entity with the statistics of last month</returns>
        public DataUse GetLastMonthsDataUse()
        {
            // Get the contents of the RRD Summary page and return the parsed result
            var pageContent = GetPageContent("/status_rrd_summary.php");
            return GetLastMonthsDataUse(pageContent);
        }

        /// <summary>
        /// Gets the RRD Summary with the data use on the pfSense server of this and last month. Requires the RRD Summary package to be installed on the pfSense server.
        /// </summary>
        /// <returns>RrdSummary entity with the data use statistics of this and last month</returns>
        public RrdSummary GetRrdSummary()
        {
            // Get the contents of the RRD Summary page
            var pageContent = GetPageContent("/status_rrd_summary.php");

            // Copy in the parsed data into the entity
            var rrdSummary = new RrdSummary
            {
                DataUseThisMonth = GetThisMonthsDataUse(pageContent),
                DataUseLastMonth = GetLastMonthsDataUse(pageContent)
            };
            return rrdSummary;
        }

        #endregion
    }
}
