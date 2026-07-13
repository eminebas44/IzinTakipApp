namespace IzinTakipApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateIzinDurumuToEnum : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.IzinTalepleri", "Durum", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.IzinTalepleri", "Durum", c => c.String(maxLength: 50));
        }
    }
}
