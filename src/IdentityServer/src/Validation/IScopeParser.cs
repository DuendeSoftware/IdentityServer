// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.Collections.Generic;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Allows parsing raw scopes values into structured scope values.
    /// </summary>
    public interface IScopeParser
    {
        // todo: test return no error, and no parsed scopes. how do callers behave?
        /// <summary>
        /// Parses the requested scopes.
        /// </summary>
        ParsedScopesResult ParseScopeValues(IEnumerable<string> scopeValues);
    }
}
