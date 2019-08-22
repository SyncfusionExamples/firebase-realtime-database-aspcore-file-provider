
namespace FirebaseHelper
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net.Http;
    using System.Net;
    using Newtonsoft.Json.Linq;

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
                return new FirebaseResponse(false, "Provided Firebase path is not a valid HTTP/S URL");
            }
            string json = null;
            if (this.JSON != null)
            {
                if (!FirebaseOperations.TryParseJSON(this.JSON, out json))
                {
                    return new FirebaseResponse(false, string.Format("Invalid JSON : {0}", json));
                }
            }

            Task<HttpResponseMessage> response = FirebaseOperations.RequestHelper(this.Method, requestURI, json);
            response.Wait();
            HttpResponseMessage result = response.Result;
            var firebaseResponse = new FirebaseResponse()
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

        public static bool TryParseJSON(string inJSON, out string output)
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
            var client = new HttpClient();
            var msg = new HttpRequestMessage(method, uri);
            msg.Headers.Add("user-agent", "firebase-net/0.2");
            if (json != null)
            {
                msg.Content = new StringContent(json, UnicodeEncoding.UTF8, "application/json");
            }
            return client.SendAsync(msg);
        }

        public FirebaseOperations(string baseURL)
        {
            this.RootNode = baseURL;
        }
        public FirebaseOperations(string baseURL, string pathToJSONKey, params string[] scopes)
        {
            this.RootNode = baseURL;
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
}