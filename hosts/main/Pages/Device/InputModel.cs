using System.Collections.Generic;

namespace IdentityServerHost.Pages.Device
{
    public class InputModel
    {
        public string Button { get; set; }
        public IEnumerable<string> ScopesConsented { get; set; }
        public bool RememberConsent { get; set; }
        public string ReturnUrl { get; set; }
        public string Description { get; set; }
        public string UserCode { get; set; }
    }
}
