using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Blitz.Avalonia.Controls.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using ReactiveUI;
using System.Reactive;
namespace Blitz.Avalonia.Controls.Views;

public partial class ResultsBox : UserControl
{
    public ReactiveCommand<int,Unit> GotoOtherEditor { get; set; }

    public ResultsBox()
    {
        InitializeComponent();
        GotoOtherEditor = ReactiveCommand.Create<int>(GotoOtherEditorRun);
        KeyDown+=OnKeyDown;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        if (e.Key == Key.Enter)
        {
            var selected = mainWindowViewModel.SelectedItems.FirstOrDefault();
            if (mainWindowViewModel.SelectedEditorViewModel != null && !mainWindowViewModel.SelectedEditorViewModel.RunTotoOnObjectGoto(selected,
                    out string errorMessage))
            {
                mainWindowViewModel.ShowImportantMessage?.Invoke(errorMessage);
            }
        }
    }

    private void ResultsView_OnSelectionChanged(object? _, SelectionChangedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        if (e.AddedItems.Count <= 0)
        {
            return;
        }
        
        mainWindowViewModel.ShowPreview?.Invoke(e.AddedItems[0]!);
    }
    
    private  void GotoOtherEditorRun(int parameter)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        var editorVm = mainWindowViewModel.GotoEditorCollection[parameter];
        var selectedFirst = mainWindowViewModel.SelectedItems.FirstOrDefault();
        if (! editorVm.RunTotoOnObjectGoto(selectedFirst, out var errorMessage))
        {
             ShowImportantMessage(errorMessage);
        }
    }

    private void ShowImportantMessage(string message)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        mainWindowViewModel.EnableTextPane = true;
        mainWindowViewModel.ShowPreview?.Invoke(message);
    }

    [SuppressMessage("ReSharper", "UnusedParameter.Local")]
    private void RightClickMenu_OnOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu contextMenu) return;
        
        string startMenuName = "Dynamic_Starter";
        int starterIndex = -1;
        for (int i = 0; i < contextMenu.Items.Count; i++)
        {
            if (contextMenu.Items[i] is Separator separator && separator.Name == startMenuName)
            {
                contextMenu.Items.RemoveAt(i);
                starterIndex = i;
                while (contextMenu.Items.Count > starterIndex && contextMenu.Items[starterIndex] is not Separator)
                {
                    contextMenu.Items.RemoveAt(i);
                }
            }
        }

        if (starterIndex == -1)
        {
            starterIndex = contextMenu.Items.Count;
        }

        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        
        var newSeparator =  new Separator() { Name = "Dynamic_Starter" };
        contextMenu.Items.Insert(starterIndex,newSeparator);
        for (int i = 0; i < mainWindowViewModel.GotoEditorCollection.Count; i++)
        {
            var editorVm = mainWindowViewModel.GotoEditorCollection[i];
            if (!editorVm.EditorExists())
            {
                continue;
            }
            var multiBinding = this.FindResource("ImageConverterBinding") as MultiBinding;
            var titleBinding = this.FindResource("TitleBinding") as IBinding;
            var menuItem = new MenuItem
            {
                DataContext = editorVm,
                Command = GotoOtherEditor,
                CommandParameter = i,
                Icon = new Image(){ [!Image.SourceProperty] = multiBinding!},
                [!HeaderedSelectingItemsControl.HeaderProperty] = titleBinding!,
            };
            contextMenu.Items.Add(menuItem);
        }
    }
    private void ResultsListBox_OnDoubleTapped(object? _, TappedEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }
        
        if (mainWindowViewModel.SelectedEditorViewModel == null)
        {
            return;
        }

        if (e.Source is Control control)
        {
            if (!mainWindowViewModel.SelectedEditorViewModel.RunTotoOnObjectGoto(control.DataContext,
                    out string errorMessage))
            {
                mainWindowViewModel.ShowImportantMessage?.Invoke(errorMessage);
            }
        }
    }
}