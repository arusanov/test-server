namespace Client.Migrations
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Client.DAO.DataSets>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
        }
    }
}
