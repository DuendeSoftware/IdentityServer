// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authentication.Cookies;

namespace Duende.SessionManagement;

/// <summary>
/// Custom type for ITicketStore
/// </summary>
// This is here really just to avoid possible confusion of any other ITicketStore already in the DI system.
public interface IServerSideTicketStore : ITicketStore { }
