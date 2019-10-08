namespace FirebaseHelper
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Net;
    using Newtonsoft.Json.Linq;
    using Google.Apis.Auth.OAuth2;
    using System.IO;

    public class FirebaseResponse
    {
        public FirebaseResponse()
        {
        }
        public FirebaseResponse(bool success, string errorMessage, HttpResponseMessage httpResponse = null, string jsonContent = null)
        {
            this.Success = success;
            this.JSONContent = jsonContent;
            this.ErrorMessage = errorMessage;
            this.HttpResponse = httpResponse;
        }

        public bool Success { get; set; }

        public string JSONContent { get; set; }

        public string ErrorMessage { get; set; }

        public HttpResponseMessage HttpResponse { get; set; }
    }

    class FirebaseRequest
    {
        public FirebaseRequest(HttpMethod method, string uri, string jsonString = null)
        {
            this.Method = method;
            this.JSON = jsonString;
            this.Uri = (uri.Replace("/", string.Empty).EndsWith("firebaseio.com")) ? uri + '/' + ".json" : uri + ".json";
        }

        private HttpMethod Method { get; set; }

        private string JSON { get; set; }

        private string Uri { get; set; }


        public FirebaseResponse Execute()
        {
            Uri requestURI;
            if (FirebaseOperations.ValidateURI(this.Uri))
            {
                requestURI = new Uri(this.Uri);
            }
            else
            {
                return new FirebaseResponse(false, "Given Firebase path is not a valid HTTP/S URL");
            }
            string json = null;
            if (this.JSON != null)
            {
                if (!FirebaseOperations.CheckParseJSON(this.JSON, out json))
                {
                    return new FirebaseResponse(false, string.Format("Invalid JSON : {0}", json));
                }
            }

            Task<HttpResponseMessage> response = FirebaseOperations.RequestHelper(this.Method, requestURI, json);
            response.Wait();
            HttpResponseMessage result = response.Result;
            if (!result.IsSuccessStatusCode && result.StatusCode.Equals(HttpStatusCode.Unauthorized))
            {
                AccessAuthentication.RefreshToken();
                response = FirebaseOperations.RequestHelper(this.Method, requestURI, json);
                response.Wait();
                result = response.Result;
            }
            FirebaseResponse firebaseResponse = new FirebaseResponse()
            {
                HttpResponse = result,
                ErrorMessage = result.StatusCode.ToString() + " : " + result.ReasonPhrase,
                Success = response.Result.IsSuccessStatusCode
            };
            if (this.Method.Equals(HttpMethod.Get))
            {
                Task<string> content = result.Content.ReadAsStringAsync();
                content.Wait();
                firebaseResponse.JSONContent = content.Result;
            }
            return firebaseResponse;
        }
    }
    public class FirebaseOperations
    {
        public static string accessKey { get; set; }

        public static bool ValidateURI(string url)
        {
            Uri locurl;
            if (System.Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out locurl))
            {
                if (
                    !(locurl.IsAbsoluteUri &&
                      (locurl.Scheme == "http" || locurl.Scheme == "https")) ||
                    !locurl.IsAbsoluteUri)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        public static bool CheckParseJSON(string inJSON, out string output)
        {
            try
            {
                JToken parsedJSON = JToken.Parse(inJSON);
                output = parsedJSON.ToString();
                return true;
            }
            catch (Exception ex)
            {
                output = ex.Message;
                return false;
            }
        }

        public static Task<HttpResponseMessage> RequestHelper(HttpMethod method, Uri uri, string json = null)
        {
            if (!string.IsNullOrEmpty(AccessAuthentication.jwtToken))
                uri = new Uri($"{uri}?access_token={AccessAuthentication.jwtToken}");
            HttpClient client = new HttpClient();
            HttpRequestMessage message = new HttpRequestMessage(method, uri);
            message.Headers.Add("user-agent", "firebase-net/0.2");
            if (json != null)
                message.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return client.SendAsync(message);
        }

        public FirebaseOperations(string baseURL)
        {
            this.RootNode = baseURL;
        }
        public FirebaseOperations(string baseURL, string pathToJSONKey, params string[] scopes)
        {
            this.RootNode = baseURL;
            AccessAuthentication.GenenateAccessToken(pathToJSONKey, scopes);
        }
        //Returns the node path 
        private string RootNode { get; set; }

        public FirebaseOperations Node(string node)
        {
            if (node.Contains("/"))
            {
                throw new FormatException("Node must not contain '/', use NodePath instead.");
            }
            return new FirebaseOperations(this.RootNode + '/' + node);
        }
        //Gets details from a node from the firebase database
        public FirebaseResponse Get(string rootNode)
        {
            return new FirebaseRequest(HttpMethod.Get, rootNode).Execute();
        }
        //Updates a node from the firebase database
        public FirebaseResponse Patch(string rootNode, string jsonData)
        {
            return new FirebaseRequest(new HttpMethod("PATCH"), rootNode, jsonData).Execute();
        }
        //Deleted a node from the firebase database
        public FirebaseResponse Delete(string rootnode)
        {
            return new FirebaseRequest(HttpMethod.Delete, rootnode).Execute();
        }
    }

    class AccessAuthentication
    {
        public static string jwtToken { get; set; }

        private static string accessKeyFilePath;

        private static string[] scopes;

        private static async Task<string> GetAccessTokenFromJSONKeyAsync(string accessKeyFilePath, params string[] scopes)
        {
            using (var stream = new FileStream(accessKeyFilePath, FileMode.Open, FileAccess.Read))
            {
                // Gets the Access Token
                return await GoogleCredential.FromStream(stream).CreateScoped(scopes).UnderlyingCredential.GetAccessTokenForRequestAsync();
            }
        }

        public static void GenenateAccessToken(string accessKeyFilePath, params string[] scopes)
        {
            try
            {
                AccessAuthentication.accessKeyFilePath = accessKeyFilePath;
                AccessAuthentication.scopes = (scopes.Length == 0) ? new string[] { "https://www.googleapis.com/auth/firebase", "https://www.googleapis.com/auth/userinfo.email" } : scopes;
                jwtToken = GetAccessTokenFromJSONKeyAsync(accessKeyFilePath, AccessAuthentication.scopes).Result;
            }
            catch (Exception)
            {
                throw new InvalidOperationException("Unauthorised Access! Given JWT token is invalid.");
            }
        }

        public static void RefreshToken()
        {
            if (!string.IsNullOrEmpty(jwtToken))
                jwtToken = GetAccessTokenFromJSONKeyAsync(accessKeyFilePath, scopes).Result;
            else
                throw new InvalidOperationException("Unauthorised Access! Given JWT token is invalid.");
        }
    }
}