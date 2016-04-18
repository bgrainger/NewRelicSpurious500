using System;
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
				SetTransactionName("Api", "Values");
		}
	}
}
