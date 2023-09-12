using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Configuration.DependencyInjection.Options;

public class PushedAuthorizationOptions
{
    private bool _enabled;
    public bool Enabled 
    {
        get => _enabled;
        set
        {
            if(!value && Required) 
            {
                throw new ArgumentException("Cannot disable Pushed Authorization when it is required");
            }
            _enabled = value;
        }
    }

    private bool _required;
    public bool Required 
    { 
        get => _required;
        set
        {
            if(value && !Enabled)
            {
                throw new ArgumentException("Cannot require Pushed Authorization when it is disabled");
            }
            _required = value;
        }
    }
}
