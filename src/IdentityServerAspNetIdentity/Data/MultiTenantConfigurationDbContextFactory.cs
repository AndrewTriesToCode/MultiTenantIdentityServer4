using Finbuckle.MultiTenant;
using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdentityServerAspNetIdentity.Data
{
    public class MultiTenantConfigurationDbContextFactory : IDesignTimeDbContextFactory<MultiTenantConfigurationDbContext>
    {
        public MultiTenantConfigurationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MultiTenantConfigurationDbContext>();
            optionsBuilder.UseSqlite("Data Source=ConfigurationStore.db");

            var dummyTenant = new TenantInfo();
            var storeOptions = new ConfigurationStoreOptions();
            return new MultiTenantConfigurationDbContext(dummyTenant, optionsBuilder.Options, storeOptions);
        }
    }
}