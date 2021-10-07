// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Duende.IdentityServer.Validation
{
    /// <summary>
    /// Interface for the backchannel authentication user validation
    /// </summary>
    public interface IBackchannelAuthenticationUserValidator
    {
        /// <summary>
        /// Validates the user.
        /// </summary>
        /// <param name="userValidatorContext"></param>
        /// <returns></returns>
        Task<BackchannelAuthenticationUserValidatonResult> ValidateRequestAsync(BackchannelAuthenticationUserValidatorContext userValidatorContext);
    }

    /// <summary>
    /// 
    /// </summary>
    public class BackchannelAuthenticationUserValidatonResult
    {
        /// <summary>
        /// Indicates if this represents an error.
        /// </summary>
        public bool IsError => !String.IsNullOrWhiteSpace(Error);

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public string Error { get; set; }
        
        /// <summary>
        /// Gets or sets the error description.
        /// </summary>
        public string ErrorDescription { get; set; }

        /// <summary>
        /// Gets or sets the subject based upon the provided hint.
        /// </summary>
        public ClaimsPrincipal Subject { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class BackchannelAuthenticationUserValidatorContext
    {
        /// <summary>
        /// Gets or sets the login hint token.
        /// </summary>
        public string LoginHintToken { get; set; }

        /// <summary>
        /// Gets or sets the id token hint.
        /// </summary>
        public string IdTokenHint { get; set; }

        /// <summary>
        /// Gets or sets the login hint.
        /// </summary>
        public string LoginHint { get; set; }

        /// <summary>
        /// Gets or sets the user code.
        /// </summary>
        public string UserCode { get; set; }

        /// <summary>
        /// Gets or sets the binding message.
        /// </summary>
        public string BindingMessage { get; set; }
    }
}