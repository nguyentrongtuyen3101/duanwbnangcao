namespace doanwebnangcao.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class UpdateDatabaseWithNewModelsAndFields : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Addresses",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    UserId = c.Int(nullable: false),
                    FullName = c.String(nullable: false, maxLength: 100),
                    PhoneNumber = c.String(nullable: false, maxLength: 15),
                    AddressLine = c.String(nullable: false, maxLength: 200),
                    City = c.String(nullable: false, maxLength: 100),
                    Country = c.String(nullable: false, maxLength: 100),
                    IsDefault = c.Boolean(nullable: false, defaultValue: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);

            CreateTable(
                "dbo.Orders",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    UserId = c.Int(nullable: false),
                    OrderDate = c.DateTime(nullable: false, defaultValueSql: "GETDATE()"),
                    TotalAmount = c.Decimal(nullable: false, precision: 18, scale: 2),
                    Status = c.String(nullable: false, maxLength: 50),
                    ShippingAddressId = c.Int(nullable: false),
                    ShippingMethodId = c.Int(nullable: false),
                    ShippingCost = c.Decimal(nullable: false, precision: 18, scale: 2),
                    PaymentMethodId = c.Int(nullable: false),
                    CouponId = c.Int(),
                    DiscountApplied = c.Decimal(nullable: false, precision: 18, scale: 2, defaultValue: 0),
                    Notes = c.String(maxLength: 500),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Coupons", t => t.CouponId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .ForeignKey("dbo.PaymentMethods", t => t.PaymentMethodId, cascadeDelete: false) // Tắt cascadeDelete
                .ForeignKey("dbo.Addresses", t => t.ShippingAddressId, cascadeDelete: false)
                .ForeignKey("dbo.ShippingMethods", t => t.ShippingMethodId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.ShippingAddressId)
                .Index(t => t.ShippingMethodId)
                .Index(t => t.PaymentMethodId)
                .Index(t => t.CouponId);

            CreateTable(
                "dbo.Coupons",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Code = c.String(nullable: false, maxLength: 50),
                    DiscountAmount = c.Decimal(precision: 18, scale: 2),
                    DiscountPercentage = c.Decimal(precision: 18, scale: 2),
                    StartDate = c.DateTime(nullable: false),
                    EndDate = c.DateTime(nullable: false),
                    MaxUsage = c.Int(nullable: false),
                    UsedCount = c.Int(nullable: false, defaultValue: 0),
                    IsActive = c.Boolean(nullable: false, defaultValue: true),
                })
                .PrimaryKey(t => t.Id)
                .Index(t => t.Code, unique: true);

            CreateTable(
                "dbo.OrderDetails",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    OrderId = c.Int(nullable: false),
                    ProductId = c.Int(nullable: false),
                    Quantity = c.Int(nullable: false),
                    UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                    Subtotal = c.Decimal(nullable: false, precision: 18, scale: 2),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Orders", t => t.OrderId, cascadeDelete: true)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.OrderId)
                .Index(t => t.ProductId);

            CreateTable(
                "dbo.Products",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    SubcategoryId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 200),
                    Description = c.String(),
                    Price = c.Decimal(nullable: false, precision: 18, scale: 2),
                    DiscountedPrice = c.Decimal(precision: 18, scale: 2),
                    StockQuantity = c.Int(nullable: false),
                    ImageUrl = c.String(maxLength: 500),
                    CreatedAt = c.DateTime(nullable: false, defaultValueSql: "GETDATE()"),
                    IsActive = c.Boolean(nullable: false, defaultValue: true),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Subcategories", t => t.SubcategoryId, cascadeDelete: true)
                .Index(t => t.SubcategoryId);

            CreateTable(
                "dbo.CartDetails",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CartId = c.Int(nullable: false),
                    ProductId = c.Int(nullable: false),
                    Quantity = c.Int(nullable: false),
                    UnitPrice = c.Decimal(nullable: false, precision: 18, scale: 2),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Carts", t => t.CartId, cascadeDelete: true)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.CartId)
                .Index(t => t.ProductId);

            CreateTable(
                "dbo.Carts",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    UserId = c.Int(nullable: false),
                    CreatedAt = c.DateTime(nullable: false, defaultValueSql: "GETDATE()"),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);

            CreateTable(
                "dbo.Reviews",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ProductId = c.Int(nullable: false),
                    UserId = c.Int(nullable: false),
                    Rating = c.Int(nullable: false),
                    Comment = c.String(maxLength: 500),
                    CreatedAt = c.DateTime(nullable: false, defaultValueSql: "GETDATE()"),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.ProductId)
                .Index(t => t.UserId);

            CreateTable(
                "dbo.Wishlists",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    UserId = c.Int(nullable: false),
                    ProductId = c.Int(nullable: false),
                    AddedDate = c.DateTime(nullable: false, defaultValueSql: "GETDATE()"),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.ProductId);

            CreateTable(
                "dbo.ProductImages",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ProductId = c.Int(nullable: false),
                    ImageUrl = c.String(nullable: false, maxLength: 500),
                    IsMain = c.Boolean(nullable: false, defaultValue: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Products", t => t.ProductId, cascadeDelete: true)
                .Index(t => t.ProductId);

            CreateTable(
                "dbo.Subcategories",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    CategoryId = c.Int(nullable: false),
                    Name = c.String(nullable: false, maxLength: 100),
                    Description = c.String(maxLength: 500),
                    IsActive = c.Boolean(nullable: false, defaultValue: true),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Categories", t => t.CategoryId, cascadeDelete: true)
                .Index(t => t.CategoryId);

            CreateTable(
                "dbo.Categories",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 100),
                    Description = c.String(maxLength: 500),
                    IsActive = c.Boolean(nullable: false, defaultValue: true),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.PaymentMethods",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 100),
                    Description = c.String(maxLength: 500),
                    IsActive = c.Boolean(nullable: false, defaultValue: true),
                })
                .PrimaryKey(t => t.Id);

            CreateTable(
                "dbo.Payments",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    OrderId = c.Int(nullable: false),
                    PaymentMethodId = c.Int(nullable: false),
                    Amount = c.Decimal(nullable: false, precision: 18, scale: 2),
                    PaymentDate = c.DateTime(nullable: false, defaultValueSql: "GETDATE()"),
                    Status = c.String(nullable: false, maxLength: 50),
                    TransactionId = c.String(maxLength: 100),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Orders", t => t.OrderId, cascadeDelete: true)
                .ForeignKey("dbo.PaymentMethods", t => t.PaymentMethodId, cascadeDelete: false) // Tắt cascadeDelete
                .Index(t => t.OrderId)
                .Index(t => t.PaymentMethodId);

            CreateTable(
                "dbo.ShippingMethods",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Name = c.String(nullable: false, maxLength: 100),
                    Cost = c.Decimal(nullable: false, precision: 18, scale: 2),
                    EstimatedDeliveryDays = c.Int(nullable: false),
                    IsActive = c.Boolean(nullable: false, defaultValue: true),
                })
                .PrimaryKey(t => t.Id);

            AddColumn("dbo.Users", "PhoneNumber", c => c.String(maxLength: 15));
            AddColumn("dbo.Users", "IsActive", c => c.Boolean(nullable: false, defaultValue: true));
        }

        public override void Down()
        {
            DropForeignKey("dbo.Orders", "ShippingMethodId", "dbo.ShippingMethods");
            DropForeignKey("dbo.Orders", "ShippingAddressId", "dbo.Addresses");
            DropForeignKey("dbo.Payments", "PaymentMethodId", "dbo.PaymentMethods");
            DropForeignKey("dbo.Payments", "OrderId", "dbo.Orders");
            DropForeignKey("dbo.Orders", "PaymentMethodId", "dbo.PaymentMethods");
            DropForeignKey("dbo.Products", "SubcategoryId", "dbo.Subcategories");
            DropForeignKey("dbo.Subcategories", "CategoryId", "dbo.Categories");
            DropForeignKey("dbo.ProductImages", "ProductId", "dbo.Products");
            DropForeignKey("dbo.OrderDetails", "ProductId", "dbo.Products");
            DropForeignKey("dbo.CartDetails", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Wishlists", "UserId", "dbo.Users");
            DropForeignKey("dbo.Wishlists", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Reviews", "UserId", "dbo.Users");
            DropForeignKey("dbo.Reviews", "ProductId", "dbo.Products");
            DropForeignKey("dbo.Orders", "UserId", "dbo.Users");
            DropForeignKey("dbo.Carts", "UserId", "dbo.Users");
            DropForeignKey("dbo.Addresses", "UserId", "dbo.Users");
            DropForeignKey("dbo.CartDetails", "CartId", "dbo.Carts");
            DropForeignKey("dbo.OrderDetails", "OrderId", "dbo.Orders");
            DropForeignKey("dbo.Orders", "CouponId", "dbo.Coupons");
            DropIndex("dbo.Payments", new[] { "PaymentMethodId" });
            DropIndex("dbo.Payments", new[] { "OrderId" });
            DropIndex("dbo.Subcategories", new[] { "CategoryId" });
            DropIndex("dbo.ProductImages", new[] { "ProductId" });
            DropIndex("dbo.Wishlists", new[] { "ProductId" });
            DropIndex("dbo.Wishlists", new[] { "UserId" });
            DropIndex("dbo.Reviews", new[] { "UserId" });
            DropIndex("dbo.Reviews", new[] { "ProductId" });
            DropIndex("dbo.Carts", new[] { "UserId" });
            DropIndex("dbo.CartDetails", new[] { "ProductId" });
            DropIndex("dbo.CartDetails", new[] { "CartId" });
            DropIndex("dbo.Products", new[] { "SubcategoryId" });
            DropIndex("dbo.OrderDetails", new[] { "ProductId" });
            DropIndex("dbo.OrderDetails", new[] { "OrderId" });
            DropIndex("dbo.Coupons", new[] { "Code" });
            DropIndex("dbo.Orders", new[] { "CouponId" });
            DropIndex("dbo.Orders", new[] { "PaymentMethodId" });
            DropIndex("dbo.Orders", new[] { "ShippingMethodId" });
            DropIndex("dbo.Orders", new[] { "ShippingAddressId" });
            DropIndex("dbo.Orders", new[] { "UserId" });
            DropIndex("dbo.Addresses", new[] { "UserId" });
            DropColumn("dbo.Users", "IsActive");
            DropColumn("dbo.Users", "PhoneNumber");
            DropTable("dbo.ShippingMethods");
            DropTable("dbo.Payments");
            DropTable("dbo.PaymentMethods");
            DropTable("dbo.Categories");
            DropTable("dbo.Subcategories");
            DropTable("dbo.ProductImages");
            DropTable("dbo.Wishlists");
            DropTable("dbo.Reviews");
            DropTable("dbo.Carts");
            DropTable("dbo.CartDetails");
            DropTable("dbo.Products");
            DropTable("dbo.OrderDetails");
            DropTable("dbo.Coupons");
            DropTable("dbo.Orders");
            DropTable("dbo.Addresses");
        }
    }
}