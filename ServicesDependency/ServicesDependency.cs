using NLog;
using NLog.Web;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.DisruptionHandlerService;
using RECO.DistrubtionHandler_MS.DistrubtionHandlerService;
using RECO.DistrubtionHandler_MS.DistrubtionHandlerService.Interface;
using RECO.DistrubtionHandler_MS.Models.RequestModel;
using RECO.DistrubtionHandler_MS.HealthChecking;
using RECO.DistrubtionHandler_MS.IUtilities;
using RECO.DistrubtionHandler_MS.Utilities;
using RECO.DistrubtionHandler_MS.Models.UCGModel;
using RECO.DistrubtionHandler_MS.UCGHandlerService.Interface;
using RECO.DistrubtionHandler_MS.UCGHandlerService;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using RECO.DistrubtionHandler_MS.Middlewares;

namespace RECO.DistrubtionHandler_MS.ServicesDependency
{
    public static class ServicesDependency
    {
        public static void RegisterServices(this WebApplicationBuilder builder)
        {
            //To fetch the secret/password/gateway key from OCP ESO
            builder.Configuration.AddJsonFile("secrets/secrets.json", optional: true, reloadOnChange: true);
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = builder.Configuration["AzureConfig:Authority"];
                options.Audience = builder.Configuration["AzureConfig:Audience"];
                options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true, // Ensure token lifetime is validated
                    ClockSkew = TimeSpan.Zero // Optional: Adjust clock skew
                };
            });
            builder.Services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
            });
            builder.Services.AddControllers();

            // Configure TLS 1.2 or higher
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ConfigureHttpsDefaults(httpsOptions =>
                {
                    httpsOptions.SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13;
                });
            });

            ///Nlog configuration
            NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("CustomPolicy", policy =>
                    policy.Requirements.Add(new CustomRequirement(builder.Configuration["InternalAuthorization"].ToString())));
            });

            builder.Services.AddSingleton<IAuthorizationHandler, CustomRequirementHandler>();

            builder.Logging.ClearProviders();
            builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Host.UseNLog();
            builder.Services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });
            var allowedOrigins = builder.Configuration["Originurl"];
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MyPolicy", builder =>
                {
                    builder.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
                });
            });

            builder.Services.AddDistributedMemoryCache();
            //builder.Services.AddResponseCaching();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSingleton<ILogHelper, LogHelper>();

            builder.Services.Configure<AuthConfigurationRequestModel>(builder.Configuration.GetSection("AuthConfigurationModel"));
            builder.Services.Configure<UCG_CredentialsModel>(builder.Configuration.GetSection("UCG_Credentials"));
            builder.Services.AddHttpClient<IAuthService, AuthService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:AuthorizationService"]);
            });
            builder.Services.AddHttpClient<ICOBRosterService, COBRosterService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["COBEndpoint"]);
            });
            builder.Services.AddHttpClient<INavitaireService, NavitaireService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:TripInfoLegKeyStatus"]);
            });
            builder.Services.AddHttpClient<ITripInfoLegsService, TripInfoLegsService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:TripInfoLegs"]);
            });
            builder.Services.AddHttpClient<IDataMsService, DataMsService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["InternalMSAPI:RecoDataMS"]);
            });
            builder.Services.AddHttpClient<IRulesMSService, RulesMSService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["InternalMSAPI:RecoRulesMS"]);
            });
            builder.Services.AddHttpClient<INotificationHUBService, NotificationHUBService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["NotificationHUB"]);
            });

            builder.Services.AddHttpClient<IReaccommodationMSService, ReaccommodationMSService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["InternalMSAPI:RecoReaccommodationMS"]);
            });
            builder.Services.AddTransient<INotificationTemplateService, NotificationTemplateService>();
            builder.Services.AddTransient<IHandleDisruptedFlightService, DistrupitonHandlerServices>();
            builder.Services.AddTransient<IBaseService, BaseService>();
            builder.Services.AddTransient<IUCGWebServices, UCGWebServices>();
           
            builder.Services.AddTransient<ILogHelper, LogHelper>();
            builder.Services.AddHealthChecks()
            .AddCheck<CustomCheck>("Todo Health Check", tags: new[] { "custom" });


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }
    }
}
