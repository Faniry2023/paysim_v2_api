namespace API_PAYSIM.Helpers
{
    public class GenerateApiKeyHelper
    { 
        public static string GenerateApiKey()
        {
            String? alphabet = "abcdefghijklmnopqrstuvwxyz";
            String? character = "&#'([-|_ç@])=}+£$ù%!§:/;.,?";
            string result = string.Empty;

            //1: un alphabet minuscule
            //2: un alphabet majuscule
            //3: un nombre
            Random random = new Random();
            for(int i = 1; i <= 26; i++)
            {
                if(i == 9 || i == 18)
                {
                    result += "-";
                }
                else
                {
                    int choice = random.Next(1, 5);
                    result += choice switch
                    {
                        1 => alphabet[random.Next(0, alphabet.Length)],
                        2 => alphabet[random.Next(0, alphabet.Length)].ToString().ToUpper(),
                        3 => character[random.Next(0, character.Length)],
                        4 => random.Next(0, 10),
                        _ => "@"
                    };
                }
                
            }
            return result;
        }

        public static String GenerateReason()
        {
            Random random = new Random();
            String? alphabet = "abcdefghijklmnopqrstuvwxyz";
            String result = string.Empty;
            for(int i = 1; i <= 20; i++)
            {
                if(i == 5 || i == 10 || i == 15)
                {
                    result += "-";
                }
                else
                {
                    int choice = random.Next(1, 4);
                    result += choice switch
                    {
                        1 => alphabet[random.Next(0, alphabet.Length)],
                        2 => alphabet[random.Next(0, alphabet.Length)].ToString().ToUpper(),
                        3 => random.Next(0, 10),
                        _ => "N/A"
                    };
                }
                
            }
            return result;
        }
    }
}
