using System;
using System.Collections.Generic;

namespace Nhom11.Models.Entities;

public partial class Cuonsach
{
    public string MaCuonSach { get; set; } = null!;

    public string MaDauSach { get; set; } = null!;

    public string? TrangThaiKho { get; set; }

    public string? HienTrangSach { get; set; }

    public virtual ICollection<ChitietPm> ChitietPms { get; set; } = new List<ChitietPm>();

    public virtual Dausach MaDauSachNavigation { get; set; } = null!;
}
