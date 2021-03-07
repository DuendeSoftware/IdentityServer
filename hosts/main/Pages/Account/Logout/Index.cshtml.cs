using System.Threading.Tasks;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerHost.Pages.Account.Logout
{
    public class Index : PageModel
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;

        public ViewModel View { get; set; }

        [BindProperty] 
        public string LogoutId { get; set; }

        public Index(IIdentityServerInteractionService interaction, IEventService events)
        {
            _interaction = interaction;
            _events = events;
        }

        public async Task<IActionResult> OnGet(string logoutId)
        {
            // build a model so the logout page knows what to display
            await BuildModelAsync(logoutId);

            if (View.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await OnPost();
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            // build a model so the logout page knows what to display
            await BuildModelAsync(LogoutId);
            
            if (User?.Identity.IsAuthenticated == true)
            {
                // delete local authentication cookie
                await HttpContext.SignOutAsync();

                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            // check if we need to trigger sign-out at an upstream identity provider
            if (View.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Page("Logout", new { logoutId = LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, View.ExternalAuthenticationScheme);
            }

            return RedirectToPage("LoggedOut", new { logoutId = LogoutId });
        }

        private async Task BuildModelAsync(string logoutId)
        {
            View = new ViewModel { ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                View.ShowLogoutPrompt = false;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                View.ShowLogoutPrompt = false;
            }

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != Duende.IdentityServer.IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        View.ExternalAuthenticationScheme = idp;
                    }
                }
            }
        }
    }
}