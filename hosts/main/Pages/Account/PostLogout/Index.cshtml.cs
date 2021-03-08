using System.Threading.Tasks;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerHost.Pages.Account.PostLogout
{
    public class Index : PageModel
    {
        private readonly IIdentityServerInteractionService _interactionService;
        
        public ViewModel View { get; set; }

        public Index(IIdentityServerInteractionService interactionService)
        {
            _interactionService = interactionService;
        }

        public async Task OnGet(string logoutId)
        {
            await BuildModelAsync(logoutId);
        }

        private async Task BuildModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interactionService.GetLogoutContextAsync(logoutId);

            View = new ViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl
            };
        }
    }
}