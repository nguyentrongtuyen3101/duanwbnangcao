namespace doanwebnangcao.Migrations
{
    using System.Data.Entity.Migrations;

    public partial class RemoveShippingMethodIdAfterRollback : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.Orders", "ShippingMethodId", "dbo.ShippingMethods");
            DropIndex("dbo.Orders", new[] { "ShippingMethodId" });
            DropColumn("dbo.Orders", "ShippingMethodId");
        }

        public override void Down()
        {
            AddColumn("dbo.Orders", "ShippingMethodId", c => c.Int());
            CreateIndex("dbo.Orders", "ShippingMethodId");
            AddForeignKey("dbo.Orders", "ShippingMethodId", "dbo.ShippingMethods", "Id");
        }
    }
}