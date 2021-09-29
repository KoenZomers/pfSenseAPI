using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using KoenZomers.pfSense.Api.Entities;
using System.Threading.Tasks;
using System.Linq;

namespace KoenZomers.pfSense.Api
{
    public class pfSense
    {
        #region Properties

        /// <summary>
        /// Address of the pfSense server this instance will communicate with. I.e. https://192.168.0.1
        /// </summary>
        public Uri ServerBaseAddress { get; private set; }

        /// <summary>
        /// Username used for communicating with pfSense
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Password used for communicating with pfSense
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Boolean indicating if the current instance already has an authenticated connection to pfSense
        /// </summary>
        public bool IsSessionAuthenticated;

        #endregion

        #region Fields

        /// <summary>
        /// Cookie container used to persist cookies throughout the session with the pfSense server
        /// </summary>
        private readonly CookieContainer _cookieContainer;

        #endregion

        #region Constructors

        /// <summary>
        /// Instantiate a new pfSense API instance to a specific pfSense server
        /// </summary>
        /// <param name="serverAddress">Address of the pfSense server this instance will communicate with</param>
        /// <param name="username">Username used for communicating with pfSense</param>
        /// <param name="password">Password used for communicating with pfSense</param>
        public pfSense(Uri serverAddress, string username, string password)
        {
            ServerBaseAddress = serverAddress;
            Username = username;
            Password = password;

            _cookieContainer = new CookieContainer();
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Authenticates to pfSense
        /// </summary>
        public async Task Authenticate()
        {
            // Verify if we already have authenticated
            if (IsSessionAuthenticated) return;

            // Create a session on the pfSense webserver
            var loginPageContents = await HttpUtility.GetPageContents(ServerBaseAddress, _cookieContainer);

            // Use a regular expression to fetch the anti cross site scriping token from the login page HTML
            var xssTokenLoginPage = Regex.Match(loginPageContents, "<input.+?type=['\"]hidden['\"].+?name=['\"]_+?csrf_magic['\"] value=['\"](?<xsstoken>.*?)['\"].+?/>", RegexOptions.IgnoreCase);

            // Verify that the anti XSS token was found
            if (!xssTokenLoginPage.Success)
            {
                xssTokenLoginPage = Regex.Match(loginPageContents, "var.*?csrfMagicToken.*?=.*?\"(?<xsstoken>.*?)\"");
            }            

            // Authenticate the session
            var authenticationResult = await HttpUtility.AuthenticateViaUrlEncodedFormMethod(string.Concat(ServerBaseAddress, "/index.php"),
                                                                                       new Dictionary<string, string>
                                                                                       {
                                                                                            {"__csrf_magic", xssTokenLoginPage.Groups["xsstoken"].Value },
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

            IsSessionAuthenticated = true;
        }

        /// <summary>
        /// Internal method to parse last months data use from the provided page contents
        /// </summary>
        /// <param name="pageContent">Raw page contents of the RRD Summary page</param>
        /// <returns>Parsed data use of last month</returns>
        private DataUse GetLastMonthsDataUse(string pageContent)
        {
            // Parse the content using a RegEx
            var regExMatch = Regex.Matches(pageContent, @"<td>\d{4}-\d{2}-\d{2} to \d{2}-\d{2}-\d{4}</td>.*?<td>(?<in>[\d\.]+).*?</td>.*?<td>(?<out>[\d\.]+).*?</td>.*?<td>(?<total>[\d\.]+).*?</td>", RegexOptions.Singleline);

            // Ensure we at least have two matches
            if (regExMatch.Count < 1) return null;
            var c = System.Globalization.CultureInfo.CurrentCulture;
            // Copy in the parsed data into the entity
            var dataUse = new DataUse
            {
                In = regExMatch[1].Groups["in"].Success ? (decimal?)decimal.Parse(regExMatch[1].Groups["in"].Value.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)) : null,
                Out = regExMatch[1].Groups["out"].Success ? (decimal?)decimal.Parse(regExMatch[1].Groups["out"].Value.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)) : null,
                Total = regExMatch[1].Groups["total"].Success ? (decimal?)decimal.Parse(regExMatch[1].Groups["total"].Value.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)) : null
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
            var regExMatch = Regex.Match(pageContent, @"<td>\d{4}-\d{2}-\d{2} to \d{2}-\d{2}-\d{4}</td>.*?<td>(?<in>[\d\.]+).*?</td>.*?<td>(?<out>[\d\.]+).*?</td>.*?<td>(?<total>[\d\.]+).*?</td>", RegexOptions.Singleline);

            // Copy in the parsed data into the entity
            var dataUse = new DataUse
            {
                In = regExMatch.Groups["in"].Success ? (decimal?)decimal.Parse(regExMatch.Groups["in"].Value.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)) : null,
                Out = regExMatch.Groups["out"].Success ? (decimal?)decimal.Parse(regExMatch.Groups["out"].Value.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)) : null,
                Total = regExMatch.Groups["total"].Success ? (decimal?)decimal.Parse(regExMatch.Groups["total"].Value.Replace(".", System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator)) : null
            };
            return dataUse;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Gets the data use on the pfSense server of this month. Requires the RRD Summary package to be installed on the pfSense server.
        /// </summary>
        /// <returns>DataUse entity with the statistics of this month</returns>
        public async Task<DataUse> GetThisMonthsDataUse()
        {
            if (!IsSessionAuthenticated)
            {
                throw new Exceptions.SessionNotAuthenticatedException();
            }

            // Get the contents of the RRD Summary page and return the parsed result
            var pageContent = await HttpUtility.GetPageContents(new Uri(ServerBaseAddress, "/status_rrd_summary.php"), _cookieContainer);
            return GetThisMonthsDataUse(pageContent);
        }

        /// <summary>
        /// Gets the data use on the pfSense server of last month. Requires the RRD Summary package to be installed on the pfSense server.
        /// </summary>
        /// <returns>DataUse entity with the statistics of last month</returns>
        public async Task<DataUse> GetLastMonthsDataUse()
        {
            if (!IsSessionAuthenticated)
            {
                throw new Exceptions.SessionNotAuthenticatedException();
            }

            // Get the contents of the RRD Summary page and return the parsed result
            var pageContent = await HttpUtility.GetPageContents(new Uri(ServerBaseAddress, "/status_rrd_summary.php"), _cookieContainer);
            return GetLastMonthsDataUse(pageContent);
        }

        /// <summary>
        /// Gets the RAW result of a request towards pfSense so you can parse the result yourself
        /// </summary>
        /// <param name="url">Server relative URL of the page to retrieve</param>
        /// <returns>String containing the page contents</returns>
        public async Task<string> GetPageContent(string url)
        {
            if (!IsSessionAuthenticated)
            {
                throw new Exceptions.SessionNotAuthenticatedException();
            }

            var pageContent = await HttpUtility.GetPageContents(new Uri(ServerBaseAddress, url), _cookieContainer);
            return pageContent;
        }

        /// <summary>
        /// Gets the RRD Summary with the data use on the pfSense server of this and last month. Requires the RRD Summary package to be installed on the pfSense server.
        /// </summary>
        /// <returns>RrdSummary entity with the data use statistics of this and last month</returns>
        public async Task<RrdSummary> GetRrdSummary()
        {
            if (!IsSessionAuthenticated)
            {
                throw new Exceptions.SessionNotAuthenticatedException();
            }

            // Get the contents of the RRD Summary page
            var pageContent = await HttpUtility.GetPageContents(new Uri(ServerBaseAddress, "/status_rrd_summary.php"), _cookieContainer);

            // Copy in the parsed data into the entity
            var rrdSummary = new RrdSummary
            {
                DataUseThisMonth = GetThisMonthsDataUse(pageContent),
                DataUseLastMonth = GetLastMonthsDataUse(pageContent)
            };
            return rrdSummary;
        }

        /// <summary>
        /// Gets the contents of a backup of the pfSense configuration
        /// </summary>
        /// <param name="includePackageInformation">Boolean indicating if package information should be included in the backup</param>
        /// <param name="includeRrdData">Boolean indicating if RRD data should be included in the backup</param>
        /// <param name="encryptBackup">Boolean indicating if the backup should be encrypted</param>
        /// <param name="encryptPassword">Password to use for the encrypted backup</param>
        public async Task<string> GetBackupContents(bool includePackageInformation = true, bool includeRrdData = true, bool encryptBackup = false, string encryptPassword = null)
        {
            if(encryptBackup && string.IsNullOrWhiteSpace(encryptPassword))
            {
                throw new ArgumentNullException("Password must be provided if encryption needs to be applied", nameof(encryptPassword));
            }

            if (!IsSessionAuthenticated)
            {
                throw new Exceptions.SessionNotAuthenticatedException();
            }

            // Get the page contents so we can get the CSRF token
            var pageContent = await HttpUtility.GetPageContents(new Uri(ServerBaseAddress, "/diag_backup.php"), _cookieContainer);

            // Use a regular expression to fetch the anti cross site scriping token from the page HTML
            var xssToken = Regex.Match(pageContent, "<input.+?type=['\"]hidden['\"].+?name=['\"]_+?csrf_magic['\"] value=['\"](?<xsstoken>.*?)['\"].+?/>", RegexOptions.IgnoreCase);

            // Verify that the anti XSS token was found
            if (!xssToken.Success)
            {
                xssToken = Regex.Match(pageContent, "var.*?csrfMagicToken.*?=.*?\"(?<xsstoken>.*?)\"");
            }

            var downloadArgs = new Dictionary<string, string>
                {
                    {"__csrf_magic", xssToken.Groups["xsstoken"].Value },
                    { "backuparea", "" },
                    { "nopackages", includePackageInformation ? "" : "yes" },
                    { "donotbackuprrd", includeRrdData ? "" : "yes" },
                    { "encrypt", encryptBackup ? "yes" : "" },
                    { "encrypt_password", encryptPassword },
                    { "encrypt_password_confirm", encryptPassword },
                    { "download", "Download configuration as XML" },
                    { "restorearea", "" }
                };

            // Get the contents of the RRD Summary page and return the parsed result
            var backupContents = HttpUtility.DownloadBackupFile(string.Concat(ServerBaseAddress, "/diag_backup.php"),
                                                new Dictionary<string, string>(),
                                                downloadArgs,
                                                _cookieContainer,
                                                out string filename);
            return backupContents;
        }

        /// <summary>
        /// Gets the contents of a backup of the pfSense configuration and saves it to the provided location
        /// </summary>
        /// <param name="filePath">Full path including filename where to save the backup</param>
        /// <param name="includePackageInformation">Boolean indicating if package information should be included in the backup</param>
        /// <param name="includeRrdData">Boolean indicating if RRD data should be included in the backup</param>
        /// <param name="encryptBackup">Boolean indicating if the backup should be encrypted</param>
        /// <param name="encryptPassword">Password to use for the encrypted backup</param>
        public async Task SaveBackupAs(string filePath, bool includePackageInformation = true, bool includeRrdData = true, bool encryptBackup = false, string encryptPassword = null)
        {
            var backupContent = await GetBackupContents(includePackageInformation, includeRrdData, encryptBackup, encryptPassword);

            await System.IO.File.WriteAllTextAsync(filePath, backupContent);
        }

        #endregion
    }
}
