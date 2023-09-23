using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SelenicSparkApp.Models;

namespace SelenicSparkApp.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

        public DbSet<SelenicSparkApp.Models.Post> Post { get; set; }

        public DbSet<SelenicSparkApp.Models.Comment> Comment { get; set; }
        
        public DbSet<SelenicSparkApp.Models.IdentityUserExpander> IdentityUserExpander { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Configure binding between IdentityUser and IdentityUserExpander
            // NO you can't move it to 'Program.cs' script
            builder.Entity<IdentityUser>()
                .HasOne<IdentityUserExpander>()
                .WithOne()
                .HasForeignKey<IdentityUserExpander>(u => u.UID)
                .IsRequired();
        }
    }
}
