using System;
using System.Diagnostics;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using static NewRelic.Api.Agent.NewRelic;

namespace WebApplication1
{
	public class WebApiApplication : System.Web.HttpApplication
	{
		protected void Application_Start()
		{
			AreaRegistration.RegisterAllAreas();
			GlobalConfiguration.Configure(WebApiConfig.Register);
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
		}

		protected void Application_BeginRequest(object sender, EventArgs eventArgs)
		{
			if (HttpContext.Current?.Request?.Path == "/api/values")
			{
				SetTransactionName("Api", "Values");
				HttpContext.Current.Response.Filter = new LoggingStream(HttpContext.Current.Response.Filter);
			}
		}
		protected void Application_EndRequest(object sender, EventArgs eventArgs)
		{
			var response = HttpContext.Current.Response;
			if (response.StatusCode == 500 && !response.IsClientConnected)
				IgnoreTransaction();

			if (HttpContext.Current?.Request?.Path == "/api/values")
			{
				var output = ((LoggingStream) response.Filter).DumpData();
				Trace.TraceInformation("Response Status: {0}", response.Status);
				Trace.TraceInformation("Response Body: {0}", output);
			}
		}
	}
}
