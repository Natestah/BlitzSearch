namespace Blitz.Goto.Demo.ViewModels;

public class ArgumentAliasViewModel(ArgumentAlias argumentAlias) : ViewModelBase
{
    
    public string Alias => argumentAlias.Alias;
    public string Description => argumentAlias.Description;

}