using Duende.SessionManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Threading.Tasks;

namespace IdentityServerHost.Pages.ServerSideSessions
{
    public class IndexModel : PageModel
    {
        private readonly IServerSideSessionStore _userSessionStore;

        public IndexModel(IServerSideSessionStore userSessionStore)
        {
            _userSessionStore = userSessionStore;
        }

        public QuerySessionsResult UserSessions { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Filter { get; set; }
        [BindProperty(SupportsGet = true)]
        public int P { get; set; } = 1;

        public async Task OnGet()
        {
            UserSessions = await _userSessionStore.QuerySessionsAsync(new QueryFilter
            {
                Page = P,
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
            await _userSessionStore.DeleteSessionAsync(Key);
            return RedirectToPage("/ServerSideSessions/Index", new { p = P, filter = Filter });
        }
    }
}
