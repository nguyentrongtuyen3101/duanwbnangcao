using System.Diagnostics;
using System.Web.Mvc;
using System.Web.Routing;

namespace doanwebnangcao
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            System.Diagnostics.Debug.WriteLine("RegisterRoutes called.");

            // Đặt MapRoute trước IgnoreRoute để tránh xung đột
            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "DangNhap", id = UrlParameter.Optional }
            );

            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            System.Diagnostics.Debug.WriteLine("Routes configured.");

            // Thêm kiểm tra null khi truy cập Defaults
            foreach (var route in routes)
            {
                if (route is Route r)
                {
                    var controller = r.Defaults != null && r.Defaults.ContainsKey("controller") ? r.Defaults["controller"] : "N/A";
                    var action = r.Defaults != null && r.Defaults.ContainsKey("action") ? r.Defaults["action"] : "N/A";
                    System.Diagnostics.Debug.WriteLine($"Route: {r.Url}, Controller: {controller}, Action: {action}");
                }
            }
        }
    }
}