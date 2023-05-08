// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.IdentityServer.Configuration.Models.DynamicClientRegistration;
using Duende.IdentityServer.Configuration.Validation.DynamicClientRegistration;

namespace Duende.IdentityServer.Configuration.Validation;

/// <summary>
/// Represents the result of a step in dynamic client registration validation or processing.
/// </summary>
public abstract class StepResult 
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="errorDescription"></param>
    /// <param name="error"></param>
    /// <returns></returns>
    public static Task<StepResult> Failure(string errorDescription,
        string error = DynamicClientRegistrationErrors.InvalidClientMetadata) =>
            Task.FromResult<StepResult>(new FailedStep(error, errorDescription));

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static Task<StepResult> Success() =>
        Task.FromResult<StepResult>(new SuccessfulStep());
}

/// <summary>
/// Represents a successful validation step.
/// </summary>
public class SuccessfulStep : StepResult
{
}

/// <summary>
/// Represents a failed validation step.
/// </summary>
public class FailedStep : StepResult, IDynamicClientRegistrationResponse, IDynamicClientRegistrationValidationResult
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="error"></param>
    /// <param name="errorDescription"></param>
    public FailedStep(string error, String errorDescription)
    {
        Error = error;
        ErrorDescription = errorDescription;
    }

    /// <summary>
    /// 
    /// </summary>
    public string Error { get; set; }


    /// <summary>
    /// 
    /// </summary>
    public string ErrorDescription { get; set; }
}
