namespace API_PAYSIM.Models
{
    public class UserModel
    {
        public Guid Id { get; set; }
        public String? IdConfidentiality {  get; set; }
        public String? FirstName {  get; set; }
        public String? LastName { get; set; }
        public String? Address {  get; set; }
        public DateTime? Birthday {  get; set; }
        public bool AccountOk { get; set; }
    }

    
}
