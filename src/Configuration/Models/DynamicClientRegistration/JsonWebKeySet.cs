//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;

/// <summary>
/// Contains a collection of <see cref="DCR_JsonWebKey"/> that can be populated from a json string.
/// </summary>
public class DCR_JsonWebKeySet
{
    /// <summary>
    /// Initializes an new instance of <see cref="DCR_JsonWebKeySet"/>.
    /// </summary>
    public DCR_JsonWebKeySet()
    { }

    /// <summary>
    /// Initializes an new instance of <see cref="DCR_JsonWebKeySet"/> from a json string.
    /// </summary>
    /// <param name="json">a json string containing values.</param>
    /// <exception cref="InvalidOperationException">if web keys are malformed</exception>
    /// <exception cref="ArgumentNullException">if 'json' is null or whitespace.</exception>
    public DCR_JsonWebKeySet(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) throw new ArgumentNullException(nameof(json));

        var jwebKeys = JsonSerializer.Deserialize<DCR_JsonWebKeySet>(json);
        if (jwebKeys == null) throw new InvalidOperationException("invalid JSON web keys");
        
        Keys = jwebKeys.Keys;
        RawData = json;
    }

    /// <summary>
    /// A list of JSON web keys
    /// </summary>
    [JsonPropertyName("keys")]
    public List<DCR_JsonWebKey> Keys { get; set; } = new();
    
    /// <summary>
    /// The JSON string used to deserialize this object
    /// </summary>
    [JsonIgnore]
    public string? RawData { get; set; }
}
