using System;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using ScorpionConsoleReadWrite;

namespace ScorpionNetworkDriver
{
    public class ScorpionDriver: IDisposable
    {
      internal static ScorpionDriverTCP SCDT;
      
      public void Dispose()
      {
          GC.SuppressFinalize(this);
          return;
      }

      public ScorpionDriver(string host, int port)
      {
        SCDT = new ScorpionDriverTCP(host, port);
        return;
      }

      public string get(string DB, string TAG, string SUBTAG, string session, bool is_css, bool is_script)
      {
        string response = null;
        string query = string.Empty;

        if(SCDT.connect())
        {
          query = NetworkEngineFunctions.buildQuery(DB, TAG, SUBTAG, session, (is_css == true || is_script == true ? NetworkEngineFunctions.types["false"] : NetworkEngineFunctions.types["true"]));
          ConsoleWrite.writeDebug("SENDING GET QUERY: ", query);
          response = SCDT.get(query);//NetworkEngineFunctions.buildQuery(DB, TAG, SUBTAG, session, (is_css == true || is_script == true ? false : true)));
        }
        try
        {
          return NetworkEngineFunctions.replaceApiResponse(response)["data"];
        }
        catch{ return null; }
      }

      public string set(string DB, string TAG, string SUBTAG, string DATA, string session)
      {
        string response = null;
        string query = string.Empty;

        if(SCDT.connect())
        {
          query = NetworkEngineFunctions.buildData(DB, TAG, SUBTAG, session, DATA);
          ConsoleWrite.writeDebug("SENDING SET QUERY: ", query);
          response = SCDT.set(query);
        }
        try
        {
          return NetworkEngineFunctions.replaceApiResponse(response)["data"];
        }
        catch{ return null; }
      }

      public string createResponse(string data, string session, bool error)
      {
        return NetworkEngineFunctions.buildApiResponse(data, session, error);
      }
    }

    public static class NetworkEngineFunctions
    {
        private static readonly string[] S_ESCAPE_SEQUENCES = {};

        private static readonly Dictionary<string, string[]> api = new Dictionary<string, string[]> 
        {
            { "scorpion", new string[]{ "{&scorpion}", "{&/scorpion}" } },
            { "database", new string[]{ "{&database}", "{&/database}" } },
            { "type", new string[] {"{&type}", "{&/type}" } },
            { "tag", new string[] {"{&tag}", "{&/tag}" } },
            { "subtag", new string[] {"{&subtag}", "{&/subtag}" } },
            { "data", new string[] {"{&data}", "{&/data}" } },
            { "status", new string[] {"{&status}", "{&/status}" } },
            { "session", new string[] {"{&session}", "{&/session}" } },
            { "includedata", new string[] {"{&includedata}", "{&/includedata}" } },
        };

        public static readonly Dictionary<string, string> api_requests = new Dictionary<string, string>
        {
          { "get", "get" },
          { "set", "set" },
          { "delete" , "delete" },
          { "response", "response" }
        };

        private static readonly Dictionary<string, string> api_result = new Dictionary<string, string>
        {
          { "ok", "ok" },
          { "error", "error" }
        };

        public static readonly Dictionary<string, string> types = new Dictionary<string, string>
        {
          { "none", "none" },
          { "true", "true" },
          { "false", "false" },
        };

        public static string cleanTagsFromDataString(string data)
        {
          foreach(KeyValuePair<string, string[]> tags in api)
          {
            data = data.Replace(tags.Value[0], string.Empty);
            data = data.Replace(tags.Value[1], string.Empty);
          }
          return data;
        }

        public static Dictionary<string, string> replaceApi(string Scorp_Line)
        {
            //Scorp_Line = Scorp_Line.Remove(0, Scorp_Line.IndexOf(api["scorpion"][0], StringComparison.CurrentCulture));
            if ((Scorp_Line = cleanScorpionMainTag(Scorp_Line)) != null) /*Scorp_Line.Contains(api["scorpion"][0]) && Scorp_Line.Contains(api["scorpion"][1]))*/
            {
                //Split other elements
                //Get the app
                string[] db, tag, subtag, type, session, includedata, data;
                type = Scorp_Line.Split(api["type"], StringSplitOptions.RemoveEmptyEntries);
                db = Scorp_Line.Split(api["database"], StringSplitOptions.RemoveEmptyEntries);
                tag = Scorp_Line.Split(api["tag"], StringSplitOptions.RemoveEmptyEntries);
                data = Scorp_Line.Split(api["data"], StringSplitOptions.RemoveEmptyEntries);
                subtag = Scorp_Line.Split(api["subtag"], StringSplitOptions.RemoveEmptyEntries);
                session = Scorp_Line.Split(api["session"], StringSplitOptions.RemoveEmptyEntries);
                includedata = Scorp_Line.Split(api["includedata"], StringSplitOptions.RemoveEmptyEntries);
                return new Dictionary<string, string> { 
                  { "type", type[1] },
                  { "db", db[1] },
                  { "tag", tag[1] },
                  { "subtag", (subtag.Length > 0 ? subtag[1] : types["none"]) },
                  { "session", session[1] },
                  { "includedata", (includedata.Length > 0 ? includedata[1] : null) },
                  { "data", data[1] },
                };
            }
            return null;
        }

        public static Dictionary<string, string> replaceApiResponse(string Scorp_Line)
        {
          if ((Scorp_Line = cleanScorpionMainTag(Scorp_Line)) != null)
          {
            //Get response data from a response
            string[] data, status, type, session, includedata;
            type = Scorp_Line.Split(api["type"], StringSplitOptions.RemoveEmptyEntries);
            data = Scorp_Line.Split(api["data"], StringSplitOptions.RemoveEmptyEntries);
            status = Scorp_Line.Split(api["status"], StringSplitOptions.RemoveEmptyEntries);
            session = Scorp_Line.Split(api["session"], StringSplitOptions.RemoveEmptyEntries);
            includedata = Scorp_Line.Split(api["includedata"], StringSplitOptions.RemoveEmptyEntries);
            return new Dictionary<string, string> {
              { "type", type[1] },
              { "data", data[1] },
              { "status", status[1] },
              { "session", session[1] },
            };
          }
          return null;
        }

        public static string buildApiResponse(string data, string session, bool error)
        {
            if(!error)
                return api["scorpion"][0] + api["type"][0] + api_requests["response"] + api["type"][1] + api["session"][0] + session + api["session"][1] + api["data"][0] + data + api["data"][1] + api["status"][0] + api_result["ok"] + api["status"][1] + api["scorpion"][1];
            return api["scorpion"][0] + api["type"][0] + api_requests["response"] + api["type"][1] + api["session"][0] + session + api["session"][1] + api["data"][0] + data + api["data"][1] + api["status"][0] + api_result["error"] + api["status"][1] + api["scorpion"][1];
        }

        //Builds a query for a network request
        public static string buildQuery(string DB, string TAG, string SUBTAG, string session, string include_data)
        {
            return api["scorpion"][0] + api["type"][0] + api_requests["get"] + api["type"][1] + api["database"][0] + DB + api["database"][1] + api["tag"][0] + TAG + api["tag"][1] + api["subtag"][0] + SUBTAG + api["subtag"][1] + api["session"][0] + session + api["session"][1] + api["scorpion"][1] + api["includedata"][0] + include_data + api["includedata"][1] + api["data"][0] + types["none"] + api["data"][1] + api["scorpion"][1];
        }

        public static string buildData(string DB, string TAG, string SUBTAG, string session, string data)
        {
            return api["scorpion"][0] + api["type"][0] + api_requests["set"] + api["type"][1] + api["database"][0] + DB + api["database"][1] + api["tag"][0] + TAG + api["tag"][1] + api["subtag"][0] + SUBTAG + api["subtag"][1] + api["session"][0] + session + api["session"][1] + api["scorpion"][1] + api["data"][0] + data + api["data"][1] + api["includedata"][0] + types["false"] + api["includedata"][1] + api["scorpion"][1];
        }

        public static string replaceTelnet(string Scorp_Line)
        {
            return Scorp_Line.Replace("\r\n", "").Replace("959;1R", "");
        }

        //Remove the main enclosing tags from a scorpion request
        private static string cleanScorpionMainTag(string Scorp_Line)
        {
            if (Scorp_Line.Contains(api["scorpion"][0]) && Scorp_Line.Contains(api["scorpion"][1]))
              return Scorp_Line.Remove(0, Scorp_Line.IndexOf(api["scorpion"][0], StringComparison.CurrentCulture));
            return null;
        }
    }

    class ScorpionDriverTCP
    {
      private static TcpClient scorpion_client;
      private ScorpionRSAMin rSAMin;
      private static int PORT = 0;
      private static string HOST;

      internal static readonly string base_path     = SquirrelDefaultPaths.SquirrelPaths.main_user_path;
      public static string main_user_aes_path_file  = SquirrelDefaultPaths.SquirrelPaths.main_user_aes_path_file;
      internal readonly string private_rsa_key      = SquirrelDefaultPaths.SquirrelPaths.main_user_rsa_path + "/client/private-key.pem";
      internal readonly string public_rsa_key       = SquirrelDefaultPaths.SquirrelPaths.main_user_rsa_path + "/server/public-key.pem";

      public ScorpionDriverTCP(string host, int port)
      {
        //Check if the AES decryption key exists
        if(!File.Exists(main_user_aes_path_file))
        {
          ScorpionConsoleReadWrite.ConsoleWrite.writeError("No AES decryption key found at: ", main_user_aes_path_file, ". Please make sure to create one with the scorpion command 'generateaeskey'. The file will be generated to the correct path if both Scorpion IEE and this program are used on the same server, else import to the new system on ~/Scorpion/AES/aes.ky");
          Environment.Exit(-1);
        }

        HOST = host;
        PORT = port;

        //Check if the RSA encryption keys exist
        if(!File.Exists(private_rsa_key) || !File.Exists(public_rsa_key))
        {
            ConsoleWrite.writeError("The provided RSA public key: ", public_rsa_key, ", or private key: ", private_rsa_key, " could not be found");
            return;
        }

        //Static file paths only
        rSAMin = new ScorpionRSAMin(public_rsa_key, public_rsa_key);
        return;
      }

      public string get(string data)
      {
        return request(data);
      }

      public string set(string data)
      {
        return request(data);
      }

      private string request(string message)
      {
        //Translate the passed message into UTF8 and store it as a Byte array.
        Byte[] data = System.Text.Encoding.UTF8.GetBytes(message);
        NetworkStream stream = scorpion_client.GetStream();

        // String to store the response in an UTF8 representation.
        string responseData = String.Empty;

        //DEBUG!!!!
        Console.WriteLine("DATA {0} : ", message);

        //Send the message to the connected TcpServer.
        //RSA encrypt using the public key
        data = rSAMin.encrypt(data);
        stream.Write(data, 0, data.Length);

        // Buffer to store the response bytes. set ti 'RSA.MAXVALUE'
        int dat_size = 0;
        data = new Byte[dat_size];

        //Create temporary byte to store read bytes in
        int tmpb = 0x00; int n = 0;
        while((tmpb = stream.ReadByte()) != -1)
        {
          //Expand array if not long enough
          if((data.Length) == n)
          {
            dat_size += 1;
            Array.Resize<byte>(ref data, dat_size);
          }
          data[n] = (byte)tmpb;
          tmpb = 0x00;
          n++;
        }

        //Decrypt using the private RSA key and get string from bytes
        using (Aes myAes = Aes.Create())
        {
          //Must be a 16 byte key
          byte[] key = ScorpionAES.ScorpionAESInHouse.importKey(main_user_aes_path_file);
          try
          {
            myAes.Key = key;
            responseData = ScorpionAES.ScorpionAESInHouse.decrypt(data, myAes.Key, myAes.IV);
          }
          catch { Console.WriteLine("Unable to decrypt for: {0}", message); }
        }

        if(responseData.Length > 0)
        {
          if(!responseData.StartsWith("{&scorpion}{&type}"))
          {
            responseData = responseData.Remove(0, responseData.IndexOf("response"));
            responseData = String.Concat("{&scorpion}{&type}", responseData);
          }
        }

        stream.Flush();
        stream.Close();

        return responseData;
      }

      public bool connect()
      {
        try
        {
          // Create a TcpClient.
          // Note, for this client to work you need to have a TcpServer
          // connected to the same address as specified by the server, port
          // combination.
          scorpion_client = new TcpClient(HOST, PORT);
          return true;
        }
        catch (ArgumentNullException e)
        {
          Console.WriteLine("ArgumentNullException: {0}", e);
          return false;
        }
        catch (SocketException e)
        {
          Console.WriteLine("SocketException: {0}", e);
          return false;
        }
      }

      public void disconnect()
      {
          scorpion_client.Close();
      }
    }

    class ScorpionRSAMin
    {
      private string private_key_path = null;
      private string public_key_path = null;

      public ScorpionRSAMin(string public_key_path_, string private_key_path_)
      {
        //check files exist
        if(!File.Exists(public_key_path_) || !File.Exists(private_key_path_))
        {
          Console.WriteLine("No keys found, returning");
          return;
        }
        private_key_path = private_key_path_;
        public_key_path = public_key_path_;
      }

      public byte[] decrypt(byte[] data)
      {
          //return Rsa.Decrypt(private_key_path, data);
          using(var rsa = RSAOpenSsl.Create())
          {
            rsa.ImportFromPem(File.ReadAllText(private_key_path));
            return rsa.Decrypt(data, RSAEncryptionPadding.Pkcs1);
          }
      }

      public byte[] encrypt(byte[] data)
      {
          using(var rsa = RSAOpenSsl.Create())
          {
            rsa.ImportFromPem(File.ReadAllText(public_key_path));
            return rsa.Encrypt(data, RSAEncryptionPadding.Pkcs1);
          }
      }
    }
}