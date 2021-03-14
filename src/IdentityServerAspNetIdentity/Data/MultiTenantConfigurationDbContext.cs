using System.Threading;
using System.Threading.Tasks;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Options;
using Microsoft.EntityFrameworkCore;

namespace IdentityServerAspNetIdentity.Data
{
    public class MultiTenantConfigurationDbContext : ConfigurationDbContext<MultiTenantConfigurationDbContext>, IMultiTenantDbContext
    {
        public MultiTenantConfigurationDbContext(ITenantInfo tenantInfo, DbContextOptions<MultiTenantConfigurationDbContext> options, ConfigurationStoreOptions storeOptions) :
            base(options, storeOptions)
        {
            TenantInfo = tenantInfo;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Client>().IsMultiTenant();
        }
        
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.EnforceMultiTenant();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            this.EnforceMultiTenant();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public ITenantInfo TenantInfo { get; }
        public TenantMismatchMode TenantMismatchMode { get; } = TenantMismatchMode.Throw;
        public TenantNotSetMode TenantNotSetMode { get; } = TenantNotSetMode.Throw;
    }
}