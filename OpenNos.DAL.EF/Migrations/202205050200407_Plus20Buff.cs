namespace OpenNos.DAL.EF.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Plus20Buff : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ItemInstance", "Plus20Buff", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.ItemInstance", "Plus20Buff");
        }
    }
}
