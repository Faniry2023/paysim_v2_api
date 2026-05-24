namespace API_PAYSIM.Helpers.PayHelper
{
    public class ContinuationPaymentHelper
    {
        public String? IdPayment {  get; set; }
        public String? IdProject {  get; set; }
        //this is the user's site
        public String? IdCustomer {  get; set; }
        public String? Reason {  get; set; }
        public String? Number {  get; set; }
        public decimal Price {  get; set; }
        public String? ActionKey {  get; set; }
    }
}
