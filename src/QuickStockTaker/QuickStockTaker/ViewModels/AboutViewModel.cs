using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs;
using NLog;
using QuickStockTaker.Data;
using QuickStockTaker.Models;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace QuickStockTaker.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        #region Fields

        private readonly NLog.ILogger _logger;
        private IUserDialogs _dialogs;
        private List<DonationAmount> _donationAmounts;

        #endregion

        #region Properties

        public decimal Amount { get; set; }

        #endregion

        #region Properties

        public string VersionNo => AppInfo.VersionString;

        #endregion

        #region Commands

        public ICommand DonationCmd { get; set; }

        #endregion


        public AboutViewModel(IUserDialogs dialogs, ILogger logger)
        {
            _dialogs = dialogs;
            _logger = logger;
            
            _donationAmounts = new List<DonationAmount>()
            {
                new DonationAmount() { Amount = (decimal)0.99, AmountString = "$0.99" },
                new DonationAmount() { Amount = (decimal)2.99, AmountString = "$2.99" },
                new DonationAmount() { Amount = (decimal)4.99, AmountString = "$4.99" }
            };

            Title = "About";
            DonationCmd = new Command(OnDonationCmd);
        }


        private void OnDonationCmd()
        {
            var cfg = new ActionSheetConfig()
                .SetTitle("Select a amount you would like to donate")
                .SetMessage("Select a amount you would like to donate")
                .SetUseBottomSheet(false)
                .SetCancel();

            foreach (var donation in _donationAmounts)
            {
                cfg.Add(donation.AmountString, async () => await DoDonation(donation.Amount));
            }

            _dialogs.ActionSheet(cfg);
        }

        /// <summary>
        /// Take the donation amount
        /// </summary>
        /// <param name="amount">donation amount</param>
        /// <returns></returns>
        private async Task DoDonation(decimal amount)
        {
            new NotImplementedException();
        }
    }
}