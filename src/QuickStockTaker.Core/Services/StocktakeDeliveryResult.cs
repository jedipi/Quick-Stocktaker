namespace QuickStockTaker.Core.Services
{
    public enum StocktakeDeliveryStatus
    {
        Succeeded,
        NoStocktakeData,
        InvalidConfiguration,
        Cancelled,
        AlreadyInProgress,
        Failed
    }

    public sealed record StocktakeDeliveryResult(
        StocktakeDeliveryStatus Status,
        StocktakeExport Export = null)
    {
        public static StocktakeDeliveryResult Succeeded(StocktakeExport export) =>
            new(StocktakeDeliveryStatus.Succeeded, export);

        public static StocktakeDeliveryResult NoStocktakeData() =>
            new(StocktakeDeliveryStatus.NoStocktakeData);

        public static StocktakeDeliveryResult Cancelled() =>
            new(StocktakeDeliveryStatus.Cancelled);

        public static StocktakeDeliveryResult AlreadyInProgress() =>
            new(StocktakeDeliveryStatus.AlreadyInProgress);

        public static StocktakeDeliveryResult Failed() =>
            new(StocktakeDeliveryStatus.Failed);
    }
}
