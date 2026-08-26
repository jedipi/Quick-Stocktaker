namespace QuickStockTaker.Core.Services
{
    public sealed class StocktakeEmailConfigurationGate : Interfaces.IStocktakeEmailConfigurationGate
    {
        private readonly SemaphoreSlim _gate = new(1, 1);

        public Task RunAsync(Func<Task> action) =>
            RunAsync(async () =>
            {
                await action();
                return true;
            });

        public async Task<T> RunAsync<T>(Func<Task<T>> action)
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
