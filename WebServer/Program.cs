using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using WebServer;
using WebServer.Models.WebServerDB;
using WebServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//多國語系
builder.Services.AddLocalization();
builder.Services.AddControllersWithViews()
    //在 cshtml 中使用多國語言
    .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
    //在 Model 中使用多國語言
    .AddDataAnnotationsLocalization(
    options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
        factory.Create(typeof(Resource));
    });

//設定連線字串
builder.Services.AddDbContext<WebServerDBContext>(options =>
{
    options.UseSqlite(builder.Configuration.GetConnectionString("WebServerDB"));
});

// 使用 Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// 使用 Cookie
builder.Services
    .AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        //存取被拒轉跳頁面
        options.AccessDeniedPath = new PathString("/Account/Signin");
        //登入頁
        options.LoginPath = new PathString("/Account/Signin");
        //登出頁
        options.LogoutPath = new PathString("/Account/Signout");
    });

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<SiteService>();
builder.Services.AddScoped<ValidatorService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<IViewRenderService, ViewRenderService>();
builder.Services.AddScoped<WebServer.Filters.AuthorizeFilter>();

var app = builder.Build();

ServiceActivator.Configure(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

using (var serviceScope = ServiceActivator.GetScope())
{
    //從DI Container 取得 Service
    SiteService siteService = (SiteService)serviceScope.ServiceProvider.GetService(typeof(SiteService));
    var cultures = siteService?.GetCultures();
    
    var localizationOptions = new RequestLocalizationOptions()
        .SetDefaultCulture(cultures[0])//預設值
        .AddSupportedCultures(cultures)
        .AddSupportedUICultures(cultures);
    localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>
        {
            new QueryStringRequestCultureProvider(),
            new CookieRequestCultureProvider(),
            new AcceptLanguageHeaderRequestCultureProvider(),
        };
    app.UseRequestLocalization(localizationOptions);
}

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();