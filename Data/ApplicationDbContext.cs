using Microsoft.EntityFrameworkCore;
using greenSpotApi.Models;
namespace greenSpotApi.Data
    
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        
        public DbSet<User> Users { get; set; }
    }
}
