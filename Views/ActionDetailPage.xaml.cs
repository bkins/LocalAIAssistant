using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LocalAIAssistant.ViewModels;

namespace LocalAIAssistant.Views;

public partial class ActionDetailPage : ContentPage
{
    public ActionDetailPage(ActionDetailViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}