// // Copyright (c) Duende Software. All rights reserved.
// // See LICENSE in the project root for license information.

// namespace Duende.IdentityServer.Configuration.RequestProcessing;

// /// <summary>
// /// Represents a step in the request processor that will return a TResult if it
// /// succeeds.
// /// </summary>
// public abstract class RequestProcessingStep<TResult>
// {
// }

// /// <summary>
// /// Represents a step in the request processor that succeeded, returning a
// /// TResult.
// /// </summary>
// public class RequestProcessingStepSuccess<TResult> : RequestProcessingStep<TResult>
// {
//     /// <summary>
//     /// The results of this step of processing.
//     /// </summary>
//     public TResult? StepResult { get; set; }
// }

// /// <summary>
// /// Represents a failed step in the request processor that would have returned a
// /// TResult if it had succeeded. If a step would return a TResult on success,
// /// use this type so that the success and failure cases have the same base type.
// /// </summary>
// public class RequestProcessingStepFailure<TResult> : RequestProcessingStep<TResult>
// {
//     /// <summary>
//     /// A short, human-readable message briefly describing the failure that occurred. 
//     /// </summary>
//     public string Error { get; set; } = string.Empty;

//     /// <summary>
//     /// A longer, human-readable message describing the failure that occurred with more detail.
//     /// </summary>
//     public string ErrorDescription { get; set; } = string.Empty;
// }

// /// <summary>
// /// Represents a step in the request processor that doesn't return any data.
// /// </summary>
// public class RequestProcessingStep
// {
// }

// /// <summary>
// /// Represents a successful step in the request processor that didn't return any
// /// data.
// /// </summary>
// public class RequestProcessingStepSuccess : RequestProcessingStep { };

// /// <summary>
// /// Represents a failed step in the request processor. Steps that would not
// /// return any data on success should use this type.
// /// </summary>
// public class RequestProcessingStepFailure : RequestProcessingStep
// {
//  /// <summary>
//     /// A short, human-readable message briefly describing the failure that occurred. 
//     /// </summary>
//     public string Error { get; set; } = string.Empty;

//     /// <summary>
//     /// A longer, human-readable message describing the failure that occurred with more detail.
//     /// </summary>
//     public string ErrorDescription { get; set; } = string.Empty;
// }
