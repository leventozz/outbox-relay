namespace OutboxRelay.ConsumerWorkerService
{
    public class ConsumerWorkerService : BackgroundService
    {
        private readonly ILogger<ConsumerWorkerService> _logger;

        public ConsumerWorkerService(ILogger<ConsumerWorkerService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
