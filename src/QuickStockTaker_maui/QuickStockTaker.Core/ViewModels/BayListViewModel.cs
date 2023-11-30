using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Controls.UserDialogs.Maui;
using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Models;
using QuickStockTaker.Core.Models.Sqlite;
using QuickStockTaker.Core.Repositories.Interfaces;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuickStockTaker.Core.ViewModels
{
    public partial class BayListViewModel : BaseViewModel
    {
        #region fields
        private IServiceProvider _serviceProvider;
        private ISQLiteRepository<StocktakeItem> _repo;
        private ObservableCollection<Bay> _unfilteredItems;
        #endregion

        #region Properties
        [ObservableProperty]
        private ObservableCollection<Bay> _bays;

        [ObservableProperty]
        private string _searchText;

        [ObservableProperty]
        private int _totalQty;

        [ObservableProperty]
        private int _totalBayCount;

        [ObservableProperty]
        private Bay _selectedBay;

        #endregion
        public BayListViewModel(
            IUserDialogs dialogs,
            ILogger<BayListViewModel> logger,
            ISQLiteRepository<StocktakeItem> repo) : base(dialogs, logger)
        {
            _repo = repo;
        }

        #region RelayCommands
        [RelayCommand]
        private void OnSearchText()
        {
            var searchPhrase = SearchText?.Trim();
            if (string.IsNullOrEmpty(searchPhrase))
            {
                Bays = _unfilteredItems;
            }
            else
            {
                Bays = new ObservableCollection<Bay>(_unfilteredItems.Where(x => x.BayLocation.ToLower().Contains(searchPhrase.ToLower())));
            }
        }

        [RelayCommand]
        private async Task OnAppearing()
        {
            await GetBays();
        }

        #endregion
        public async Task GetBays()
        {
            _dialogs.ShowLoading("Loading data...");
            Bays = new ObservableCollection<Bay>();

            var items = await _repo.GetAllAsync();
            
            var bays = items.GroupBy(x => x.BayLocation).Select(l => new Bay
            {
                BayLocation = l.Key,
                TotalCount = l.Sum(y => y.Qty)
            }).OrderBy(x => x.BayLocation).ToList();

            Bays = _unfilteredItems = new ObservableCollection<Bay>(bays);

            GetCount();

            _dialogs.HideHud();

            if (_unfilteredItems.Count == 0)
            {
                await _dialogs.AlertAsync("There is no stocktake data.", "No Data", "OK");
            }
        }

        /// <summary>
        /// get total number of items and total number of bays
        /// </summary>
        public void GetCount()
        {
            if (_unfilteredItems == null) return;
            TotalQty = (int)_unfilteredItems.Sum(x => x.TotalCount);
            TotalBayCount = _unfilteredItems.Count;
        }
    }
}
