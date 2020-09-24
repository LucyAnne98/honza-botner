using System;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using HonzaBotner.Discord.Services;
using HonzaBotner.Discord.Services.Messages;
using HonzaBotner.Discord.Services.Pools;
using HonzaBotner.Data;
using HonzaBotner.Discord;
using HonzaBotner.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using HonzaBotner.Core.Contract;

namespace HonzaBotner
{

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddHttpContextAccessor();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(Configuration["CVUT:ConnectionString"]));
            services.AddDefaultIdentity<IdentityUser>(options =>
            {
                options.SignIn.RequireConfirmedEmail = false;
                options.SignIn.RequireConfirmedAccount = false;
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "CVUT";
                })
                .AddCookie()
                .AddOAuth("CVUT", "CVUT Login", options =>
                {
                    options.AuthorizationEndpoint = "https://auth.fit.cvut.cz/oauth/authorize";
                    options.TokenEndpoint = "https://auth.fit.cvut.cz/oauth/token";
                    options.UserInformationEndpoint = "https://auth.fit.cvut.cz/oauth/check_token";

                    options.CallbackPath = "/signin-oidc";

                    options.Scope.Add("urn:ctu:oauth:umapi.read");
                    options.Scope.Add("cvut:umapi:read");

                    options.ClientId = Configuration["CVUT:ClientId"];
                    options.ClientSecret = Configuration["CVUT:ClientSecret"];

                    var innerHandler = new HttpClientHandler();
                    options.BackchannelHttpHandler = new AuthorizingHandler(innerHandler, options);
                    options.SaveTokens = true;
                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = OAuthOnCreating
                    };
            });
            services.AddRazorPages();

            services.AddDiscordOptions(Configuration)
                .AddDiscordBot(config =>
            {
                config.AddCommand<HiCommand>(HiCommand.ChatCommand);
                config.AddCommand<AuthorizeCommand>(AuthorizeCommand.ChatCommand);
                config.AddCommand<Activity>(Activity.ChatCommand);
                // Messages
                config.AddCommand<SendMessage>(SendMessage.ChatCommand);
                config.AddCommand<EditMessage>(EditMessage.ChatCommand);
                config.AddCommand<SendImage>(SendImage.ChatCommand);
                config.AddCommand<EditImage>(EditImage.ChatCommand);
                // Pools
                config.AddCommand<YesNo>(YesNo.ChatCommand);
                config.AddCommand<Abc>(Abc.ChatCommand);
            });

            services.AddScoped<IAccessTokenProvider, AccessTokenProvider>();
            services.AddBotnerServicesOptions(Configuration)
                .AddHttpClient()
                .AddBotnerServices();
        }

        private async Task OAuthOnCreating(OAuthCreatingTicketContext context)
        {
            string? userName = await GetUserName(context);
            if (userName == null)
            {
                throw new InvalidOperationException();
            }

            context.Identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, $"{userName}@fit.cvut.cz"));
            context.Identity.AddClaim(new Claim(ClaimTypes.Email, $"{userName}@fit.cvut.cz")); // HACK: FIX IT
            context.Identity.AddClaim(new Claim(ClaimTypes.Name, userName));

            context.RunClaimActions();
        }

        private static async Task<string?> GetUserName(OAuthCreatingTicketContext context)
        {
            var uriBuilder = new UriBuilder(context.Options.UserInformationEndpoint);
            uriBuilder.Query = $"token={context.AccessToken}";
            var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

            HttpResponseMessage response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
            response.EnsureSuccessStatusCode();

            string responseText = await response.Content.ReadAsStringAsync();
            var user = JsonDocument.Parse(responseText);

            return user.RootElement.GetProperty("user_name").GetString();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}