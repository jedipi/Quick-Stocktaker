using QuickStockTaker.Core.Services.Interfaces;

namespace QuickStockTaker.Core.Services
{
    public sealed class StocktakeRemoteConfigurationGate : IStocktakeRemoteConfigurationGate
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        public async Task<T> RunAsync<T>(
            Func<Task<T>> action,
            CancellationToken cancellationToken = default)
        {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await action().ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        public Task RunAsync(
            Func<Task> action,
            CancellationToken cancellationToken = default) =>
            RunAsync(async () =>
            {
                await action().ConfigureAwait(false);
                return true;
            }, cancellationToken);
    }
}
