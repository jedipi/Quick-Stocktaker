using Microsoft.Extensions.Logging;
using QuickStockTaker.Core.Repositories;
using QuickStockTaker.Core.Repositories.Interfaces;

namespace QuickStockTaker;

public partial class MainPage : ContentPage
{
	int count = 0;
	IServiceProvider _provider;
    private readonly ILogger<MainPage> _logger;
    public MainPage(IServiceProvider provider, ILogger<MainPage> logger)
	{
		InitializeComponent();
        _provider = provider;
		_logger = logger;

    }

	private async void OnCounterClicked(object sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);

        _logger.LogInformation("Click was called");

        //var a = _provider.GetService<ISmtpService>();
        //var b = await a.GetSmtp("");


    }
}

