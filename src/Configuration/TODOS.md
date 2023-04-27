# TODOS 
- Consider how the design will evolve to support DCM
- Create a standalone host
- Create a console client
- Create a sample that requires use of extensibility points to set client id and secret (scenario is a CI/CD pipeline needs to create the client)

# Client Properties not supported, yet
## Client Claims
- AlwaysSendClientClaims
- Claims
- ClientClaimsPrefix

## DPOP
- RequireDPoP
- DPoPValidationMode
- DPoPClockSkew

## Device Code
- DeviceCodeLifetime
- UserCodeType

## Ciba
- CibaLifetime
- PollingInterval

## Properties that we want to encourage use of defaults
- RequirePkce
- AllowPlainTextPkce
- AlwaysIncludeUserClaimsInIdToken
- IncludeJwtId
- PairWiseSubjectSalt
- Description

