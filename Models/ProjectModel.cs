namespace API_PAYSIM.Models
{
    public class ProjectModel
    {
        public Guid Id { get; set; }
        public String? IdDeveloper {  get; set; }
        public String? ProjectName {  get; set; }
        public String? Link {  get; set; }
        public String? ApiKey {  get; set; }
        public String? ApiKeyPrefix {  get; set; }
    }
}
