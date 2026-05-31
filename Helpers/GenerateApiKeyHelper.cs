using System.Security.Cryptography;

namespace API_PAYSIM.Helpers
{
    public class GenerateApiKeyHelper
    { 
        public static string GenerateApiKey()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes)
                .Replace("+", "0")
                .Replace("/", "1")
                .Replace("=", "");
        }

        public static String GenerateReason()
        {
            Random random = new Random();
            String result = string.Empty;
            for(int i = 1; i <= 20; i++)
            {
                if(i == 5 || i == 10 || i == 15)
                {
                    result += "0";
                }
                else
                {
                    result += random.Next(0, 10);
                }
                
            }
            return result;
        }
    }
}
