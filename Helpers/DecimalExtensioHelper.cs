namespace API_PAYSIM.Helpers
{
    public static class DecimalExtensioHelper
    {
        public static decimal SansVirgule(this decimal value)
        {
            return Math.Truncate(value);
        }
    }
}
