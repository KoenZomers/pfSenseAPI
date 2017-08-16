using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KoenZomers.pfSense.Api
{
    /// <summary>
    /// Internal utility class for Http communication with pfSense
    /// </summary>
    internal static class HttpUtility
    {
        /// <summary>
        /// Performs a HEAD request to the provided url to have the remote webserver hand out a new sessionId
        /// </summary>
        /// <param name="url">Url to query</param>
        /// <param name="cookieContainer">Cookies which have been recorded for this session</param>
        public static async Task HttpCreateSession(string url, CookieContainer cookieContainer)
        {
            // Construct the request
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "HEAD";
            request.CookieContainer = cookieContainer;

            // Send the request to the webserver
            await request.GetResponseAsync();
        }

        /// <summary>
        /// Performs a GET request to the provided url to retrieve the page contents
        /// </summary>
        /// <param name="url">Url of the page to retrieve</param>
        /// <param name="cookieContainer">Cookies which have been recorded for this session</param>
        /// <returns>Contents of the page</returns>
        public static async Task<string> GetPageContents(Uri url, CookieContainer cookieContainer)
        {
            // Construct the request
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.CookieContainer = cookieContainer;

            // Send the request to the webserver
            var response = await request.GetResponseAsync();

            // Get the stream containing content returned by the server.
            var dataStream = response.GetResponseStream();
            if (dataStream == null) return null;
            
            // Open the stream using a StreamReader for easy access.
            var reader = new StreamReader(dataStream);
            
            // Read the content returned
            var responseFromServer = await reader.ReadToEndAsync();
            return responseFromServer;
        }

        /// <summary>
        /// Sends a POST request using the url encoded form method
        /// </summary>
        /// <param name="url">Url to POST to</param>
        /// <param name="formFields">Dictonary with key/value pairs containing the forms data to POST to the webserver</param>
        /// <param name="headerFields">NameValueCollection with the fields to add to the header sent to the server with the request</param>
        /// <param name="cookieContainer">Cookies which have been recorded for this session</param>
        /// <param name="timeout">Timeout in milliseconds on how long the request may take. Default = 60000 = 60 seconds.</param>
        /// <returns>The website contents returned by the webserver after posting the data</returns>
        public static async Task<string> GetPostResponse(Uri url, Dictionary<string, string> formFields, NameValueCollection headerFields = null, CookieContainer cookieContainer = null, int timeout = 60000)
        {
            // Construct the POST request which performs the login
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Post;
            request.ServicePoint.Expect100Continue = false;
            request.CookieContainer = cookieContainer;
            request.Timeout = timeout;
            if (headerFields != null)
            {
                request.Headers.Add(headerFields);
            }

            // Construct POST data
            var postData = new StringBuilder();
            foreach (var formField in formFields)
            {
                if (postData.Length > 0) postData.Append("&");
                postData.Append($"{formField.Key}={formField.Value}");
            }
            // Convert the POST data to a byte array
            var postDataByteArray = Encoding.UTF8.GetBytes(postData.ToString());
            // Set the ContentType property of the WebRequest
            request.ContentType = "application/x-www-form-urlencoded";
            // Set the ContentLength property of the WebRequest.
            request.ContentLength = postDataByteArray.Length;
            // Get the request stream
            var dataStream = await request.GetRequestStreamAsync();
            // Write the POST data to the request stream
            await dataStream.WriteAsync(postDataByteArray, 0, postDataByteArray.Length);
            // Close the Stream object
            dataStream.Close();
            // Receive the response from the webserver
            var response = await request.GetResponseAsync() as HttpWebResponse;
            // Make sure the webserver has sent a response
            if (response == null) return null;
            dataStream = response.GetResponseStream();
            // Make sure the datastream with the response is available
            if (dataStream == null) return null;
            var reader = new StreamReader(dataStream);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Sends a POST request using the url encoded form method to authenticate to pfSense
        /// </summary>
        /// <param name="url">Url to POST the login information to</param>
        /// <param name="formFields">Dictonary with key/value pairs containing the forms data to POST to the webserver</param>
        /// <param name="cookieContainer">Cookies which have been recorded for this session</param>
        /// <returns>The website contents returned by the webserver after posting the data</returns>
        public static async Task<string> AuthenticateViaUrlEncodedFormMethod(string url, Dictionary<string, string> formFields, CookieContainer cookieContainer)
        {
            // Construct the POST request which performs the login
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Post;
            request.Accept = "*/*";
            request.ServicePoint.Expect100Continue = false;
            request.CookieContainer = cookieContainer;

            // Construct POST data
            var postData = new StringBuilder();
            foreach (var formField in formFields)
            {
                if (postData.Length > 0) postData.Append("&");
                postData.Append(formField.Key);
                postData.Append("=");
                postData.Append(formField.Value);
            }

            // Convert the POST data to a byte array
            var postDataByteArray = Encoding.UTF8.GetBytes(postData.ToString());

            // Set the ContentType property of the WebRequest
            request.ContentType = "application/x-www-form-urlencoded";

            // Set the ContentLength property of the WebRequest.
            request.ContentLength = postDataByteArray.Length;

            // Get the request stream
            var dataStream = await request.GetRequestStreamAsync();

            // Write the POST data to the request stream
            dataStream.Write(postDataByteArray, 0, postDataByteArray.Length);

            // Close the Stream object
            dataStream.Close();

            // Receive the response from the webserver
            var response = await request.GetResponseAsync() as HttpWebResponse;

            // Make sure the webserver has sent a response
            if (response == null) return null;

            dataStream = response.GetResponseStream();

            // Make sure the datastream with the response is available
            if (dataStream == null) return null;

            var reader = new StreamReader(dataStream);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Forcing basic http authentication for HttpWebRequest (in .NET/C#) 
        /// http://blog.kowalczyk.info/article/Forcing-basic-http-authentication-for-HttpWebReq.html
        /// </summary>
        /// <param name="req">HttpWebRequest to add Authorization Header</param>
        /// <param name="userName">UserName of PFSense</param>
        /// <param name="userPassword">Password of PFSense</param>
        public static void SetBasicAuthHeader(HttpWebRequest req, string userName, string userPassword)
        {
            string authInfo = userName + ":" + userPassword;
            authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
            req.Headers["Authorization"] = "Basic " + authInfo;
        }
    }
}
