using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace GetPS3UpdatePackages {
    class Program {
        // Regions and formats for PS3 games
        readonly static List<string> regions = new List<string>() { "U", "E", "J", "A" };
        readonly static List<string> formats = new List<string>() { "BC", "BL", "NP" };
        static void Main(string[] args) {
            Console.WriteLine("Welcome to the PS3 Update Package Downloader!");

            // Wait until the user inputs a valid PS3 game code
            string input = ""; 
            bool entered = false;
            while(!entered) {
                Console.WriteLine("Please enter the Game ID:");

                // Read and parse user input
                input = Console.ReadLine();
                string format = input.Substring(0, 2);
                string region = input.ToCharArray()[2].ToString();

                // Validate that the game code is correctly formatted
                if (regions.Contains(region) && formats.Contains(format)) entered = true;
                else Console.WriteLine("Invalid Game ID!");
            }

            // Attempt to download the latest update .pkg file
            bool result = SelectAndDownload(input);
            if (result) Console.WriteLine("The latest update package file has been downloaded successfully!");
            else Console.WriteLine("The update package file was not downloaded successfully. Please refer to the log above to see which ones failed.");

            // Wait until the user presses enter to proceed
            Console.WriteLine("\n" + "Please press Enter / Return to close this application.");
            Console.ReadLine();
        }
        static bool SelectAndDownload(string gameId) {
            string connectionString = "https://a0.ww.np.dl.playstation.net/tpl/np/" + gameId + "/" + gameId + "-ver.xml";
            string packageURL = "";

            // Set up the web request for retrieving game data XML
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(connectionString);
            request.Method = "GET";
            ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true); //Trust all certificates
            ServicePointManager.ServerCertificateValidationCallback = ((sender, cert, chain, errors) => cert.Subject.Contains("YourServerName")); // trust sender
            ServicePointManager.ServerCertificateValidationCallback += new RemoteCertificateValidationCallback(ValidateRemoteCertificate); // validate cert by calling a function

            try {
                // Attempt to retrieve game data XML
                WebResponse webResponse = request.GetResponse();
                Stream webStream = webResponse.GetResponseStream();

                XmlTextReader reader = new XmlTextReader(webStream);
                packageURL = ReadXML(reader);
                reader.Close();
            } catch (Exception e) {
                Console.WriteLine("-----------------");
                Console.WriteLine("ERROR RETRIEVING GAME INFO: " + e.Message);
                return false;
            }

            if (String.IsNullOrEmpty(packageURL)) return false;
            else {
                // Parse the link to the latest game update file from the game data XML
                string[] splitted = packageURL.Split('/');
                string fileName = splitted[splitted.Length - 1];

                // Attempt to download the latest game update .pkg file
                Console.WriteLine("Downloading the update to " + Directory.GetCurrentDirectory() + fileName);
                using (var client = new WebClient()) {
                    try {
                        client.DownloadFile(packageURL, "" + fileName);
                    } catch (Exception e) {
                        Console.WriteLine("-----------------");
                        Console.WriteLine("ERROR DOWNLOADING UPDATE PACKAGE: " + e.Message);
                        return false;
                    }
                }
                return true;
            }
        }
        public static string ReadXML(XmlTextReader reader) {
            string output = "";
            while (reader.Read()) {
                if(reader.NodeType == XmlNodeType.Element && reader.Name == "package") output = reader.GetAttribute("url");
            }
            return output;
        }
        private static bool ValidateRemoteCertificate(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors policyErrors) {
            return true;
        }
    }
}
