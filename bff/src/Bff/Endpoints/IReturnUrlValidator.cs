// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


namespace Duende.Bff.Endpoints;

/// <summary>
/// Allows validating if the return URL for login and logout is valid.
/// </summary>
public interface IReturnUrlValidator
{
    /// <summary>
    /// Returns true if the return URL is valid and safe to redirect to.
    /// </summary>
    /// <param name="returnUrl">The return URL to validate.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task containing the validation result.</returns>
    public Task<bool> IsValidAsync(Uri returnUrl, Ct ct = default);
}
