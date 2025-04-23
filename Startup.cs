using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using Microsoft.AspNet.Identity;
using System.Globalization; // Thêm namespace để dùng CultureInfo

[assembly: OwinStartup(typeof(doanwebnangcao.Startup))]

namespace doanwebnangcao
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Cấu hình Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("Logs/log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Log.Information("OWIN Configuration started.");

            // Thiết lập culture vi-VN cho toàn bộ ứng dụng
            app.Use(async (context, next) =>
            {
                var cultureInfo = new CultureInfo("vi-VN");
                CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
                Log.Information("Set culture to vi-VN for request: {Path}", context.Request.Path);
                await next.Invoke();
            });

            // Middleware ghi log pipeline
            app.Use(async (context, next) =>
            {
                Log.Information("Entering OWIN Pipeline: {Path}", context.Request.Path);
                await next.Invoke();
                Log.Information("Exiting OWIN Pipeline. Status Code: {StatusCode}", context.Response.StatusCode);
            });

            // Middleware bỏ qua Browser Link
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments(new PathString("/__browserLink")))
                {
                    Log.Information("Browser Link request detected - ignoring: {Path}", context.Request.Path);
                    context.Response.StatusCode = 404;
                    return;
                }
                await next.Invoke();
            });

            // Middleware gỡ lỗi
            app.Use(async (context, next) =>
            {
                try
                {
                    Log.Information("Entering Error Handling Middleware.");
                    await next.Invoke();
                    Log.Information("Exiting Error Handling Middleware - No errors.");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "OWIN Pipeline Error");
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Add("Location", new[] { "/Home/DangNhap?error=ServerError&message=An unexpected error occurred during authentication: " + ex.Message });
                    context.Response.Write("Redirecting to login page due to server error.");
                }
            });

            // Cấu hình Session Cookie
            app.Use(async (context, next) =>
            {
                var sessionCookie = context.Request.Cookies["ASP.NET_SessionId"];
                if (!string.IsNullOrEmpty(sessionCookie))
                {
                    context.Response.Cookies.Append("ASP.NET_SessionId", sessionCookie, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    });
                }
                Log.Information("Processing request: {Path}", context.Request.Path);
                await next.Invoke();
            });

            // Cấu hình Cookie Authentication
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "ApplicationCookie",
                LoginPath = new PathString("/Home/DangNhap"),
                CookieHttpOnly = true,
                CookieSecure = CookieSecureOption.SameAsRequest, // Thay đổi để kiểm tra trên localhost
                CookieName = "MyAppCookie",
                ExpireTimeSpan = TimeSpan.FromMinutes(30),
                CookieSameSite = SameSiteMode.Strict,
                Provider = new CookieAuthenticationProvider
                {
                    OnException = context =>
                    {
                        Log.Error(context.Exception, "Lỗi xác thực cookie");
                    },
                    OnResponseSignIn = context =>
                    {
                        Log.Information("Cookie đang được đăng nhập: {Name}", context.Identity?.Name);
                        if (context.Identity != null)
                        {
                            foreach (var claim in context.Identity.Claims)
                            {
                                Log.Information("Claim Type: {Type}, Value: {Value}", claim.Type, claim.Value);
                            }
                        }
                    },
                    OnResponseSignedIn = context =>
                    {
                        Log.Information("Cookie đã được đăng nhập thành công: {Name}", context.Identity?.Name);
                    },
                    OnValidateIdentity = context =>
                    {
                        Log.Information("Đang xác thực identity cho yêu cầu: {Path}", context.OwinContext.Request.Path);
                        if (context.Identity != null)
                        {
                            foreach (var claim in context.Identity.Claims)
                            {
                                Log.Information("Claim Type: {Type}, Value: {Value}", claim.Type, claim.Value);
                            }
                        }
                        return Task.FromResult(0);
                    }
                }
            });
            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);
            Log.Information("OWIN Configuration completed.");
        }
    }
}