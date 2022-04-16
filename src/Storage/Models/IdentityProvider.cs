// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
namespace Duende.IdentityServer.Models;

/// <summary>
/// Models name for a scheme
/// </summary>
public class IdentityProviderName
{
    /// <summary>
    /// Scheme name for the provider.
    /// </summary>
    public string Scheme { get; set; }

    /// <summary>
    /// Display name for the provider.
    /// </summary>
    public string DisplayName { get; set; }
        
    /// <summary>
    /// Flag that indicates if the provider should be used.
    /// </summary>
    public bool Enabled { get; set; }
}

/// <summary>
/// Additional options that are used to setup a new <see cref="IdentityProvider"/>
/// </summary>
public class IdentityProviderOptions
{
    /// <summary>
    /// additional properties to add to the provider
    /// make sure the keys added do not collide with the property names that exist within the class
    /// </summary>
    public Dictionary<string, string> AdditionalProperties { get; set; }
}

/// <summary>
/// Models general storage for an external authentication provider/handler scheme
/// </summary>
public class IdentityProvider
{
    /// <summary>
    /// Ctor
    /// </summary>
    public IdentityProvider(string type)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    /// <summary>
    /// Ctor
    /// </summary>
    public IdentityProvider(string type, Action<IdentityProviderOptions> setupAction) : this(type)
    {
        ProcessOptions(setupAction);
    }

    /// <summary>
    /// Ctor
    /// </summary>
    public IdentityProvider(string type, IdentityProvider other) : this(type)
    {
        if (other == null) throw new ArgumentNullException(nameof(other));
        if (other.Type != type) throw new ArgumentException($"Type '{type}' does not match type of other '{other.Type}'");

        Scheme = other.Scheme;
        DisplayName = other.DisplayName;
        Enabled = other.Enabled;
        Type = other.Type;
        // add the other's properties this way, so it never collides with already set properties 
        //  via other property setters in this class or subclasses
        ProcessOptions(options =>
            options.AdditionalProperties = new Dictionary<string, string>(other.Properties));
    }

    /// <summary>
    /// Ctor
    /// </summary>
    public IdentityProvider(string type, IdentityProvider other, Action<IdentityProviderOptions> setupAction) : this(type, other)
    {
        ProcessOptions(setupAction);
    }

    /// <summary>
    /// Processes additional setup options passed in from the constructor
    /// </summary>
    private void ProcessOptions(Action<IdentityProviderOptions> setupAction)
    {
        if (setupAction == null)
            throw new ArgumentNullException(nameof(setupAction));

        var idpConfiguration = new IdentityProviderOptions();
        setupAction(idpConfiguration);
        foreach (var property in idpConfiguration.AdditionalProperties)
            _properties.Add(property.Key, property.Value);
    }

    /// <summary>
    /// Scheme name for the provider.
    /// </summary>
    public string Scheme { get; set; }

    /// <summary>
    /// Display name for the provider.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Flag that indicates if the provider should be used.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Protocol type of the provider.
    /// </summary>
    public string Type { get; set; }

    /// <summary>
    /// Protocol specific properties for the provider.
    /// </summary>
    private Dictionary<string, string> _properties = new Dictionary<string, string>();

    /// <summary>
    /// Protocol specific properties for the provider.
    /// </summary>
    public IReadOnlyDictionary<string, string> Properties 
    {
        // ensure that the properties collection can never be modified
        // directly by the outside code after the object is constructed.  
        // since this _properties collection is populated via standard property setters
        //  via subclasses which implement specific providers (e.g. OidcProvider)
        // which is not intuitive to the consumer of this class.  
        get => _properties;
        // private setter for AutoMapper (which can access properties with private setters)
        private set
        {
            foreach (var kvp in value)
                this[kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Properties indexer
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    protected string this[string name]
    {
        get
        {
            _properties.TryGetValue(name, out var result);
            return result;
        }
        set
        {
            _properties[name] = value;
        }
    }
}