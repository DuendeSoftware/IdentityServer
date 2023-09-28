using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Configuration.DependencyInjection.Options;

public class PushedAuthorizationOptions
{
    public bool Required { get; set; }
 
    public int Lifetime { get; set; } = 60*15;
}
