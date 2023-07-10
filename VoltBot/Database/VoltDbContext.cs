using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoltBot.Database.Entities;

namespace VoltBot.Database
{
    internal class VoltDbContext :DbContext
    {
        public DbSet<GuildSettings> GuildSettings { get; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)=>optionsBuilder.UseSqlite("Data Source=volt.db");   
    }
}
