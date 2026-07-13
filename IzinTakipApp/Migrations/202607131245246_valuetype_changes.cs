namespace IzinTakipApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class valuetype_changes : DbMigration
    {
        public override void Up()
        {
            DropIndex("dbo.IzinTalepleri", new[] { "KullaniciID" });
            AlterColumn("dbo.Users", "KalanYillikIzin", c => c.Double(nullable: false));
            AlterColumn("dbo.Users", "MazeretIzinKotasi", c => c.Double(nullable: false));
            AlterColumn("dbo.Users", "UcretsizIzinKotasi", c => c.Double(nullable: false));
            CreateIndex("dbo.IzinTalepleri", "KullaniciId");
        }
        
        public override void Down()
        {
            DropIndex("dbo.IzinTalepleri", new[] { "KullaniciId" });
            AlterColumn("dbo.Users", "UcretsizIzinKotasi", c => c.Int(nullable: false));
            AlterColumn("dbo.Users", "MazeretIzinKotasi", c => c.Int(nullable: false));
            AlterColumn("dbo.Users", "KalanYillikIzin", c => c.Int(nullable: false));
            CreateIndex("dbo.IzinTalepleri", "KullaniciID");
        }
    }
}
