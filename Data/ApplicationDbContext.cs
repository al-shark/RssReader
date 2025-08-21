using Microsoft.EntityFrameworkCore;
using RssReader.Models;

namespace RssReader.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Feed> Feeds { get; set; }
        public DbSet<Article> Articles { get; set; }
    }
}