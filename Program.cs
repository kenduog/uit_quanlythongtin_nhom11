using Microsoft.AspNetCore.HttpOverrides;
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
builder.Services.AddScoped<IThuTucService, ThuTucService>();

// Dev-only: tự bật Cloudflare Tunnel khi chạy app và in link public ra console.
// Tắt bằng "Tunnel:Enabled": false trong appsettings nếu không muốn chia sẻ.
if (builder.Environment.IsDevelopment() && builder.Configuration.GetValue("Tunnel:Enabled", true))
    builder.Services.AddHostedService<CloudflareTunnelService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Khi chia sẻ qua tunnel/proxy (VS Code Port Forwarding, dev tunnels, ...) để test:
// đọc X-Forwarded-Proto/Host để UseHttpsRedirection không redirect sai và link bên ngoài
// hoạt động đúng. Chỉ bật ở môi trường Development.
if (app.Environment.IsDevelopment())
{
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost
    };
    forwardedOptions.KnownNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedOptions);
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
