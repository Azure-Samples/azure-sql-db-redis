using System;
using Microsoft.EntityFrameworkCore;
using ReaderFunction.Models;

namespace ReaderFunction
{
    public class AppDbContext : DbContext
    {
        public DbSet<RankResponseModel> Companies { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options)
            :base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new RankResponseEntityConfiguration());
        }
    }
}

