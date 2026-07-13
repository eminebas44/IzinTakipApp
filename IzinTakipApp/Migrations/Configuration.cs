namespace IzinTakipApp.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<IzinTakipApp.Data.AppDbContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
            // Hata veren AutomaticDataLossAllowed satırını tamamen kaldırdık!
        }

        protected override void Seed(IzinTakipApp.Data.AppDbContext context)
        {
        }
    }
}