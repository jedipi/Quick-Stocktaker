namespace QuickStockTaker.Core.Services
{
    public static class NavigationRoutes
    {
        public const string HomeTabPage = "HomeTabPage";
        public const string DashboardPage = "DashboardPage";
        public const string SettingsPage = "SettingsPage";
        public const string EmailSettingPage = "EmailSettingPage";
        public const string FtpSetingPage = "FtpSetingPage";
        public const string AboutPage = "AboutPage";
        public const string NewStocktakePage = "NewStocktakePage";
        public const string EnterDatePage = "EnterDatePage";
        public const string BayListPage = "BayListPage";
        public const string BayDetailsPage = "BayDetailsPage";
        public const string ItemDetailPage = "ItemDetailPage";
        public const string ReviewPage = "ReviewPage";
        public const string DataUploadPage = "DataUploadPage";

        public const string Back = "..";

        public static string DashboardRoot => $"//{DashboardPage}";

        public static string BayDetailsWithSelectedBay(string encodedSelectedBay)
        {
            return $"{BayDetailsPage}?SelectedBayContent={encodedSelectedBay}";
        }

        public static string BackWithSelectedItem(string encodedSelectedItem)
        {
            return $"{Back}?SelectedItemContent={encodedSelectedItem}";
        }
    }
}
