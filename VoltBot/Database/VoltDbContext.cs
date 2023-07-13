using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using VoltBot.Database.Entities;

namespace VoltBot.Database
{
    internal class VoltDbContext : DbContext
    {
        public VoltDbContext() { Database.EnsureCreated(); }

        public DbSet<GuildSettings> GuildSettings { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string databaseDirectoryPath = Path.Combine(Program.Directory, "database");
            if (!Directory.Exists(databaseDirectoryPath))
                Directory.CreateDirectory(databaseDirectoryPath);
            optionsBuilder.UseSqlite($"Data Source={Path.Combine(databaseDirectoryPath, "volt.db")}");
        }
    }
}