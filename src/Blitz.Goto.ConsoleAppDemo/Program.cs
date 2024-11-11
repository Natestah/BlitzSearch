using Blitz.Goto;

// Blitz.Goto comes with a predefined set of popular editors so you don't have to wrangle the command line
var definitions = new GotoDefinitions();
foreach (var editor in  new GotoDefinitions().GetBuiltInEditors())
{
    var executablePath = Path.Combine(editor.ExecutableWorkingDirectory, editor.Executable);
    var line =  $"{editor.Title}: {executablePath} {editor.Arguments}";
    Console.WriteLine(line);
}

// Grab the first from the list for example purpose. ( GetBuiltInEditors can be used to populate a selector )
var firstEditor = definitions.GetBuiltInEditors().FirstOrDefault(def => def.Title == "Notepad++")!;

//Create a Goto Action from the Editor. Goto Action takes care of translating the tags and environment variables.
var gotoAction = new GotoAction(firstEditor);

// Define what you want to go to.  CreateAndTestFile will create a temp file path that you can test Goto Functionality with.
new GotoTestFile().CreateAndTestFile(out var myFileName,out var lineNumber, out var columnNumber );

// Create a directive for the goto action.
var directive = new GotoDirective(myFileName, lineNumber, columnNumber);

//execute goto 
gotoAction.ExecuteGoto(directive);