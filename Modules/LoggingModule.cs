using System;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace doanwebnangcao.Modules
{
    public class LoggingModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += (sender, e) =>
            {
                var app = (HttpApplication)sender;
                Debug.WriteLine($"HttpModule - BeginRequest: {app.Request.Url}");
            };

            context.PreRequestHandlerExecute += (sender, e) =>
            {
                var app = (HttpApplication)sender;
                Debug.WriteLine($"HttpModule - PreRequestHandlerExecute: {app.Request.Url}");
                Debug.WriteLine($"HttpContext exists: {app.Context != null}");
                Debug.WriteLine($"Response Status Code: {app.Response.StatusCode}");
                Debug.WriteLine($"Response Headers: {string.Join(", ", app.Response.Headers.AllKeys.Select(k => $"{k}: {app.Response.Headers[k]}"))}");
            };

            context.EndRequest += (sender, e) =>
            {
                var app = (HttpApplication)sender;
                Debug.WriteLine($"HttpModule - EndRequest: {app.Request.Url}");
            };
        }

        public void Dispose() { }
    }
}