using QuickStockTaker.Core.Repositories;
using QuickStockTaker.Core.Repositories.Interfaces;

namespace QuickStockTaker;

public partial class MainPage : ContentPage
{
	int count = 0;
	IServiceProvider _provider;

    public MainPage(IServiceProvider provider)
	{
		InitializeComponent();
        _provider = provider;

    }

	private async void OnCounterClicked(object sender, EventArgs e)
	{
		count++;

		if (count == 1)
			CounterBtn.Text = $"Clicked {count} time";
		else
			CounterBtn.Text = $"Clicked {count} times";

		SemanticScreenReader.Announce(CounterBtn.Text);

		//var a = _provider.GetService<ISmtpService>();
		//var b = await a.GetSmtp("");


    }
}

