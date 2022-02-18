using Duende.SessionManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace IdentityServerHost.Pages.ServerSideSessions
{
    public class IndexModel : PageModel
    {
        private readonly ISessionManagementService _sessionManagementService;

        public IndexModel(ISessionManagementService sessionManagementService)
        {
            _sessionManagementService = sessionManagementService;
        }

        public QueryResult<UserSession> UserSessions { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Filter { get; set; }
        [BindProperty(SupportsGet = true)]
        public int P { get; set; } = 1;

        public async Task OnGet()
        {
            UserSessions = await _sessionManagementService.QuerySessionsAsync(new QueryFilter
            {
                Page = P,
                Count = 10,
                DisplayName = Filter,
                SessionId = Filter,
                SubjectId = Filter,
            });
        }

        [BindProperty]
        public string SessionId { get; set; }

        public async Task<IActionResult> OnPost()
        {
            await _sessionManagementService.RemoveSessionsAsync(new RemoveSessionsContext { 
                SessionId = SessionId,
            });
            return RedirectToPage("/ServerSideSessions/Index", new { p = P, filter = Filter });
        }
    }
}
