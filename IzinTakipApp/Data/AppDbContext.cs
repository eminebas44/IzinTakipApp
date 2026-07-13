using System.Data.Entity;
using IzinTakipApp.Models;

namespace IzinTakipApp.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext() : base("name=IzinTakipBaglantisi")
        {
            Database.SetInitializer<AppDbContext>(null);
        }

        public DbSet<Kullanici> Users { get; set; }
        public DbSet<IzinTalebi> IzinTalepleri { get; set; }
    }
}