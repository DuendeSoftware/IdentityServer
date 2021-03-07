// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


namespace IdentityServerHost.Pages.Account.Logout
{
    public class ViewModel
    {
        public bool ShowLogoutPrompt { get; set; } = true;
        
        public string ExternalAuthenticationScheme { get; set; }
        public bool TriggerExternalSignout => ExternalAuthenticationScheme != null;
    }
}
