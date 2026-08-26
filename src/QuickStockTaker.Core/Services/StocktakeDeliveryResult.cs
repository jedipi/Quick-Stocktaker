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
        StocktakeExport Export = null,
        string Message = null)
    {
        public static StocktakeDeliveryResult Succeeded(StocktakeExport export) =>
            new(StocktakeDeliveryStatus.Succeeded, export);

        public static StocktakeDeliveryResult Succeeded(StocktakeExport export, string message) =>
            new(StocktakeDeliveryStatus.Succeeded, export, message);

        public static StocktakeDeliveryResult NoStocktakeData() =>
            new(StocktakeDeliveryStatus.NoStocktakeData);

        public static StocktakeDeliveryResult InvalidConfiguration(string message) =>
            new(StocktakeDeliveryStatus.InvalidConfiguration, Message: message);

        public static StocktakeDeliveryResult Cancelled() =>
            new(StocktakeDeliveryStatus.Cancelled);

        public static StocktakeDeliveryResult AlreadyInProgress() =>
            new(StocktakeDeliveryStatus.AlreadyInProgress);

        public static StocktakeDeliveryResult Failed() =>
            new(StocktakeDeliveryStatus.Failed);

        public static StocktakeDeliveryResult Failed(string message) =>
            new(StocktakeDeliveryStatus.Failed, Message: message);
    }
}
