using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServerHost.Pages.Admin.Clients
{
    public class ClientModel
    {
        public string ClientId { get; set; }
        public string ClientName { get; set; }
        public string Secret { get; set; }
        
        public Flow Flow { get; set; }

        public string RedirectUri { get; set; }
        public string PostLogoutRedirectUri { get; set; }
        public string FrontChannelLogoutUri { get; set; }
        public string BackChannelLogoutUri { get; set; }

        public string AllowedScopes { get; set; }
    }

    public enum Flow
    {
        ClientCredentials,
        CodeFlowWithPkce
    }

    public class ClientRepository
    {
        private readonly ConfigurationDbContext _context;

        public ClientRepository(ConfigurationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ClientModel>> GetAllAsync()
        {
            var grants = new[] { GrantType.AuthorizationCode, GrantType.ClientCredentials };
            
            var query = _context.Clients
                .Where(x => x.AllowedGrantTypes.Count == 1 && x.AllowedGrantTypes.Any(grant => grants.Contains(grant.GrantType)))
                .Select(x=>new ClientModel 
                { 
                    ClientId = x.ClientId,
                    ClientName = x.ClientName,
                    Flow = x.AllowedGrantTypes.Select(x=>x.GrantType).Single() == GrantType.ClientCredentials ? Flow.ClientCredentials : Flow.CodeFlowWithPkce
                });

            return await query.ToArrayAsync();
        }

        public async Task<ClientModel> GetByIdAsync(string id)
        {
            var query = _context.Clients
               .Where(x => x.ClientId == id)
               .Select(x => new ClientModel
               {
                   ClientId = x.ClientId,
                   ClientName = x.ClientName,
                   Flow = x.AllowedGrantTypes.Select(x => x.GrantType).Single() == GrantType.ClientCredentials ? Flow.ClientCredentials : Flow.CodeFlowWithPkce
               });
            return await query.SingleOrDefaultAsync();
        }
    }
}
