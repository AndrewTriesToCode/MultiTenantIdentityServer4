using Finbuckle.MultiTenant;
using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace IdentityServerAspNetIdentity.Data
{
    public class MultiTenantPersistedGrantDbContextFactory : IDesignTimeDbContextFactory<MultiTenantPersistedGrantDbContext>
    {
        public MultiTenantPersistedGrantDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<MultiTenantPersistedGrantDbContext>();
            optionsBuilder.UseSqlite("Data SourceOperationalStore.db");

            var dummyTenant = new TenantInfo();
            var storeOptions = new OperationalStoreOptions();
            return new MultiTenantPersistedGrantDbContext(dummyTenant, optionsBuilder.Options, storeOptions);
        }
    }
}