namespace doanwebnangcao.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class MigrationName : DbMigration
    {
        public override void Up()
        {
            DropForeignKey("dbo.CartDetails", "ProductId", "dbo.Products");
            DropForeignKey("dbo.OrderDetails", "ProductId", "dbo.Products");
            DropIndex("dbo.OrderDetails", new[] { "ProductId" });
            DropIndex("dbo.CartDetails", new[] { "ProductId" });
            RenameColumn(table: "dbo.CartDetails", name: "ProductId", newName: "Product_Id");
            RenameColumn(table: "dbo.OrderDetails", name: "ProductId", newName: "Product_Id");
            CreateTable(
                "dbo.ProductVariants",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ProductId = c.Int(nullable: false),
                        SizeId = c.Int(nullable: false),
                        ColorId = c.Int(nullable: false),
                        StockQuantity = c.Int(nullable: false),
                        VariantPrice = c.Decimal(precision: 18, scale: 2),
                        VariantDiscountedPrice = c.Decimal(precision: 18, scale: 2),
                        VariantImageUrl = c.String(maxLength: 500),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .ForeignKey("dbo.Colors", t => t.ColorId, cascadeDelete: true)
                .ForeignKey("dbo.Sizes", t => t.SizeId, cascadeDelete: true)
                .Index(t => t.ProductId)
                .Index(t => t.SizeId)
                .Index(t => t.ColorId);
            
            CreateTable(
                "dbo.Colors",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        HexCode = c.String(maxLength: 7),
                        Description = c.String(maxLength: 200),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Sizes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 50),
                        Description = c.String(maxLength: 200),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            AddColumn("dbo.OrderDetails", "ProductVariantId", c => c.Int(nullable: false));
            AddColumn("dbo.CartDetails", "ProductVariantId", c => c.Int(nullable: false));
            AlterColumn("dbo.OrderDetails", "Product_Id", c => c.Int());
            AlterColumn("dbo.CartDetails", "Product_Id", c => c.Int());
            CreateIndex("dbo.OrderDetails", "ProductVariantId");
            CreateIndex("dbo.OrderDetails", "Product_Id");
            CreateIndex("dbo.CartDetails", "ProductVariantId");
            CreateIndex("dbo.CartDetails", "Product_Id");
            AddForeignKey("dbo.CartDetails", "ProductVariantId", "dbo.ProductVariants", "Id", cascadeDelete: true);
            AddForeignKey("dbo.OrderDetails", "ProductVariantId", "dbo.ProductVariants", "Id", cascadeDelete: true);
            AddForeignKey("dbo.CartDetails", "Product_Id", "dbo.Products", "Id");
            AddForeignKey("dbo.OrderDetails", "Product_Id", "dbo.Products", "Id");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.OrderDetails", "Product_Id", "dbo.Products");
            DropForeignKey("dbo.CartDetails", "Product_Id", "dbo.Products");
            DropForeignKey("dbo.ProductVariants", "SizeId", "dbo.Sizes");
            DropForeignKey("dbo.OrderDetails", "ProductVariantId", "dbo.ProductVariants");
            DropForeignKey("dbo.ProductVariants", "ColorId", "dbo.Colors");
            DropForeignKey("dbo.CartDetails", "ProductVariantId", "dbo.ProductVariants");
            DropForeignKey("dbo.ProductVariants", "ProductId", "dbo.Products");
            DropIndex("dbo.CartDetails", new[] { "Product_Id" });
            DropIndex("dbo.CartDetails", new[] { "ProductVariantId" });
            DropIndex("dbo.ProductVariants", new[] { "ColorId" });
            DropIndex("dbo.ProductVariants", new[] { "SizeId" });
            DropIndex("dbo.ProductVariants", new[] { "ProductId" });
            DropIndex("dbo.OrderDetails", new[] { "Product_Id" });
            DropIndex("dbo.OrderDetails", new[] { "ProductVariantId" });
            AlterColumn("dbo.CartDetails", "Product_Id", c => c.Int(nullable: false));
            AlterColumn("dbo.OrderDetails", "Product_Id", c => c.Int(nullable: false));
            DropColumn("dbo.CartDetails", "ProductVariantId");
            DropColumn("dbo.OrderDetails", "ProductVariantId");
            DropTable("dbo.Sizes");
            DropTable("dbo.Colors");
            DropTable("dbo.ProductVariants");
            RenameColumn(table: "dbo.OrderDetails", name: "Product_Id", newName: "ProductId");
            RenameColumn(table: "dbo.CartDetails", name: "Product_Id", newName: "ProductId");
            CreateIndex("dbo.CartDetails", "ProductId");
            CreateIndex("dbo.OrderDetails", "ProductId");
            AddForeignKey("dbo.OrderDetails", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
            AddForeignKey("dbo.CartDetails", "ProductId", "dbo.Products", "Id", cascadeDelete: true);
        }
    }
}
