using BackgroundEmailSenderSample.Models.Services.Application;
using BackgroundEmailSenderSample.HostedServices;
using BackgroundEmailSenderSample.Models.Options;
using BackgroundEmailSenderSample.Models.Services.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BackgroundEmailSenderSample
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddHostedService<EmailSenderHostedService>();                  // OPZIONE 1 commentare OPZIONE 2
            //services.AddSingleton<IHostedService, EmailSenderHostedService>();    // OPZIONE 2 commentare OPZIONE 1
            
            services.AddTransient<IBackgroundEmailSenderService, BackgroundEmailSenderService>();
            services.AddDbContextPool<MyEmailSenderDbContext>(optionsBuilder => {
                string connectionString = Configuration.GetSection("ConnectionStrings").GetValue<string>("Default");
                optionsBuilder.UseSqlite(connectionString);
            });
            services.Configure<SmtpOptions>(Configuration.GetSection("Smtp"));
        }

        public void Configure(IApplicationBuilder app, IHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(routeBuilder =>
            {
                routeBuilder.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
