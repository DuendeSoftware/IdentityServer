using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServerHost.Pages.Admin.Clients
{
    [SecurityHeaders]
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ClientRepository _repository;

        public IndexModel(ClientRepository repository)
        {
            _repository = repository;
        }

        public IEnumerable<ClientModel> Clients { get; private set; }

        public async Task OnGetAsync()
        {
            Clients = await _repository.GetAllAsync();
        }
    }
}
