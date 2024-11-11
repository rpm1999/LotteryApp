using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LotteryApp.Entities.Config;
using LotteryApp.Services;

class Program
{
    static void Main(string[] args)
    {
        // Set up the configuration to read from appsettings
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        IConfiguration configuration = builder.Build();

        // Create a service collection and configure the dependencies
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection, configuration);

        // Build the service provider and start the lottery service
        var serviceProvider = serviceCollection.BuildServiceProvider();
        var lotteryService = serviceProvider.GetService<ILotteryGameService>();
        lotteryService?.StartLottery();
    }

    private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(configuration);

        // Configure logging
        services.AddLogging(configure => configure.AddConsole())
            .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Information);

        // Bind LotteryConfig
        var lotteryConfig = new LotteryConfig();
        configuration.GetSection("LotteryConfig").Bind(lotteryConfig);
        services.AddSingleton(lotteryConfig);

        // Register services
        services.AddSingleton<IPlayerService, PlayerService>();
        services.AddSingleton<ITicketService, TicketService>();
        services.AddSingleton<ILotteryGameService, LotteryGameService>();
    }
}