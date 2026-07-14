namespace IzinTakipApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddIsFirstLoginToUsers : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "IsFirstLogin", c => c.Boolean(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Users", "IsFirstLogin");
        }
    }
}
