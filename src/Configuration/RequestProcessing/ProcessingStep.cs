// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

namespace Duende.IdentityServer.Configuration.RequestProcessing;

/// <summary>
/// Represents the result of a step in the dynamic client registration validator.
/// </summary>
public abstract class RequestProcessingStep<TResult>
{
}

/// <summary>
/// Represents a successful RequestProcessing step that returns some data.
/// </summary>
public class RequestProcessingStepSuccess<TResult> : RequestProcessingStep<TResult>
{
    /// <summary>
    /// The results of this step of processing.
    /// </summary>
    public TResult? StepResult { get; set; }
}

/// <summary>
/// Represents a failed RequestProcessing step.
/// </summary>
public class RequestProcessingStepFailure<TResult> : RequestProcessingStep<TResult>
{
    /// <summary>
    /// A short, human-readable message briefly describing the failure that occurred. 
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// A longer, human-readable message describing the failure that occurred with more detail.
    /// </summary>
    public string ErrorDescription { get; set; } = string.Empty;
}

/// <summary>
/// TODO
/// </summary>
public class RequestProcessingStep
{
}

/// <summary>
/// TODO
/// </summary>
public class RequestProcessingStepSuccess : RequestProcessingStep { };

/// <summary>
/// TODO
/// </summary>
public class RequestProcessingStepFailure : RequestProcessingStep // TODO - inheriet the other from this
{
 /// <summary>
    /// A short, human-readable message briefly describing the failure that occurred. 
    /// </summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>
    /// A longer, human-readable message describing the failure that occurred with more detail.
    /// </summary>
    public string ErrorDescription { get; set; } = string.Empty;
}
