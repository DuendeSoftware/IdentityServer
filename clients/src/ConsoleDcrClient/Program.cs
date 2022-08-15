using System;
using System.Net.Http;
using System.Text.Json;
using Clients;
using IdentityModel.Client;

var client = new HttpClient();

var document = new DynamicClientRegistrationDocument
{
    GrantTypes = { "client_credentials" },
    Scope = "api1"
};

var response = await client.RegisterClientAsync(new DynamicClientRegistrationRequest
{
    Address = Constants.Authority + "/connect/dcr",
    Document = document
});

if (response.IsError)
{
    Console.WriteLine(response.Error);
    return;
}

Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));

Console.ReadLine();

document = new DynamicClientRegistrationDocument
{
    GrantTypes = { "client_credentials", "authorization_code", "refresh_token" },
    RedirectUris = { new Uri("https://client/cb") },
    Scope = "api1"
};

response = await client.RegisterClientAsync(new DynamicClientRegistrationRequest
{
    Address = Constants.Authority + "/connect/dcr",
    Document = document
});

if (response.IsError)
{
    Console.WriteLine(response.Error);
    return;
}

Console.WriteLine(JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));