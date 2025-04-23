using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Optimization; // Thêm để đăng ký Bundle
using System.Web.Routing;
using Serilog; // Thêm để ghi log bằng Serilog

namespace doanwebnangcao
{
    public class MvcApplication : System.Web.HttpApplication // Đổi tên class thành MvcApplication
    {
        protected void Application_Start()
        {
            System.Diagnostics.Debug.WriteLine("Application_Start called.");
            try
            {
                System.Diagnostics.Debug.WriteLine("Registering areas...");
                AreaRegistration.RegisterAllAreas();
                System.Diagnostics.Debug.WriteLine("Areas registered successfully.");

                System.Diagnostics.Debug.WriteLine("Registering global filters...");
                FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                System.Diagnostics.Debug.WriteLine("Global filters registered successfully.");

                System.Diagnostics.Debug.WriteLine("Registering routes...");
                RouteConfig.RegisterRoutes(RouteTable.Routes);
                System.Diagnostics.Debug.WriteLine("Routes registered successfully.");

                System.Diagnostics.Debug.WriteLine("Registering bundles...");
                BundleConfig.RegisterBundles(BundleTable.Bundles);
                System.Diagnostics.Debug.WriteLine("Bundles registered successfully.");

                AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in Application_Start: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                Log.Error(ex, "Lỗi trong Application_Start"); // Ghi log bằng Serilog
            }
            System.Diagnostics.Debug.WriteLine("Application_Start completed.");
        }

        protected void Application_Error()
        {
            var exception = Server.GetLastError();
            System.Diagnostics.Debug.WriteLine("Application_Error: " + exception?.Message);
            System.Diagnostics.Debug.WriteLine("Stack Trace: " + exception?.StackTrace);
            System.Diagnostics.Debug.WriteLine("Inner Exception: " + (exception?.InnerException?.Message ?? "No inner exception"));
            System.Diagnostics.Debug.WriteLine("Request URL: " + (Request?.Url?.ToString() ?? "Unknown"));
            System.Diagnostics.Debug.WriteLine("HTTP Method: " + (Request?.HttpMethod ?? "Unknown"));
            System.Diagnostics.Debug.WriteLine("Response Status Code: " + (Response?.StatusCode.ToString() ?? "Unknown"));

            // Ghi log bằng Serilog
            Log.Error(exception, "Lỗi toàn cục - URL: {Url}, Method: {Method}, Status: {Status}",
                Request?.Url?.ToString() ?? "Unknown",
                Request?.HttpMethod ?? "Unknown",
                Response?.StatusCode.ToString() ?? "Unknown");
        }
    }
}