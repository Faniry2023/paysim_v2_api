namespace API_PAYSIM.Models
{
    public class HistoricalModel
    {
        public Guid Id { get; set; }
        //this is the id if the site's client
        public String? IdCustomer {  get; set; }
        public String? IdPayment {  get; set; }
        //this is a id commande
        public String? IdDeveloper {  get; set; }
        public String? Name_developer {  get; set; }
        public String? Reference {  get; set; }
        public String? Reason {  get; set; }
        public decimal Price {  get; set; }
        public String? NumberDeveloper {  get; set; }
        public DateTime Created_at { get; set; }
    }
}
