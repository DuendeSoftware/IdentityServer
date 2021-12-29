using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace IdentityServerHost.Pages.Admin.ApiScopes;

[SecurityHeaders]
[Authorize]
public class EditModel : PageModel
{
    private readonly ApiScopeRepository _repository;

    public EditModel(ApiScopeRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public ApiScopeModel InputModel { get; set; }
    [BindProperty]
    public string Button { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        InputModel = await _repository.GetByIdAsync(id);
        if (InputModel == null)
        {
            return RedirectToPage("/Admin/ApiScopes/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (Button == "delete")
        {
            await _repository.DeleteAsync(id);
            return RedirectToPage("/Admin/ApiScopes/Index");
        }

        if (ModelState.IsValid)
        {
            await _repository.UpdateAsync(InputModel);
            return RedirectToPage("/Admin/ApiScopes/Edit", new { id });
        }

        return Page();
    }
}