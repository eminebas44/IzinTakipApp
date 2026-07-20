using System;
using System.Web.Http;
using System.Web.Routing;

namespace IzinTakipApp
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            // GÜNCELLEME: Uygulama ilk kez aya?a kalkarken kök dizindeki NLog.config ayarlar?n? okur ve loglama motorunu ba?lat?r
            NLog.LogManager.LoadConfiguration("NLog.config");
        }
    }
}