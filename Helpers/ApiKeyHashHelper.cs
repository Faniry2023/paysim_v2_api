using System.Security.Cryptography;
using System.Text;

namespace API_PAYSIM.Helpers
{
    public static class ApiKeyHashHelper
    {
        //Génère une ApiKey aléatoire
        //Format : paysim_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx(256 bits)
        public static string GenerateApiKey()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return "paysim_" + Convert.ToHexString(bytes).ToLower();
        }

        //Hash irréversible de l'ApiKey avec sel
        //Le sel est aléatoire et stocké avec le hash (format: sel:hash)
        //Ainsi même deux ApiKey identiques donnent des hash différents

        public static string HashApiKey(string apiKey)
        {
            //Génère un sel aléatoire de 16 bytes
            var sel = RandomNumberGenerator.GetBytes(16);
            var selHex = Convert.ToHexString(sel).ToLower();

            // Hash = SHA256(sel + apiKey)
            var input = Encoding.UTF8.GetBytes(selHex + apiKey);
            var hash = SHA256.HashData(input);
            var hashHex = Convert.ToHexString(hash).ToLower();

            return $"{selHex}:{hashHex}";
        }

        //Vérifie si une ApiKey correspond au hash stocké
        //Utilisé dans DeveloperInformationSetup pour vérifier l'ApiKey
        public static bool VerifierApiKey(string apiKey, string hashStock)
        {
            try
            {
                var parties = hashStock.Split(':');
                if (parties.Length != 2) return false;

                var selHex = parties[0];
                var hashHex = parties[1];

                // Recalcule le hash avec le même sel
                var input = Encoding.UTF8.GetBytes(selHex + apiKey);
                var hash = SHA256.HashData(input);
                var hashRecalcule = Convert.ToHexString(hash).ToLower();

                //Comparaison en temps constant (évite timing attacks)

                return CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(hashRecalcule),
                    Encoding.UTF8.GetBytes(hashHex)
                );
            }
            catch {  return false; }
        }
    }
}
