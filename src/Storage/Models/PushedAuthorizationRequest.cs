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
    public int Expiration { get; set; }

    public string Parameters { get; set; }

    // Maybe don't want to do this, because we should data protect the payload?
    //public NameValueCollection Parameters { get; set; }
    
    // Can't do this, because we are in the storage layer, and the validation result isn't accessible here
    // public AuthorizeRequestValidationResult MyProperty { get; set; }
}
