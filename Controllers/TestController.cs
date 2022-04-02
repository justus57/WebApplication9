using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;


namespace WebApplication9.Controllers
{
    
    public class TestController : Controller
    {
        static string path = GetDLLPath() + "\\config.xml";
        static string certPath = "SafaricomMaster.p12";
        static X509Certificate2 cert = new X509Certificate2(System.IO.File.ReadAllBytes(certPath), "1234567890");
        public static string umbrellaResponse { get; private set; }
        // GET: Test
        [System.Web.Http.Route("apisafumbrella")]
        public string Test([FromBody] Payload payload)
        {
            var Amount = payload.Amount;
            var UUID = payload.UUID;
            var MSISDN = payload.MSISDN;
            var Shortcode = payload.Shortcode;
            var BankName = payload.BankName;
            var AccountNumber = payload.AccountNumber;
            var AdditionalInfo = payload.AdditionalInfo;

            string Safreponse = null;
            try
            {
                var command = new Payload()
                {
                    UUID = UUID,
                    Timestamp = string.Format("{0:yyyy-MM-ddTHH:mm:ss.FF}", DateTime.UtcNow),
                    Amount = Amount,
                    MSISDN = MSISDN,
                    Shortcode = Shortcode,
                    BankName = BankName,
                    AccountNumber = AccountNumber,
                    AdditionalInfo = AdditionalInfo
                };
                string jsonbody = JsonConvert.SerializeObject(command, Formatting.Indented);

                X509CertificateCollection clientCerts = new X509CertificateCollection();
                clientCerts.Add(cert);

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) =>
                {
                    bool valid = (sslPolicyErrors & SslPolicyErrors.RemoteCertificateNotAvailable) != SslPolicyErrors.RemoteCertificateNotAvailable;
                    return valid;
                };
                var client = new RestClient(GetConfigData("Endpoint"));
                client.Timeout = -1;
                client.ClientCertificates = clientCerts;
                var request = new RestRequest(Method.POST);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("X-Correlation-ConversationID", "X-Correlation-ConversationID");
                request.AddHeader("Username", GetConfigData("Username"));
                request.AddHeader("Password", GetConfigData("Password"));
                var body = jsonbody;
                request.AddParameter("application/json", body, ParameterType.RequestBody);
                IRestResponse response = client.Execute(request);
                Safreponse = response.Content;

                var Json = Newtonsoft.Json.JsonConvert.DeserializeObject<Rootobject>(Safreponse);
                var statusCode = Json.Response.ResponseCode;
                var statusMessage = Json.Response.ResponseMessage;

                umbrellaResponse = statusCode.ToString() + "|" + statusMessage.ToString();

            }
            catch (Exception es)
            {
                umbrellaResponse = "99|" + es.Message;
                WriteLog(es.Message);
                string innerEx = "";
                if (es.InnerException != null)
                    innerEx = es.InnerException.ToString();
            }
            return umbrellaResponse;
        }
        public static string GetConfigData(string XMLNode)
        {
            string value = "";
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(path);
                XmlNode WebServiceNameNode = doc.GetElementsByTagName(XMLNode)[0];

                value = WebServiceNameNode.InnerText;
            }
            catch (Exception es)
            {
                WriteLog(es.Message);
            }
            return value;
        }
        public static void WriteLog(string text)
        {
            try
            {
                //set up a filestream
                string strPath = @"C:\Logs\SafumbrellaAPI";
                string fileName = DateTime.Now.ToString("MMddyyyy") + "_logs.txt";
                string filenamePath = strPath + '\\' + fileName;
                Directory.CreateDirectory(strPath);
                FileStream fs = new FileStream(filenamePath, FileMode.OpenOrCreate, FileAccess.Write);
                //set up a streamwriter for adding text
                StreamWriter sw = new StreamWriter(fs);
                //find the end of the underlying filestream
                sw.BaseStream.Seek(0, SeekOrigin.End);
                //add the text
                sw.WriteLine(DateTime.Now.ToString() + ": " + text);
                //add the text to the underlying filestream
                sw.Flush();
                //close the writer
                sw.Close();
            }
            catch (Exception ex)
            {
                //throw;
                ex.Data.Clear();
            }
        }
        private static string GetDLLPath()
        {
            string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return assemblyFolder;
        }
    }
    public class Payload
    {
        public string UUID { get; set; }
        public string Timestamp { get; set; }
        public string Amount { get; set; }
        public string MSISDN { get; set; }
        public string Shortcode { get; set; }
        public string BankName { get; set; }
        public string AccountNumber { get; set; }
        public string AdditionalInfo { get; set; }
        //public string Content_Length { get; set; }
        //public string Content_Language { get; set; }
        //public string Accept_Encoding { get; set; }
        //public string User_Agent { get; set; }
    }

    public class Rootobject
    {
        public Response Response { get; set; }
    }

    public class Response
    {
        public string ResponseCode { get; set; }
        public string ResponseMessage { get; set; }
    }
}