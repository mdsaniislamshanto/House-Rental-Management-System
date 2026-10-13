using Shanto_VilaManagement_System.Models;

namespace Shanto_VilaManagement_System.Services
{
    public interface IPdfService
    {
        byte[] GenerateBanglaBillPdf(MonthlyBill bill, Setting setting);
        byte[] GenerateBatchA4BillsPdf(List<MonthlyBill> bills, Setting setting);
    }
}
