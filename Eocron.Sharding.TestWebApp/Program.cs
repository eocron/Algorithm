using Eocron.Sharding.TestWebApp.IoC;

namespace Eocron.Sharding.TestWebApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ApplicationConfigurator.Configure(builder.Services);

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseMetricsAllEndpoints();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
            app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            app.Run();
        }
    }
}