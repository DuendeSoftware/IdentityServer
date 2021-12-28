using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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
        [BindProperty]
        public string Button { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            InputModel = await _repository.GetByIdAsync(id);
            if (InputModel == null)
            {
                return RedirectToPage("/Admin/Clients/Index");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            if (Button == "delete")
            {
                await _repository.DeleteAsync(id);
                return RedirectToPage("/Admin/Clients/Index");
            }

            if (ModelState.IsValid)
            {
                await _repository.UpdateAsync(InputModel);
                return RedirectToPage("/Admin/Clients/Edit", new { id });
            }

            return Page();
        }
    }
}
