using Microsoft.Extensions.Logging;
using FileUploadApp.Views;
using FileUploadApp.ViewModels;
using FileUploadApp.Services;

namespace FileUploadApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register Services
            builder.Services.AddSingleton<IAuthenticationService, MockAuthenticationService>();
            builder.Services.AddSingleton<IUploadImage, MockUploadImageService>();
            builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
            builder.Services.AddSingleton<INotificationService, NotificationService>();

            // Register ViewModels
            builder.Services.AddSingleton<MainViewModel>(); // Changed to Singleton
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<HistoryViewModel>();
            builder.Services.AddTransient<UploadResultsViewModel>();

            // Register Views
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddSingleton<MainPage>(); // Changed to Singleton
            builder.Services.AddTransient<HistoryPage>();
            builder.Services.AddTransient<Views.UploadResultsPage>();

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
