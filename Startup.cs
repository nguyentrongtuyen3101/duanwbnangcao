using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Linq;
using System.Threading.Tasks;

[assembly: OwinStartup(typeof(doanwebnangcao.Startup))]

namespace doanwebnangcao
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            System.Diagnostics.Debug.WriteLine("OWIN Configuration started.");

            // Middleware ghi log pipeline
            app.Use(async (context, next) =>
            {
                System.Diagnostics.Debug.WriteLine("Entering OWIN Pipeline.");
                await next.Invoke();
                System.Diagnostics.Debug.WriteLine("Exiting OWIN Pipeline.");
                System.Diagnostics.Debug.WriteLine($"Response Status Code: {context.Response.StatusCode}");
                System.Diagnostics.Debug.WriteLine($"Response Headers: {string.Join(", ", context.Response.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
                System.Diagnostics.Debug.WriteLine($"Response Body CanWrite: {context.Response.Body.CanWrite}");
            });

            // Middleware bỏ qua Browser Link
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments(new PathString("/__browserLink")))
                {
                    System.Diagnostics.Debug.WriteLine("Browser Link request detected - ignoring: " + context.Request.Path);
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
                    System.Diagnostics.Debug.WriteLine("Entering Error Handling Middleware.");
                    await next.Invoke();
                    System.Diagnostics.Debug.WriteLine("Exiting Error Handling Middleware - No errors.");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("OWIN Pipeline Error: " + ex.Message);
                    System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                    System.Diagnostics.Debug.WriteLine("Inner Exception: " + (ex.InnerException?.Message ?? "No inner exception"));
                    context.Response.StatusCode = 302;
                    context.Response.Headers.Add("Location", new[] { "/Home/DangNhap?error=ServerError&message=An unexpected error occurred during authentication: " + ex.Message });
                    context.Response.Write("Redirecting to login page due to server error.");
                }
            });

            // Cấu hình Cookie Authentication
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "ApplicationCookie",
                LoginPath = new PathString("/Home/DangNhap"),
                CookieHttpOnly = true,
                CookieSecure = CookieSecureOption.SameAsRequest,
                CookieName = "MyAppCookie",
                ExpireTimeSpan = TimeSpan.FromMinutes(30),
                CookieSameSite = SameSiteMode.Lax,
                Provider = new CookieAuthenticationProvider
                {
                    OnException = context =>
                    {
                        System.Diagnostics.Debug.WriteLine("Cookie Authentication Error: " + context.Exception.Message);
                    },
                    OnResponseSignIn = context =>
                    {
                        System.Diagnostics.Debug.WriteLine("Cookie being signed in: " + context.Identity?.Name);
                        // Ngăn redirect tự động bằng cách đặt RedirectUri là null
                        context.Properties.RedirectUri = null;
                    },
                    OnResponseSignedIn = context =>
                    {
                        System.Diagnostics.Debug.WriteLine("Cookie signed in successfully: " + context.Identity?.Name);
                    }
                }
            });

            System.Diagnostics.Debug.WriteLine("OWIN Configuration completed.");
        }
    }
}