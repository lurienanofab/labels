using Microsoft.Owin;
using Owin;
using System.Web.Mvc;
using System.Web.Routing;
using LNF.Impl;
using System.Web.Http;
using System.Net.Http;


[assembly: OwinStartup(typeof(Labels.Startup))]

namespace Labels
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.UseDataAccess();

            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            var config = new HttpConfiguration();
            WebApiConfig.Register(config);

            app.UseWebApi(config);
        }
    }
}
