// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using System.Text.Json;

namespace IntegrationTests.TestFramework;

public record JsonRecord(string type, JsonElement value);

public record ClaimRecord(string type, string value);