using Shanto_VilaManagement_System.Models;
using Shanto_VilaManagement_System.ViewModels;

namespace Shanto_VilaManagement_System.Services
{
    public interface IBillService
    {
        Task<decimal> GetPreviousReadingAsync(int roomId, int targetYear, int targetMonth);
        Task<decimal> GetPreviousDueAsync(int roomId, int targetYear, int targetMonth);
        Task<MonthlyBill> GenerateOrUpdateBillAsync(int roomId, int year, int month, decimal currentReading, decimal? customPreviousReading = null, decimal? manualPreviousDue = null);
        Task<bool> RecordPaymentAsync(int billId, string paymentType, decimal? customAmount, DateTime paidDate);
        Task<bool> MarkBillAsPaidAsync(int billId, DateTime paidDate);
        Task<bool> MarkBillAsUnpaidAsync(int billId);
        Task<DashboardViewModel> GetDashboardStatsAsync(int? year = null, int? month = null);
        Task<BulkMeterReadingViewModel> PrepareBulkReadingViewModelAsync(int year, int month);
        Task<(int SuccessCount, List<string> Errors)> ProcessBulkReadingAsync(BulkMeterReadingViewModel model);
    }
}
