﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using QuickStockTaker.ViewModels;
using QuickStockTaker.ViewModels.Base;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace QuickStockTaker.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class EnterDatePage : ContentPage
    {
        public EnterDatePage()
        {
            InitializeComponent();
            BindingContext = ViewModelLocator.Container.Resolve<EnterDateViewModel>();
        }
    }
}