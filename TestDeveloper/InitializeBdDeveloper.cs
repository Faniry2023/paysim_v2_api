using API_PAYSIM.Helpers;
using API_PAYSIM.Models;

namespace API_PAYSIM.TestDeveloper
{
    public class InitializeBdDeveloper
    {
        public ConfidentialityModel? Confidentiality { get; set; }
        public UserModel? User { get; set; }
        public DeveloperModel? Developer { get; set; }
        public ProjectModel? Project { get; set; }
        public String? apiKey {  get; set; }
    }
}
