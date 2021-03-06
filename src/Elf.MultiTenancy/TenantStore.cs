﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elf.MultiTenancy
{
    public interface ITenantStore<T> where T : Tenant
    {
        Task<T> GetTenantAsync(string identifier);

        Task<List<T>> GetAllTenantsAsync();
    }

    /// <summary>
    /// In memory store for testing
    /// </summary>
    public class InMemoryTenantStore : ITenantStore<Tenant>
    {
        /// <summary>
        /// Get a tenant for a given identifier
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public Task<Tenant> GetTenantAsync(string identifier)
        {
            var tenant = new[]
            {
                new Tenant
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000000"),
                    Identifier = "localhost",
                    Items = new()
                    {
                        { "DefaultRedirectionUrl","https://localhost" }
                    }
                }
            }.SingleOrDefault(t => t.Identifier == identifier);

            return Task.FromResult(tenant);
        }

        public Task<List<Tenant>> GetAllTenantsAsync()
        {
            throw new NotImplementedException();
        }
    }

    public class AppSettingsTenantStore : ITenantStore<Tenant>
    {
        private readonly IOptionsSnapshot<List<Tenant>> _tenantOptionsSnapshot;

        private readonly ILogger<AppSettingsTenantStore> _logger;

        public AppSettingsTenantStore(
            IOptionsSnapshot<List<Tenant>> tenantOptionsSnapshot,
            ILogger<AppSettingsTenantStore> logger)
        {
            _tenantOptionsSnapshot = tenantOptionsSnapshot;
            _logger = logger;
        }

        public Task<Tenant> GetTenantAsync(string identifier)
        {
            _logger.LogDebug($"Finding Tenant with identifier '{identifier}'");

            var tenants = _tenantOptionsSnapshot.Value;
            if (tenants.Count > 0)
            {
                var tenant = tenants.SingleOrDefault(t => t.Identifier == identifier);
                if (null == tenant)
                {
                    _logger.LogWarning($"Tenant '{identifier}' not found, finding default Tenant.");

                    var defaultTenant = tenants.SingleOrDefault(t => t.IsDefault);
                    if (null == defaultTenant)
                    {
                        _logger.LogCritical($"No Default Tenant is found.");
                        throw new InvalidOperationException("No Default Tenant is found.");
                    }

                    _logger.LogInformation($"Default Tenant '{defaultTenant.Id}: {defaultTenant.Identifier}' is now being used for the current request.");
                    return Task.FromResult(defaultTenant);
                }
                return Task.FromResult(tenant);
            }

            _logger.LogCritical("No Tenants found in configuration.");
            throw new InvalidOperationException("No Tenants found in configuration.");
        }

        public Task<List<Tenant>> GetAllTenantsAsync()
        {
            var tenants = _tenantOptionsSnapshot.Value;
            return Task.FromResult(tenants);
        }
    }
}
