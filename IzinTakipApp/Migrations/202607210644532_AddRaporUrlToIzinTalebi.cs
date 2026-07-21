namespace IzinTakipApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddRaporUrlToIzinTalebi : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.IzinTalepleri", "RaporUrl", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo.IzinTalepleri", "RaporUrl");
        }
    }
}
