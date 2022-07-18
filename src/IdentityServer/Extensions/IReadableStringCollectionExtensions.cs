// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

#pragma warning disable 1591

namespace Duende.IdentityServer.Extensions;

public static class IReadableStringCollectionExtensions
{
    [DebuggerStepThrough]
    public static NameValueCollection AsNameValueCollection(this IEnumerable<KeyValuePair<string, StringValues>> collection)
    {
        var nv = new NameValueCollection();

        foreach (var field in collection)
        {
            foreach (var val in field.Value)
            {
                // special check for some Azure product: https://github.com/DuendeSoftware/Support/issues/48
                if (!String.IsNullOrWhiteSpace(val))
                {
                    // ignore duplicate values, as this is used internally to validate requests
                    // so for simplicy we filter dup values
                    var vals = nv.GetValues(field.Key);
                    if (vals == null || !vals.Contains(val))
                    {
                        nv.Add(field.Key, val);
                    }
                }
            }
        }

        return nv;
    }

    [DebuggerStepThrough]
    public static NameValueCollection AsNameValueCollection(this IDictionary<string, StringValues> collection)
    {
        var nv = new NameValueCollection();

        foreach (var field in collection)
        {
            foreach (var item in field.Value)
            {
                // special check for some Azure product: https://github.com/DuendeSoftware/Support/issues/48
                if (!String.IsNullOrWhiteSpace(item))
                {
                    // this version of the method keeps dups, as it's used to reconstruct the exact same URL elsewhere (e.g. redirect uri)
                    nv.Add(field.Key, item);
                }
            }
        }

        return nv;
    }
}