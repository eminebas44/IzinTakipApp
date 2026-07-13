using System.Web.Mvc;
using System.Web.Routing;

namespace IzinTakipApp
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Eğer projenin kök dizinine (/) istek gelirse index.html'e izin ver
            routes.IgnoreRoute("");
            routes.IgnoreRoute("index.html");
            routes.IgnoreRoute("personel.html");
            routes.IgnoreRoute("admin.html");

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}