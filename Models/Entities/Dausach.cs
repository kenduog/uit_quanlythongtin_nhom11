using System;
using System.Collections.Generic;

namespace Nhom11.Models.Entities;

public partial class Dausach
{
    public string MaDauSach { get; set; } = null!;

    public string TenSach { get; set; } = null!;

    public string TacGia { get; set; } = null!;

    public string TheLoai { get; set; } = null!;

    public virtual ICollection<Cuonsach> Cuonsaches { get; set; } = new List<Cuonsach>();
}
