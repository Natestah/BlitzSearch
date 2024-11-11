namespace Blitz.Goto;

public class ArgumentAlias(string alias, string description,ArgumentAlias.ArgumentAction action)
{
    public string Alias { get; } = alias;
    public string Description { get; } = description;
    public delegate string ArgumentAction(string inputArgument);
    public string ConvertArguments(string inputArgument) => action(inputArgument);
}