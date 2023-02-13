// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace Duende.IdentityServer.Validation;

/// <summary>
/// Class describing the token endpoint request validation context
/// </summary>
public class TokenRequestValidationContext
{
    /// <summary>
    /// The request form parameters.
    /// </summary>
    public NameValueCollection RequestParameters { get; set; }

    /// <summary>
    /// The validaiton result of client authentication.
    /// </summary>
    public ClientSecretValidationResult ClientValidationResult { get; set; }

    /// <summary>
    /// The request headers.
    /// </summary>
    public IHeaderCollection RequestHeaders { get; set; } = new EmptyHeaderCollection();
}

/// <summary>
/// Abstracts accessing the HTTP request headers.
/// </summary>
public interface IHeaderCollection
{
    /// <summary>
    /// Gets the value associated with the specified key.
    ///     When this method returns, the value associated with the specified key, if the
    ///     key is found; otherwise, the default value for the type of the value parameter.
    ///     This parameter is passed uninitialized.
    /// Returns true if the collection contains an element with the specified key; otherwise, false.
    /// </summary>
    bool TryGetValue(string key, out string value);
}

internal class HeaderCollection : IHeaderCollection
{
    readonly IHeaderDictionary _headers;

    public HeaderCollection(IHeaderDictionary headers)
    {
        _headers = headers ?? throw new ArgumentNullException(nameof(headers));
    }

    public bool TryGetValue(string key, out string value)
    {
        var result = _headers.TryGetValue(key, out var values);
        value = values;
        return result;
    }
}

internal class EmptyHeaderCollection : IHeaderCollection
{
    public bool TryGetValue(string key, out string value)
    {
        value = null;
        return false;
    }
}