namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class EmptyMigration : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.NpcMonster", "Stars", c => c.Byte(nullable: false));
            AddColumn("dbo.Mate", "TrainingLevel", c => c.Byte(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Mate", "TrainingLevel");
            DropColumn("dbo.NpcMonster", "Stars");
        }
    }
}
