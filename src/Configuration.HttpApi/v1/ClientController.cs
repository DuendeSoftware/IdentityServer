using Duende.IdentityServer.Configuration.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Duende.IdentityServer.Configuration.WebApi.v1;

[ApiController]
[Route("/api/v1/clients")]
public class ClientController : ControllerBase
{
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<ClientController> _logger;

    public ClientController(
        IClientRepository clientRepository,
        ILogger<ClientController> logger)
    {
        _clientRepository = clientRepository;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateClientResponse), 200, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), 400, "application/problem+json")]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CreateClient(CreateClientRequest request)
    {
        var client = new Models.Client
        {
            ClientId = request.ClientId // TODO are we going to allow callers specify client identifiers?
            // TODO automapper or not rest of properties?
        };

        await _clientRepository.Add(client); // TODO exception handling. ProblemDetails ?

        var response = new CreateClientResponse
        {
            ClientId = Guid.NewGuid().ToString(),
        };

        return new OkObjectResult(response);
    }

    [HttpPut("{clientId}")]
    [ProducesResponseType(typeof(PutClientResponse), 200, "application/json")]
    [ProducesResponseType(typeof(ProblemDetails), 400, "application/problem+json")]
    [ProducesResponseType(typeof(ProblemDetails), 409, "application/problem+json")]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> PutClient(string clientId, PutClientRequest request)
    {
        var client = new Models.Client
        {
            ClientId = clientId
            // TODO automapper or not rest of properties?
        };

        await _clientRepository.Update(client);

        var response = new PutClientResponse
        {
            ClientId = client.ClientId,
            Version = 1 // TODO do we want versioning an explicit concept? Need to implement in repository too.
        };

        return new OkObjectResult(response);
    }

    [HttpGet("{clientId}")]
    [ProducesResponseType(typeof(GetClientResponse), 200, "application/json")]
    [ProducesResponseType(401)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> GetClient(string clientId)
    {
        var client = await _clientRepository.Read(clientId);

        if (client == null)
        {
            return NotFound();
        }

        var response = new GetClientResponse
        {
            ClientId = client.ClientId,
            // TODO automapper or not rest of properties?
        };

        return new OkObjectResult(response);
    }
}