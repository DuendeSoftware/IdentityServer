using Duende.SessionManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace IdentityServerHost.Pages.Admin.Users
{
    public class IndexModel : PageModel
    {
        private readonly IUserSessionStore _userSessionStore;

        public IndexModel(IUserSessionStore userSessionStore)
        {
            _userSessionStore = userSessionStore;
        }

        public GetAllUserSessionsResult UserSessions { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Filter { get; set; }

        public async Task OnGet(int p = 1)
        {
            UserSessions = await _userSessionStore.GetAllUserSessionsAsync(new GetAllUserSessionsFilter
            {
                Page = p,
                Count = 10,
                DisplayName = Filter,
                SessionId = Filter,
                SubjectId = Filter,
            });
        }

        [BindProperty]
        public string Key { get; set; }

        public async Task<IActionResult> OnPost()
        {
            await _userSessionStore.DeleteUserSessionAsync(Key);
            return RedirectToPage("/Admin/Users/Index");
        }
    }
}
