namespace doanwebnangcao.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddSizeColorProductVariantAndUpdateModels : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.ProductImages", "ProductId", "dbo.Products");
            DropIndex("dbo.ProductImages", new[] { "ProductId" });
            RenameColumn(table: "dbo.ProductImages", name: "ProductId", newName: "Product_Id");
            AddColumn("dbo.ProductImages", "ProductVariantId", c => c.Int(nullable: false));
            AlterColumn("dbo.ProductImages", "Product_Id", c => c.Int());
            CreateIndex("dbo.ProductImages", "ProductVariantId");
            CreateIndex("dbo.ProductImages", "Product_Id");
            AddForeignKey("dbo.ProductImages", "ProductVariantId", "dbo.ProductVariants", "Id", cascadeDelete: true);
            AddForeignKey("dbo.ProductImages", "Product_Id", "dbo.Products", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ProductImages", "Product_Id", "dbo.Products");
            DropForeignKey("dbo.ProductImages", "ProductVariantId", "dbo.ProductVariants");
            DropIndex("dbo.ProductImages", new[] { "Product_Id" });
            DropIndex("dbo.ProductImages", new[] { "ProductVariantId" });
            AlterColumn("dbo.ProductImages", "Product_Id", c => c.Int(nullable: false));
            DropColumn("dbo.ProductImages", "ProductVariantId");
            RenameColumn(table: "dbo.ProductImages", name: "Product_Id", newName: "ProductId");
            CreateIndex("dbo.ProductImages", "ProductId");
            AddForeignKey("dbo.ProductImages", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
        }
    }
}
