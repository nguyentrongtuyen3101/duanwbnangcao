namespace doanwebnangcao.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class UpdateOrderStatusToEnum : DbMigration
    {
        public override void Up()
        {
            AlterColumn("dbo.Orders", "Status", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            AlterColumn("dbo.Orders", "Status", c => c.String(nullable: false, maxLength: 50));
        }
    }
}
