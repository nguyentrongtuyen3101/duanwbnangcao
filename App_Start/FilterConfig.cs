using doanwebnangcao.Filters;
using System.Web;
using System.Web.Mvc;

namespace doanwebnangcao
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            System.Diagnostics.Debug.WriteLine("RegisterGlobalFilters called.");
            filters.Add(new HandleErrorAttribute());
            filters.Add(new LogActionFilter());
            System.Diagnostics.Debug.WriteLine("Filters configured.");
        }
    }
}
