using Blitz.Goto;

namespace Blitz.Avalonia.Controls.ViewModels;
public class ArgumentAliasViewModel(ArgumentAlias argumentAlias) : ViewModelBase
{
    
    public string Alias => argumentAlias.Alias;
    public string Description => argumentAlias.Description;

}