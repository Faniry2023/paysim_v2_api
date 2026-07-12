namespace API_PAYSIM.Helpers.PayHelper
{
    public class SellerCheckHelper
    {

        public string? Reference {  get; set; }
        public string? ConnectionId {  get; set; }
        public string? IdDeveloper {  get; set; }
        public string? Reason {  get; set; }
        public decimal Price { get; set; }
        public string? BuyerNumber {  get; set; }
        public string? BuyerName {  get; set; }
        public decimal? SellerBalance {  get; set; }
    }
}
