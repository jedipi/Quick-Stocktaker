using CommunityToolkit.Mvvm.ComponentModel;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        protected IUserDialogs _dialogs;
        protected ILogger _logger;
        public BaseViewModel(IUserDialogs dialogs, ILogger logger)
        {
            _dialogs = dialogs;
            _logger = logger;
        }
    }
}
