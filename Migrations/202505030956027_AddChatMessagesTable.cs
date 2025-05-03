namespace doanwebnangcao.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AddChatMessagesTable : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ChatMessages",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    SenderId = c.Int(nullable: false),
                    ReceiverId = c.Int(nullable: false),
                    Content = c.String(nullable: false),
                    FilePath = c.String(),
                    SentAt = c.DateTime(nullable: false),
                    IsRead = c.Boolean(nullable: false),
                })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.SenderId, cascadeDelete: false)
                .ForeignKey("dbo.Users", t => t.ReceiverId, cascadeDelete: false)
                .Index(t => t.SenderId)
                .Index(t => t.ReceiverId);
        }

        public override void Down()
        {
            DropForeignKey("dbo.ChatMessages", "ReceiverId", "dbo.Users");
            DropForeignKey("dbo.ChatMessages", "SenderId", "dbo.Users");
            DropIndex("dbo.ChatMessages", new[] { "ReceiverId" });
            DropIndex("dbo.ChatMessages", new[] { "SenderId" });
            DropTable("dbo.ChatMessages");
        }
    }
}