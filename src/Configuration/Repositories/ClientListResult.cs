namespace Duende.IdentityServer.Configuration.Repositories;

public class ClientListResult
{
    public ClientListResult(IReadOnlyCollection<ClientListItem> items, string continuationToken)
    {
        Items = items;
        ContinuationToken = continuationToken;
    }

    public IReadOnlyCollection<ClientListItem> Items { get; }

    public string ContinuationToken { get; }
}