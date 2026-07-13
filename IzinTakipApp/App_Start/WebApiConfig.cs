using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace IzinTakipApp
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.EnableCors(new EnableCorsAttribute("*", "*", "*"));

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            var appXmlType = config.Formatters.XmlFormatter.SupportedMediaTypes
                .FirstOrDefault(t => t.MediaType == "application/xml");

            if (appXmlType != null)
                config.Formatters.XmlFormatter.SupportedMediaTypes.Remove(appXmlType);
        }
    }
}