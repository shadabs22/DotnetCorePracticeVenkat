using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WebApplication.Models;
using WebApplication.Security;
using WebApplication.ViewModels;

namespace WebApplication
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940

        private IConfiguration _config;

        public Startup(IConfiguration _config)
        {
            this._config = _config;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContextPool<AppDbContext>(
                options => options.UseSqlServer(_config.GetConnectionString("EmployeeDBConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                //options.Password.RequiredLength = 10;
                //options.Password.RequiredUniqueChars = 3;
                //options.Password.RequireNonAlphanumeric = false;
            }).AddEntityFrameworkStores<AppDbContext>();

            services.ConfigureApplicationCookie(options =>
            {
                options.AccessDeniedPath = new PathString("/Administration/AccessDenied");
            });

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequiredUniqueChars = 3;
                options.Password.RequireNonAlphanumeric = false;
            });

            services.AddMvc(config =>
            {
                var policy = new AuthorizationPolicyBuilder()
                                .RequireAuthenticatedUser()
                                .Build();
                config.Filters.Add(new AuthorizeFilter(policy));
            });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("DeleteRolePolicy",
                    policy => policy.RequireClaim("Delete Role"));
                //options.AddPolicy("EditRolePolicy",
                //    policy => policy.RequireClaim("Edit Role", "true"));

                //options.AddPolicy("EditRolePolicy",
                //    policy => policy.RequireRole("Admin")
                //    .RequireClaim("Edit Role", "true")
                //    .RequireRole("Super Admin")
                //                );

                //options.AddPolicy("EditRolePolicy", policy => policy.RequireAssertion(context =>
                //    context.User.IsInRole("Admin") &&
                //    context.User.HasClaim(claim => claim.Type == "Edit Role" && claim.Value == "true") ||
                //    context.User.IsInRole("Super Admin")));

                options.AddPolicy("EditRolePolicy", policy => policy.AddRequirements(new ManageAdminRolesAndClaimsRequirement()));

                options.InvokeHandlersAfterFailure = false;

                options.AddPolicy("AdminRolePolicy",
                    policy => policy.RequireClaim("Admin Role"));
            });

            // Register the first handler
            services.AddSingleton<IAuthorizationHandler, CanEditOnlyOtherAdminRolesAndClaimsHandler>();
            // Register the second handler
            services.AddSingleton<IAuthorizationHandler, SuperAdminHandler>();

            //services.AddTransient<IEmployeeRepository, MockEmployeeRepository>();
            services.AddTransient<IEmployeeRepository, SQLEmployeeRepository>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env
            , ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //else
            {
                //app.UseStatusCodePagesWithReExecute("/Error/{0}");
                //app.UseStatusCodePagesWithRedirects("/Error/{0}");
                //app.UseStatusCodePages();
                app.UseExceptionHandler("/Error1");
                app.UseStatusCodePagesWithReExecute("/Error/{0}");
            }
            //app.Use(async (context, next) =>
            //{
            //    logger.LogInformation("M1:Start");
            //    await next();
            //    logger.LogInformation("M1:End");
            //    await context.Response.WriteAsync("BEFORE RESPONSE");
            //    await next();
            //    await context.Response.WriteAsync("AFTER RESPONSE");
            //});
            app.UseStaticFiles();
            app.UseAuthentication();
            //app.UseMvcWithDefaultRoute();
            app.UseMvc(routes =>
            {
                routes.MapRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });

            //app.Run(async (context) =>
            //{
            //    await context.Response.WriteAsync("Hello World!");
            //});
        }
    }
}
