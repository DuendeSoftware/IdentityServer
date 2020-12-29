// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using Microsoft.AspNetCore.DataProtection;
using System;
using System.Text.Json;

namespace Duende.IdentityServer.Stores.Serialization
{
    /// <summary>
    /// Options for how persisted grants are persisted.
    /// </summary>
    public class PersistentGrantOptions
    {
        /// <summary>
        /// Data protect the persisted grants "data" column.
        /// </summary>
        public bool DataProtectData { get; set; } = true;
    }

    /// <summary>
    /// JSON-based persisted grant serializer
    /// </summary>
    /// <seealso cref="IPersistentGrantSerializer" />
    public class PersistentGrantSerializer : IPersistentGrantSerializer
    {
        private static readonly JsonSerializerOptions _settings;

        private readonly PersistentGrantOptions _options;
        private readonly IDataProtector _provider;

        static PersistentGrantSerializer()
        {
            _settings = new JsonSerializerOptions
            {
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                IgnoreNullValues = true,
            };
            _settings.Converters.Add(new ClaimConverter());
            _settings.Converters.Add(new ClaimsPrincipalConverter());
        }

        /// <summary>
        /// Ctor.
        /// </summary>
        /// <param name="options"></param>
        /// <param name="dataProtectionProvider"></param>
        public PersistentGrantSerializer(PersistentGrantOptions options = null, IDataProtectionProvider dataProtectionProvider = null)
        {
            _options = options;
            _provider = dataProtectionProvider?.CreateProtector(nameof(PersistentGrantSerializer));
        }

        bool ShouldDataProtect => _options?.DataProtectData == true && _provider != null;

        /// <summary>
        /// Serializes the specified value.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public string Serialize<T>(T value)
        {
            var payload = JsonSerializer.Serialize(value, _settings);

            if (ShouldDataProtect)
            {
                payload = _provider.Protect(payload);
            }
            
            var data = new PersistentGrantDataContainer
            { 
                PersistentGrantDataContainerVersion = 1,
                DataProtected = ShouldDataProtect,
                Payload = payload,
            };

            return JsonSerializer.Serialize(data, _settings);
        }

        /// <summary>
        /// Deserializes the specified string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="json">The json.</param>
        /// <returns></returns>
        public T Deserialize<T>(string json)
        {
            var container = JsonSerializer.Deserialize<PersistentGrantDataContainer>(json, _settings);
            
            if (container.PersistentGrantDataContainerVersion == 0)
            {
                return JsonSerializer.Deserialize<T>(json, _settings);
            }

            if (container.PersistentGrantDataContainerVersion == 1)
            {
                var payload = container.Payload;
                
                if (container.DataProtected)
                {
                    if (_provider == null)
                    {
                        throw new Exception("No IDataProtectionProvider configured.");
                    }

                    payload = _provider.Unprotect(container.Payload);
                }

                return JsonSerializer.Deserialize<T>(payload, _settings);
            }

            throw new Exception($"Invalid version in persisted grant data: '{container.PersistentGrantDataContainerVersion}'.");
        }
    }

    class PersistentGrantDataContainer
    {
        public int PersistentGrantDataContainerVersion { get; set; }
        public bool DataProtected { get; set; }
        public string Payload { get; set; }
    }
}