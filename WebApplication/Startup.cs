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
using System;
using WebApplication.Models;
using WebApplication.Security;

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
                options.SignIn.RequireConfirmedEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

            }).AddEntityFrameworkStores<AppDbContext>()
              .AddDefaultTokenProviders();

            // Set token life span to 5 hours
            services.Configure<DataProtectionTokenProviderOptions>(o =>
                o.TokenLifespan = TimeSpan.FromHours(5));


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

            services.AddSingleton<DataProtectionPurposeStrings>();

            //Enable Google Authentication in ASP.NET Core
            services.AddAuthentication().AddGoogle(options =>
            {
                options.ClientId = "663220105446-2t9vrqm455d1qqbgqdse4rpcf7cgmlte.apps.googleusercontent.com";
                options.ClientSecret = "u0zBFlfPiom10ZJ2Ua6HDLJ8";
            })
            .AddFacebook(options =>
            {
                options.AppId = "472960623579323";
                options.AppSecret = "eef15e903d5dc159c9bb2e2c95fd9cec";
            });
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
