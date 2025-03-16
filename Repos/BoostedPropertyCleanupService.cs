using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Propertease.Models;

namespace Propertease.Services
{
    public class BoostedPropertyCleanupService : BackgroundService
    {
        private readonly ILogger<BoostedPropertyCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public BoostedPropertyCleanupService(
            ILogger<BoostedPropertyCleanupService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Boosted Property Cleanup Service running.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking for expired boosted properties at: {time}", DateTimeOffset.Now);

                try
                {
                    await DeactivateExpiredBoosts();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while deactivating expired boosts.");
                }

                // Run every hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task DeactivateExpiredBoosts()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ProperteaseDbContext>();

                var currentTime = DateTime.UtcNow;
                var expiredBoosts = await dbContext.BoostedProperties
                    .Where(bp => bp.IsActive && bp.EndTime < currentTime)
                    .ToListAsync();

                if (expiredBoosts.Any())
                {
                    foreach (var boost in expiredBoosts)
                    {
                        boost.IsActive = false;
                    }

                    await dbContext.SaveChangesAsync();
                    _logger.LogInformation("Deactivated {count} expired boosted properties.", expiredBoosts.Count);
                }
            }
        }
    }
}