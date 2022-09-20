﻿using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using IdentityServerAspNetIdentity.Models;
using System;

namespace IdentityServerAspNetIdentity.Data
{
    public partial class ApplicationDbContext : MultiTenantIdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(ITenantInfo tenantInfo) : base(tenantInfo)
        {
        }

        public ApplicationDbContext(ITenantInfo tenantInfo, DbContextOptions<ApplicationDbContext> options) :
            base(tenantInfo, options)
        {
        }

#if PER_TENANT_IDENTITY_STORES
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(TenantInfo.ConnectionString ?? throw new InvalidOperationException());
            base.OnConfiguring(optionsBuilder);
        }
#endif
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<ApplicationUser>().IsMultiTenant();
        }
    }
}