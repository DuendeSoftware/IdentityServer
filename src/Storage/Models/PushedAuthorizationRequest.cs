using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Storage.Models;

public class PushedAuthorizationRequest
{
    public string RequestUri { get; set; }
    public DateTime Expiration { get; set; }

    public NameValueCollection Parameters { get; set; }
    
    // Can't do this, because we are in the storage layer, and the validation result isn't accessible here
    // public AuthorizeRequestValidationResult MyProperty { get; set; }
}
