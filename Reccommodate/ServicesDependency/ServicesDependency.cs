using NLog;
using NLog.Web;
using RECO.Reaccommodation_MS.HealthChecking;
using RECO.Reaccommodation_MS.Models.RequestModel;
using RECO.Reaccommodation_MS.IUtilities;
using RECO.Reaccommodation_MS.Utilities;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.PnrHandlerService;
using RECO.Reaccommodation_MS.UCGHandlerService.Interface;
using RECO.Reaccommodation_MS.UCGHandlerService;
using RECO.Reaccommodation_MS.Models.UCGModel;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Model;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey.Interface;
using RECO.Reaccommodation_MS.ReaccommodationService.Area.Journey;

namespace RECO.Reaccommodation_MS.ServicesDependency
{
    public static class ServicesDependency
    {
        public static void RegisterServices(this WebApplicationBuilder builder)
        {
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
            //To fetch the secret/password/gateway key from OCP ESO
            builder.Configuration.AddJsonFile("secrets/secrets.json", optional: true, reloadOnChange: true);
            builder.Logging.ClearProviders();
            builder.Logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            builder.Host.UseNLog();
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddSession(options => {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
            });
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("MyPolicy", builder =>
                {
                    builder.WithOrigins("*").AllowAnyMethod().AllowAnyHeader();
                });
            });
            builder.Services.AddDistributedMemoryCache();
            //builder.Services.AddResponseCaching();
            builder.Services.AddHttpContextAccessor();

            builder.Services.AddSingleton<ILogHelper, LogHelper>();
            builder.Services.Configure<AuthConfigurationRequest>(builder.Configuration.GetSection("AuthConfigurationModel"));

            builder.Services.Configure<UCG_CredentialsModel>(builder.Configuration.GetSection("UCG_Credentials"));
            // Configure Sales Force services
            builder.Services.Configure<SalesforceOptions>(builder.Configuration.GetSection("Salesforce"));
            builder.Services.Configure<QueueModel>(builder.Configuration.GetSection("QueueModel"));
           
            //****  Given Flight --> Multi Journey ***//
            builder.Services.AddHttpClient<IBookingService,BookingService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:BookingService"]);
            });
            builder.Services.AddHttpClient<IUpdateJourney, UpdateJourney>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:UpdateMoveJourneyService"]);
            });
            builder.Services.AddHttpClient<IBookingCommit, BookingCommit>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:BookingallowConcurrentChangesService"]);
            });
            builder.Services.AddHttpClient<IQueueService, QueueService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:queueService"]);
            });
            builder.Services.AddTransient<IModelService, ModelService>();
            builder.Services.AddTransient<IJourneyService, JourneyService>();
            builder.Services.AddTransient<ISpecificFlightJourneyService, SpecificFlightJourneyService>();
            builder.Services.AddTransient<IDelay_ToJourneyKey, Delay_ToJourneyKey>();
            builder.Services.AddTransient<IToJourneyKeyService, ToJourneyKeyService>();
            builder.Services.AddHttpClient<ICheckMoveAvailabilityService, CheckMoveAvailabilityService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:MoveavailabilityService"]);
            });
            builder.Services.AddHttpClient<IMoveAvailabilityServiceForNextDays, MoveAvailabilityServiceForNextDays>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:MoveavailabilityService"]);
            });
            builder.Services.AddTransient<IMultipleJourneyService, MultipleJourneyService>();
            builder.Services.AddTransient<ISingleJourneyService, SingleJourneyService>();
            builder.Services.AddHttpClient<IDataMsService, DataMsService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["InternalMSAPI:RecoDataMS"]);
            });
            builder.Services.AddHttpClient<ICheckingService, CheckingService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:checkin"]);
            });
            builder.Services.AddHttpClient<IRulesMSService, RulesMSService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["InternalMSAPI:RecoRulesMS"]);
            });
            
            builder.Services.AddHttpClient<INavitaireTripInfoStatusService, NavitaireTripInfoStatusService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:TripInfoLegKeyStatus"]);
            });
            builder.Services.AddHttpClient<INavitaireManifestLegDetailsService, NavitaireManifestLegService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:ManifestLegDetailsService"]);
            });
            builder.Services.AddHttpClient<INavitaireManifestDetailsService, NavitaireManifestDetailsService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:ManifestDetailsService"]);
            });

            builder.Services.AddHttpClient<INavitaireAuthorizationService, NavitaireAuthorizationService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["dotrezapiUrls:AuthorizationService"]);
            });

            builder.Services.AddHttpClient<INotificationMS, NotificationMS>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["InternalMSAPI:NotificationMS"]);
            });
            builder.Services.AddHttpClient<INotificationHUBService, NotificationHUBService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["NotificationHUB"]);
            });
            builder.Services.AddTransient<INotificationTemplateService, NotificationTemplateService>();
            builder.Services.AddTransient<IUCGWebServices, UCGWebServices>();

            builder.Services.AddTransient<IReaccommodationHandlerService, ReaccommodationHandlerService>();
            builder.Services.AddTransient<IPNRHandlerService, PNRHandlerService>();
            builder.Services.AddTransient<ISpecificFlightService, SpecificFlightService>();
            builder.Services.AddTransient<ISuitableFlightService, SuitableFlightService>();
            builder.Services.AddTransient<IPaxPriorityService, PaxPriorityService>();
            builder.Services.AddTransient<IPNRPriorityService, PNRPriorityService>();
            builder.Services.AddTransient<IDashboardDetailsService, DashboardDetailsService>();
            builder.Services.AddTransient<IDashboardDetailsPerPNR, DashboardDetailsPerPNR>();
            builder.Services.AddTransient<ICancelled_ToJourneyKey, Cancelled_ToJourneyKey>();
            builder.Services.AddHttpClient<ISalesForceService, SalesForceService>(client =>
            {
                client.BaseAddress = new Uri(builder.Configuration["Salesforce:InstanceUrl"]);
            });
            builder.Services.AddTransient<IBaseService, BaseService>();
            builder.Services.AddTransient<IUCGWebServices, UCGWebServices>();
            builder.Services.AddTransient<IPNR_HandlerService, PNR_HandlerService>();
            builder.Services.AddHealthChecks()
            .AddCheck<CustomCheck>("Todo Health Check", tags: new[] { "custom" });


            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
        }
    }
}
