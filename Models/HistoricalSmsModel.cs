namespace API_PAYSIM.Models
{
    public class HistoricalSmsModel
    {
        public Guid Id { get; set; }
        public String? Id_payement {  get; set; }
        public String? BuyerNumber {  get; set; }
        public String? BuyerName { get; set; }
        public String? Reference { get; set; }
        public decimal? Price { get; set; }
    }
}
