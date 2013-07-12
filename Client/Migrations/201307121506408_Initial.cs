namespace Client.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.MasterRecords",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.DetailsRecords",
                c => new
                    {
                        MasterRecordId = c.Int(nullable: false),
                        Name = c.String(nullable: false, maxLength: 400),
                    })
                .PrimaryKey(t => new { t.MasterRecordId, t.Name })
                .ForeignKey("dbo.MasterRecords", t => t.MasterRecordId, cascadeDelete: true)
                .Index(t => t.MasterRecordId);
            
        }
        
        public override void Down()
        {
            DropIndex("dbo.DetailsRecords", new[] { "MasterRecordId" });
            DropForeignKey("dbo.DetailsRecords", "MasterRecordId", "dbo.MasterRecords");
            DropTable("dbo.DetailsRecords");
            DropTable("dbo.MasterRecords");
        }
    }
}
