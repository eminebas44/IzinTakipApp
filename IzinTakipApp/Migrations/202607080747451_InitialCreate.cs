namespace IzinTakipApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.IzinTalepleri",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        KullaniciID = c.Int(nullable: false),
                        BaslangicTarihi = c.DateTime(nullable: false),
                        BitisTarihi = c.DateTime(nullable: false),
                        ToplamGun = c.Int(nullable: false),
                        Durum = c.String(nullable: false, maxLength: 50),
                        Aciklama = c.String(maxLength: 500),
                        OlusturulmaTarihi = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Users", t => t.KullaniciID, cascadeDelete: true)
                .Index(t => t.KullaniciID);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        ID = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 100),
                        Email = c.String(nullable: false, maxLength: 100),
                        PasswordHash = c.String(nullable: false),
                        Role = c.String(nullable: false, maxLength: 50),
                        IseGirisTarihi = c.DateTime(nullable: false),
                        KalanYillikIzin = c.Int(nullable: false),
                        ManagerID = c.Int(),
                    })
                .PrimaryKey(t => t.ID)
                .ForeignKey("dbo.Users", t => t.ManagerID)
                .Index(t => t.ManagerID);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.IzinTalepleri", "KullaniciID", "dbo.Users");
            DropForeignKey("dbo.Users", "ManagerID", "dbo.Users");
            DropIndex("dbo.Users", new[] { "ManagerID" });
            DropIndex("dbo.IzinTalepleri", new[] { "KullaniciID" });
            DropTable("dbo.Users");
            DropTable("dbo.IzinTalepleri");
        }
    }
}
