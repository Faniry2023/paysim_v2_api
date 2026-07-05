namespace API_PAYSIM.Models
{
    public class HistoricalSmsModel
    {
        public Guid Id { get; set; }
        public String? Id_developer {  get; set; }
        public String? Id_user {  get; set; }
        public String? Name_customer {  get; set; }
        public String? Id_payement {  get; set; }
        public String? BuyerNumber {  get; set; }
        public String? BuyerName { get; set; }
        public String? Reference { get; set; }
        public decimal? Price { get; set; }
        public decimal? Balance_seller {  get; set; }
        public DateTime Created_at { get; set; }
    }
}
