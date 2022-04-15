using System.Text;

namespace Scorpion.Crypto
{    public partial class Cryptographer
    {
        public string To_String(byte[] byt)
        {
            return Encoding.Default.GetString(byt);
        }
    }

    public static class Encoder
    {
        public static string encodebase64string(string to_encode)
        {
            return System.Convert.ToBase64String(Encoding.UTF8.GetBytes(to_encode));
        }

        public static string decodebase64string(string to_decode)
        {
            return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(to_decode));
        }
    }
}