namespace Shanto_VilaManagement_System.Services
{
    public interface IDatabaseBackupService
    {
        Task<byte[]> GenerateSqlBackupAsync();
    }
}
