using Microsoft.EntityFrameworkCore;
using MovieMunch.Context.Models;
using MovieMunch.EntityConfigurations;

namespace MovieMunch.Context
{
    public class MOMContext : DbContext
    {
        public MOMContext(DbContextOptions<MOMContext> options)
            : base(options)
        {
        }

        public DbSet<Tblusermaster> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply entity configurations
            modelBuilder.ApplyConfiguration(new TblusermasterConfiguration());
        }

    }
}
