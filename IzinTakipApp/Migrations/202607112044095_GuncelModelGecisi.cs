namespace IzinTakipApp.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class GuncelModelGecisi : DbMigration
    {
        public override void Up()
        {
            //AddColumn("dbo.Users", "MazeretIzinKotasi", c => c.Int(nullable: false));
            //AddColumn("dbo.Users", "UcretsizIzinKotasi", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            //DropColumn("dbo.Users", "UcretsizIzinKotasi");
            //DropColumn("dbo.Users", "MazeretIzinKotasi");
        }
    }
}
