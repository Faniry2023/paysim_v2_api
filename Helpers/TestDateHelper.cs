namespace API_PAYSIM.Helpers
{
    public class TestDateHelper
    {
        public static bool IsAnAdult(DateTime birthday)
        {
            DateTime now = DateTime.Today;

            int age = now.Year - birthday.Year;
            
            if(birthday.Date > now.AddYears(-age))
            {
                age--;
            }
            return (age < 18) ? false : true;
        }
    }
}
