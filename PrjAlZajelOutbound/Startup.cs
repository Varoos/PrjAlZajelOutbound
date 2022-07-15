using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PrjAlZajelOutbound.Startup))]
namespace PrjAlZajelOutbound
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
