namespace IzinTakipApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddMaxIzinliKotaToUser : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "MaxIzinliKota", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "MaxIzinliKota");
        }
    }
}
