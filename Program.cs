using Microsoft.EntityFrameworkCore;
using Nhom11.Data;
using Nhom11.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// EF Core - kết nối database DoAn_Nhom11.
builder.Services.AddDbContext<DoAnNhom11Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cấu hình nghiệp vụ + các service.
builder.Services.Configure<ThuVienOptions>(builder.Configuration.GetSection("ThuVien"));
builder.Services.AddScoped<MaGenerator>();
builder.Services.AddScoped<IPhieuMuonService, PhieuMuonService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
