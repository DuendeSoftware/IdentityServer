using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServerHost.Pages.Admin.IdentityScopes;

[SecurityHeaders]
[Authorize]
public class EditModel : PageModel
{
    private readonly IdentityScopeRepository _repository;

    public EditModel(IdentityScopeRepository repository)
    {
        _repository = repository;
    }

    [BindProperty]
    public IdentityScopeModel InputModel { get; set; }
    [BindProperty]
    public string Button { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        InputModel = await _repository.GetByIdAsync(id);
        if (InputModel == null)
        {
            return RedirectToPage("/Admin/IdentityScopes/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        if (Button == "delete")
        {
            await _repository.DeleteAsync(id);
            return RedirectToPage("/Admin/IdentityScopes/Index");
        }

        if (ModelState.IsValid)
        {
            await _repository.UpdateAsync(InputModel);
            return RedirectToPage("/Admin/IdentityScopes/Edit", new { id });
        }

        return Page();
    }
}