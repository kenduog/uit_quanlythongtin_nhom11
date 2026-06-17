using System;
using System.Collections.Generic;

namespace Nhom11.Models.Entities;

public partial class Phieumuon
{
    public string MaPhieuMuon { get; set; } = null!;

    public string MaDocGia { get; set; } = null!;

    public DateOnly? NgayMuon { get; set; }

    public DateOnly HanPhaiTra { get; set; }

    public virtual ICollection<ChitietPm> ChitietPms { get; set; } = new List<ChitietPm>();

    public virtual Docgium MaDocGiaNavigation { get; set; } = null!;
}
