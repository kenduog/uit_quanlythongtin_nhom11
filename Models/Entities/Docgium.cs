using System;
using System.Collections.Generic;

namespace Nhom11.Models.Entities;

public partial class Docgium
{
    public string MaDocGia { get; set; } = null!;

    public string HoTen { get; set; } = null!;

    public DateOnly? NgayLapThe { get; set; }

    public DateOnly NgayHetHan { get; set; }

    public decimal? TongNo { get; set; }

    public string? TrangThai { get; set; }

    public virtual ICollection<Phieumuon> Phieumuons { get; set; } = new List<Phieumuon>();
}
