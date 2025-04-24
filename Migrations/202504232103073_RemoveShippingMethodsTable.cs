namespace doanwebnangcao.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class RemoveShippingMethodsTable : DbMigration
    {
        public override void Up()
        {
            // Chỉ giữ lại lệnh xóa bảng, vì cột ShippingMethod_Id đã được xóa trước đó
            DropTable("dbo.ShippingMethods");
        }

        public override void Down()
        {
            CreateTable(
                "dbo.ShippingMethods",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 100),
                    Cost = c.Decimal(nullable: false, precision: 18, scale: 2),
                    EstimatedDeliveryDays = c.Int(nullable: false),
                    IsActive = c.Boolean(nullable: false),
                })
                .PrimaryKey(t => t.Id);

            // Thêm lại cột ShippingMethod_Id với nullable: true (nếu trước đây nó là nullable)
            AddColumn("dbo.Orders", "ShippingMethod_Id", c => c.Int(nullable: true));
            CreateIndex("dbo.Orders", "ShippingMethod_Id");
            AddForeignKey("dbo.Orders", "ShippingMethod_Id", "dbo.ShippingMethods", "Id");
        }
    }
}