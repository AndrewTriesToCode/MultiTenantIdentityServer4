// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Linq;
using System.Security.Claims;
using Finbuckle.MultiTenant;
using IdentityModel;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.EntityFramework.Storage;
using IdentityServer4.Models;
using IdentityServerAspNetIdentity.Data;
using IdentityServerAspNetIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace IdentityServerAspNetIdentity
{
    public class SeedData
    {
        public static void EnsureSeedData(IConfiguration config)
        {
            var services = new ServiceCollection();

            services.AddSingleton(config);

            services.AddLogging();

            services.AddDbContext<ApplicationDbContext>(
#if !PER_TENANT_IDENTITY_STORES
                options => options.UseSqlite(config.GetConnectionString("AspNetIdentityConnection"))
#endif
                );

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

#if PER_TENANT_CONFIGURATION
            services.AddConfigurationDbContext<MultiTenantConfigurationDbContext>(options =>
                options.ConfigureDbContext = optionsBuilder =>
                    optionsBuilder.UseSqlite(config.GetConnectionString("ConfigurationStoreConnection")));
#else
            services.AddConfigurationDbContext(options =>
                options.ConfigureDbContext = optionsBuilder =>
                    optionsBuilder.UseSqlite(config.GetConnectionString("ConfigurationStoreConnection"), b => b.MigrationsAssembly("IdentityServerAspNetIdentity")));
#endif

            services.AddMultiTenant<TenantInfo>()
                .WithConfigurationStore();

            using (var serviceProvider = services.BuildServiceProvider())
            {
                foreach (var tenant in serviceProvider.GetRequiredService<IMultiTenantStore<TenantInfo>>()
                    .GetAllAsync().Result)
                {
                    using (var scope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
                    {
                        var multiTenantContextAccessor = scope.ServiceProvider
                            .GetRequiredService<IMultiTenantContextAccessor<TenantInfo>>();
                        multiTenantContextAccessor.MultiTenantContext = new MultiTenantContext<TenantInfo>
                        {
                            TenantInfo = tenant
                        };

                        Log.Debug($"Tenant: {tenant.Name}, {tenant.Identifier}, {tenant.Id}");

                        var applicationDbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

                        applicationDbContext.Database.Migrate();

                        var userMgr = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                        var alice = userMgr.FindByNameAsync("alice").Result;
                        if (alice == null)
                        {
                            alice = new ApplicationUser
                            {
                                UserName = "alice", Email = "AliceSmith@email.com", EmailConfirmed = true
                            };
                            var result = userMgr.CreateAsync(alice, "Pass123$").Result;
                            if (!result.Succeeded) throw new Exception(result.Errors.First().Description);

                            result = userMgr.AddClaimsAsync(alice,
                                new[]
                                {
                                    new Claim(JwtClaimTypes.Name, "Alice Smith"),
                                    new Claim(JwtClaimTypes.GivenName, "Alice"),
                                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                                    new Claim(JwtClaimTypes.WebSite, "http://alice.com")
                                }).Result;
                            if (!result.Succeeded) throw new Exception(result.Errors.First().Description);

                            Log.Debug("alice created");
                        }
                        else
                        {
                            Log.Debug("alice already exists");
                        }

                        var bob = userMgr.FindByNameAsync("bob").Result;
                        if (bob == null)
                        {
                            bob = new ApplicationUser
                            {
                                UserName = "bob", Email = "BobSmith@email.com", EmailConfirmed = true
                            };
                            var result = userMgr.CreateAsync(bob, "Pass123$").Result;
                            if (!result.Succeeded) throw new Exception(result.Errors.First().Description);

                            result = userMgr.AddClaimsAsync(bob,
                                new[]
                                {
                                    new Claim(JwtClaimTypes.Name, "Bob Smith"),
                                    new Claim(JwtClaimTypes.GivenName, "Bob"),
                                    new Claim(JwtClaimTypes.FamilyName, "Smith"),
                                    new Claim(JwtClaimTypes.WebSite, "http://bob.com"),
                                    new Claim("location", "somewhere")
                                }).Result;
                            if (!result.Succeeded) throw new Exception(result.Errors.First().Description);

                            Log.Debug("bob created");
                        }
                        else
                        {
                            Log.Debug("bob already exists");
                        }

#if PER_TENANT_CONFIGURATION
                        var configurationDbContext =
                            scope.ServiceProvider.GetRequiredService<MultiTenantConfigurationDbContext>();
#else
                        var configurationDbContext =
                            scope.ServiceProvider.GetRequiredService<IdentityServer4.EntityFramework.DbContexts.ConfigurationDbContext>();
#endif

                        configurationDbContext.Database.Migrate();

                        if (!configurationDbContext.IdentityResources.Any())
                        {
                            configurationDbContext.IdentityResources.AddRange(new IdentityResources.OpenId().ToEntity(),
                                new IdentityResources.Profile().ToEntity());
                            Log.Debug("ApiResources added.");
                        }
                        else
                        {
                            Log.Debug("ApiResources already exist.");
                        }

                        var clientId = $"mvc-{tenant.Identifier}";
                        if (!configurationDbContext.Clients.Where(c => c.ClientId == clientId).Any())
                        {
                            var client = new Client
                            {
                                ClientId = clientId,
                                ClientSecrets = { new Secret("secret".Sha256()) },
                                AllowedGrantTypes = GrantTypes.Code,
                                RedirectUris = { "https://localhost:44392/signin-oidc" },
                                //FrontChannelLogoutUri = "https://localhost:44300/signout-oidc",
                                //PostLogoutRedirectUris = { "https://localhost:44300/signout-callback-oidc" },

                                //AllowOfflineAccess = true,
                                AllowedScopes = { "openid", "profile" }
                            };
                            configurationDbContext.Clients.Add(client.ToEntity());
                            Log.Debug("Clients added.");
                        }
                        else
                        {
                            Log.Debug("Clients already exist.");
                        }

                        configurationDbContext.SaveChanges();
                    }
                }
            }
        }
    }
}