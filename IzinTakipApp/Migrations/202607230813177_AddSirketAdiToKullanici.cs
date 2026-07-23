namespace IzinTakipApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSirketAdiToKullanici : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "SirketAdi", c => c.String(maxLength: 150));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "SirketAdi");
        }
    }
}
