namespace IzinTakipApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSirketPolitikasi : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "SirketPolitikasi", c => c.String());
            AlterColumn("dbo.Users", "PasswordHash", c => c.String(nullable: false, maxLength: 255));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Users", "PasswordHash", c => c.String(nullable: false));
            DropColumn("dbo.Users", "SirketPolitikasi");
        }
    }
}
