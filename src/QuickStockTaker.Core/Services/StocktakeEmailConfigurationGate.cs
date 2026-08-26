namespace QuickStockTaker.Core.Services
{
    public sealed class StocktakeEmailConfigurationGate
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        internal async Task RunAsync(Func<Task> action)
        {
            await _gate.WaitAsync();
            try
            {
                await action();
            }
            finally
            {
                _gate.Release();
            }
        }

        internal async Task<T> RunAsync<T>(Func<Task<T>> action)
        {
            await _gate.WaitAsync();
            try
            {
                return await action();
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
