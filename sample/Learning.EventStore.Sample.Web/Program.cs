using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace Learning.EventStore.Sample.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Build().Run();
        }

        public static IWebHostBuilder BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
