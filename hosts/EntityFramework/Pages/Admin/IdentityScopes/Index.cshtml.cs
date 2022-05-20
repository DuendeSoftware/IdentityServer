using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerHost.Pages.Admin.IdentityScopes;

[SecurityHeaders]
[Authorize]
public class IndexModel : PageModel
{
    private readonly IdentityScopeRepository _repository;

    public IndexModel(IdentityScopeRepository repository)
    {
        _repository = repository;
    }

    public IEnumerable<IdentityScopeSummaryModel> Scopes { get; private set; }
    public string Filter { get; set; }

    public async Task OnGetAsync(string filter)
    {
        Filter = filter;
        Scopes = await _repository.GetAllAsync(filter);
    }
}