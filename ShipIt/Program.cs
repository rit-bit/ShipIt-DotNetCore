using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ShipIt
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            DotNetEnv.Env.Load();
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
