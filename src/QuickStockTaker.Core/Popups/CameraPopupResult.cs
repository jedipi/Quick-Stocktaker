using ZXing.Net.Maui;

namespace QuickStockTaker.Core.Popups;

public sealed record CameraPopupResult(BarcodeResult[] Barcodes);
