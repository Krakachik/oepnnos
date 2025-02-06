namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class sp10kacke : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Character", "SP10progress1", c => c.Int(nullable: false));
            AddColumn("dbo.Character", "SP10progress2", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Character", "SP10progress2");
            DropColumn("dbo.Character", "SP10progress1");
        }
    }
}
