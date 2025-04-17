namespace doanwebnangcao.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddProductCodeToProducts : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Products", "ProductCode", c => c.String(nullable: false, maxLength: 50));
            CreateIndex("dbo.Products", "ProductCode", unique: true);
        }
        
        public override void Down()
        {
            DropIndex("dbo.Products", new[] { "ProductCode" });
            DropColumn("dbo.Products", "ProductCode");
        }
    }
}
