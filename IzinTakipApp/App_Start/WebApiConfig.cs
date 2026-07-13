using System.Web.Http;
using System.Web.Http.Cors;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace IzinTakipApp
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API rotaları
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            // 🚨 500 HATASINI BİTİREN KRİTİK JSON AYARLARI
            var json = config.Formatters.JsonFormatter;

            // Nesne ilişkilerindeki döngüsel kilitlenmeleri görmezden gel ve engelle:
            json.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            // Nesnelerin JavaScript dünyasına camelCase (küçük harfle başlayan) gitmesini sağla:
            json.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            json.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;

            // XML çıktısını kapat
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}