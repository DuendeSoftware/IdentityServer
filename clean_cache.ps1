Remove-Item $env:USERPROFILE\.nuget\packages\duende.identityserver\ -Recurse -ErrorAction SilentlyContinue 
Remove-Item $env:USERPROFILE\.nuget\packages\duende.identityserver.storage\ -Recurse -ErrorAction SilentlyContinue 
Remove-Item $env:USERPROFILE\.nuget\packages\duende.identityserver.entityframework\ -Recurse -ErrorAction SilentlyContinue 
Remove-Item $env:USERPROFILE\.nuget\packages\duende.identityserver.entityframework.storage\ -Recurse -ErrorAction SilentlyContinue 
Remove-Item $env:USERPROFILE\.nuget\packages\duende.identityserver.aspnetidentity\ -Recurse -ErrorAction SilentlyContinue 

Remove-Item $env:USERPROFILE\.nuget\packages\identitymodel\ -Recurse -ErrorAction SilentlyContinue 
Remove-Item $env:USERPROFILE\.nuget\packages\IdentityModel.AspNetCore.OAuth2Introspection\ -Recurse -ErrorAction SilentlyContinue 
Remove-Item $env:USERPROFILE\.nuget\packages\IdentityServer4.AccessTokenValidation\ -Recurse -ErrorAction SilentlyContinue 