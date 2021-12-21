using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServerHost.Pages.Admin.Clients
{
    [SecurityHeaders]
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly ClientRepository _repository;

        public EditModel(ClientRepository repository)
        {
            _repository = repository;
        }

        [BindProperty]
        public ClientModel InputModel { get; set; }

        public async Task OnGetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                InputModel = new ClientModel();
            }
            else
            {
                InputModel = await _repository.GetByIdAsync(id);
            }
        }
    }
}
