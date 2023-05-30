// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using IdentityModel;
using Microsoft.AspNetCore.Authentication;
using System.Text;
using System.Text.Json;

namespace IdentityServerHost.Pages.Diagnostics;

public class ViewModel
{
    public ViewModel(AuthenticateResult result)
    {
        AuthenticateResult = result;

        if (result?.Properties != null && result.Properties.Items.ContainsKey("client_list"))
        {
            var encoded = result.Properties.Items["client_list"];
            if (encoded != null)
            {
                var bytes = Base64Url.Decode(encoded);
                var value = Encoding.UTF8.GetString(bytes);
                Clients = JsonSerializer.Deserialize<string[]>(value) ?? Enumerable.Empty<string>();
                return;
            }
        }
        Clients = Enumerable.Empty<string>();
    }

    public AuthenticateResult AuthenticateResult { get; }
    public IEnumerable<string> Clients { get; }
}