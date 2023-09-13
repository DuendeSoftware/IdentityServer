using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duende.IdentityServer.ResponseHandling;

public class PushedAuthorizationResponse
{
    public string RequestUri { get; set; }
    public DateTime Expiration { get; set; }
}
