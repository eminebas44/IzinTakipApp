namespace IzinTakipApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateIzinTalebiEnum : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.IzinTalepleri", "Kategori", c => c.Int(nullable: false));
            AddColumn("dbo.IzinTalepleri", "SureTipi", c => c.Int(nullable: false));
            AlterColumn("dbo.IzinTalepleri", "ToplamGun", c => c.Double(nullable: false));
            AlterColumn("dbo.IzinTalepleri", "Durum", c => c.String(maxLength: 50));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.IzinTalepleri", "Durum", c => c.String(nullable: false, maxLength: 50));
            AlterColumn("dbo.IzinTalepleri", "ToplamGun", c => c.Int(nullable: false));
            DropColumn("dbo.IzinTalepleri", "SureTipi");
            DropColumn("dbo.IzinTalepleri", "Kategori");
        }
    }
}
