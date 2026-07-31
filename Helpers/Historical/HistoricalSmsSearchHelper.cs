namespace API_PAYSIM.Helpers.Historical
{
    public class HistoricalSmsSearchHelper
    {
        public String? BuyerName { get; set; }
        public String? BuyerNumber { get; set; }
        public String? Reference { get; set; }
        public decimal? Price { get; set; }
        public String? Reason { get; set; }
        public DateOnly? Created_at { get; set; }
        public int Page {  get; set; }
        public int Step {  get; set; }
    }
}
