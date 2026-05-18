using System.ComponentModel.DataAnnotations;

namespace API_PAYSIM.Models
{
    public class PaymentModel
    {
        [Key]
        public Guid IdPayment {  get; set; }
        public String? IdProject {  get; set; }
        //this is the id commande
        public String? ActionKey {  get; set; }
        public String? Reason {  get; set; }
        public String? Number {  get; set; }
        public decimal Price {  get; set; }
    }
}
