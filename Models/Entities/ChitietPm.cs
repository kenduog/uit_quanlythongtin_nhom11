using System;
using System.Collections.Generic;

namespace Nhom11.Models.Entities;

public partial class ChitietPm
{
    public string MaCuonSach { get; set; } = null!;

    public string MaPhieuMuon { get; set; } = null!;

    public DateOnly? NgayTraThucTe { get; set; }

    public string? HienTrangKhiTra { get; set; }

    public decimal? TienPhat { get; set; }

    public virtual Cuonsach MaCuonSachNavigation { get; set; } = null!;

    public virtual Phieumuon MaPhieuMuonNavigation { get; set; } = null!;
}
