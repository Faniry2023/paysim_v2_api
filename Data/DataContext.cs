using API_PAYSIM.Helpers.KotlinTestHelper;
using API_PAYSIM.Models;
using Microsoft.EntityFrameworkCore;

namespace API_PAYSIM.Data
{
    public class DataContext(DbContextOptions<DataContext> options) : DbContext(options)
    {
        public DbSet<ConfidentialityModel> Confidentiality { get; set; }
        public DbSet<DeveloperModel> Developer { get; set; }
        public DbSet<HistoricalModel> Historical { get; set; }
        public DbSet<PaymentModel> Payment {  get; set; }
        public DbSet<ProjectModel> Project { get; set; }
        public DbSet<UserModel> User { get; set; }
        public DbSet<UserTestKotlin> UserKotlin { get; set; }
    }
}
