namespace QuickStockTaker.Core.Services
{
    public sealed class StocktakeDeliveryOperationGate
    {
        private int _isOperationRunning;

        internal bool TryEnter() =>
            Interlocked.CompareExchange(ref _isOperationRunning, 1, 0) == 0;

        internal void Exit() =>
            Volatile.Write(ref _isOperationRunning, 0);
    }
}
