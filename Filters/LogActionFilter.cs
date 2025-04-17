using System.Diagnostics;
using System.Web.Mvc;

namespace doanwebnangcao.Filters
{
    public class LogActionFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            Debug.WriteLine("OnActionExecuting called.");
            Debug.WriteLine($"Controller: {filterContext.Controller?.GetType().Name ?? "Null"}");
            Debug.WriteLine($"Action: {filterContext.ActionDescriptor?.ActionName ?? "Null"}");
            Debug.WriteLine($"Request URL: {filterContext.HttpContext?.Request.Url}");
            Debug.WriteLine($"HttpContext exists: {filterContext.HttpContext != null}");
            Debug.WriteLine($"HttpContext.User exists: {filterContext.HttpContext?.User != null}");
            Debug.WriteLine($"HttpContext.User.Identity.IsAuthenticated: {filterContext.HttpContext?.User?.Identity?.IsAuthenticated ?? false}");
            Debug.WriteLine($"HttpContext.Request.IsAuthenticated: {filterContext.HttpContext?.Request.IsAuthenticated}");
            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            Debug.WriteLine("OnActionExecuted called.");
            if (filterContext.Exception != null)
            {
                Debug.WriteLine($"Exception in action: {filterContext.Exception.Message}");
                Debug.WriteLine($"Stack Trace: {filterContext.Exception.StackTrace}");
            }
            base.OnActionExecuted(filterContext);
        }
    }
}