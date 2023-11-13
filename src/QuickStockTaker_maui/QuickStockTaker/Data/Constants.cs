using System;
using System.Collections.Generic;
using System.Text;

namespace QuickStockTaker.Data
{
    public static class Constants
    {
        public static readonly string StocktakeNumber = "StocktakeNumber";
        public static readonly string Site = "Site";
        public static readonly string StocktakeDate = "StocktakeDate";
        public static readonly string DeviceId = "DeviceId";
        public static readonly string SmtpProvider = "SmtpProvider";
        public static readonly string SmtpHost = "SmtpHost";
        public static readonly string SmtpPort = "SmtpPort";
        public static readonly string SmtpUsername = "SmtpUsername";
        public static readonly string SmtpPassword = "SmtpPassword";
        public static readonly string SmtpFrom = "SmtpFrom";

        public static readonly List<string> SmtpHostProviders = new List<string>(){"Gmail", "Other"};




    }
}
