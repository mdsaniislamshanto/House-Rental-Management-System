using Microsoft.EntityFrameworkCore;
using Shanto_VilaManagement_System.Models;

namespace Shanto_VilaManagement_System.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Room> Rooms { get; set; } = null!;
        public DbSet<Resident> Residents { get; set; } = null!;
        public DbSet<EmergencyContact> EmergencyContacts { get; set; } = null!;
        public DbSet<MonthlyBill> MonthlyBills { get; set; } = null!;
        public DbSet<Setting> Settings { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Room -> Residents
            modelBuilder.Entity<Resident>()
                .HasOne(r => r.Room)
                .WithMany(rm => rm.Residents)
                .HasForeignKey(r => r.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Room -> EmergencyContacts
            modelBuilder.Entity<EmergencyContact>()
                .HasOne(e => e.Room)
                .WithMany(rm => rm.EmergencyContacts)
                .HasForeignKey(e => e.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Room -> MonthlyBills
            modelBuilder.Entity<MonthlyBill>()
                .HasOne(b => b.Room)
                .WithMany(rm => rm.MonthlyBills)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for fast billing query and unique room bill per month
            modelBuilder.Entity<MonthlyBill>()
                .HasIndex(b => new { b.RoomId, b.BillingYear, b.BillingMonthNumber })
                .IsUnique();

            modelBuilder.Entity<MonthlyBill>()
                .HasIndex(b => b.RoomId);
        }
    }
}
