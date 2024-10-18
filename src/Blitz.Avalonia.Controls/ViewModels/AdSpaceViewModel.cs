using System.Collections.ObjectModel;
using System.Linq;
using Material.Icons;

namespace Blitz.Avalonia.Controls.ViewModels;

public class AdSpaceViewModel(MainWindowViewModel mainWindowViewModel, MaterialIconKind icon, string textInfo, string linkUrl)
{
    public MainWindowViewModel MainWindowViewModel => mainWindowViewModel;
    public MaterialIconKind Icon => icon;
    
    public string TextInformation => textInfo;
    
    public string LinkUrl => linkUrl;
}

public class AdsCollection : ObservableCollection<AdSpaceViewModel>
{
    public AdSpaceViewModel FirstAdSpace => this.First();
    
    public AdsCollection(MainWindowViewModel mainWindowViewModel)
    {
        Add(new(mainWindowViewModel, MaterialIconKind.Youtube, "Setting Blitz Search Free",
            "https://youtube.com/shorts/bb9mbht9O8o?feature=share"));
    }
    
}
