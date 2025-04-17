using System;
using System.Diagnostics;
using System.Web.Mvc;
using System.Web.Routing;

namespace doanwebnangcao
{
    public class MvcStartup_ver2 : System.Web.HttpApplication
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error in Application_Start: " + ex.Message);
                System.Diagnostics.Debug.WriteLine("Stack Trace: " + ex.StackTrace);
                throw;
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
        }
    }
}