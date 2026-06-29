using Microsoft.EntityFrameworkCore;
using Nhom11.Models.Entities;

namespace Nhom11.Data;

public partial class DoAnNhom11Context
{
    /// <summary>
    /// Cấu hình bổ sung (không bị ghi đè khi scaffold lại).
    /// Báo EF Core rằng các bảng có trigger → không dùng OUTPUT clause.
    /// </summary>
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Phieumuon>(e => e.ToTable(tb => tb.HasTrigger("TRG_ChanMuonSach")));
        modelBuilder.Entity<ChitietPm>(e => e.ToTable(tb => tb.HasTrigger("TRG_XuatKho")));
        modelBuilder.Entity<Docgium>(e => e.ToTable(tb => tb.HasTrigger("TRG_KhoaThe")));
    }
}
