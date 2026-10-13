using Microsoft.AspNetCore.Mvc;
using Shanto_VilaManagement_System.Services;

namespace Shanto_VilaManagement_System.Controllers
{
    public class DashboardController : Controller
    {
        private readonly IBillService _billService;

        public DashboardController(IBillService billService)
        {
            _billService = billService;
        }

        public async Task<IActionResult> Index(int? year, int? month)
        {
            var stats = await _billService.GetDashboardStatsAsync(year, month);
            return View(stats);
        }
    }
}
