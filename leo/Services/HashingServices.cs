using System.Security.Cryptography;
using System.Text;

namespace leo.Services
{
    public class HashingServices
    {
        public static string HashData(string UserData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(UserData);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                byte[] shortenedHash = new byte[16];
                Array.Copy(hashBytes, shortenedHash, 16);

                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < shortenedHash.Length; i++)
                {
                    builder.Append(shortenedHash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        internal static string HashData(object password)
        {
            throw new NotImplementedException();
        }

    }
}