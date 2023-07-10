using Microsoft.EntityFrameworkCore;
using VoltBot.Database.Entities;

namespace VoltBot.Database
{
    internal class VoltDbContext : DbContext
    {
        public VoltDbContext() { Database.EnsureCreated(); }

        public DbSet<GuildSettings> GuildSettings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseSqlite("Data Source=volt.db");
    }
}