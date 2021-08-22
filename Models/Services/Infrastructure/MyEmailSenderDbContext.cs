using BackgroundEmailSenderSample.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BackgroundEmailSenderSample.Models.Services.Infrastructure
{
    public class MyEmailSenderDbContext : DbContext
    {
        public MyEmailSenderDbContext(DbContextOptions<MyEmailSenderDbContext> options) : base(options)
        {
        }

        public virtual DbSet<Email> Emails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Email>(entity =>
            {
                entity.ToTable("EmailMessage");
            });
        }
    }
}