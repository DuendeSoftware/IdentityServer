// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerHost.Pages.Ciba
{
    [SecurityHeaders]
    [Authorize]
    public class IndexModel : PageModel
    {
        public IEnumerable<BackChannelAuthenticationRequest> Logins { get; set; }

        [BindProperty, Required]
        public string Id { get; set; }
        [BindProperty, Required]
        public string Button { get; set; }

        private readonly IBackchannelAuthenticationInteractionService _backchannelAuthenticationInteraction;

        public IndexModel(IBackchannelAuthenticationInteractionService backchannelAuthenticationInteractionService)
        {
            _backchannelAuthenticationInteraction = backchannelAuthenticationInteractionService;
        }

        public async Task OnGet()
        {
            Logins = await _backchannelAuthenticationInteraction.GetLoginsForSubjectAsync(User.GetSubjectId());
        }

        public async Task<IActionResult> OnPost()
        {
            if (Id != null && Button != null)
            {
                if (Button == "allow")
                {
                }
                if (Button == "deny")
                {
                }
                if (Button == "delete")
                {
                    await _backchannelAuthenticationInteraction.RemoveLoginAsync(Id);
                }
            }

            return RedirectToPage();
        }
    }
}
