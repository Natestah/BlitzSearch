using System;
using System.Collections;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Blitz.Avalonia.Controls.ViewModels;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using ReactiveUI;
using System.Reactive;
using System.Threading.Tasks;
using Blitz.Interfacing;

namespace Blitz.Avalonia.Controls.Views;

public partial class ResultsBox : UserControl
{
    public ReactiveCommand<int,Unit> GotoOtherEditor { get; set; }

    public ResultsBox()
    {
        InitializeComponent();
        GotoOtherEditor = ReactiveCommand.Create<int>(GotoOtherEditorRun);
        KeyDown+=OnKeyDown;
        AddHandler(PointerWheelChangedEvent, Handler, RoutingStrategies.Tunnel);
        AddHandler(KeyDownEvent, KeyDownHandler, RoutingStrategies.Tunnel);
    }

    private void KeyDownHandler(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Right)
        {
            e.Handled = true;
            SkipNextResultCommandRun();
        }

        if (e.Key is Key.Left)
        {
            e.Handled = true;
            SkipPrevResultCommandRun();
        }
    }
    

    private void SkipNextResultCommandRun()
    {
        if (ResultsListBox.Items.Count == 0 )
        {
            return;
        }
        
        if (ResultsListBox.SelectedItems.Count == 0)
        {
            ResultsListBox.SelectedItems.Clear();
            ResultsListBox.SelectedItems.Add(ResultsListBox.Items.First());
            return;
        }

        if (ResultsListBox.ItemContainerGenerator == null)
        {
            return;
        }
        if (ResultsListBox.SelectedItems.Cast<object>().LastOrDefault() is not { } lastorDefault)
        {
            return;
        }
        ResultsListBox.ScrollIntoView(lastorDefault);

        var thisType = lastorDefault.GetType();
        var index = ResultsListBox.Items.IndexOf(lastorDefault);
        if (ResultsListBox.Scroll is not ScrollViewer scroll)
        {
            return;
        }
        
        while (index < ResultsListBox.Items.Count - 1)
        {
            index++;
            var nextItem = ResultsListBox.Items[index];
            while (ResultsListBox.ContainerFromIndex(index) == null)
            {
                scroll.LineDown();
                UpdateLayout();
            }


            if (ResultsListBox.ContainerFromIndex(index) is not ListBoxItem selectedItem)
            {
                continue;
            }
            selectedItem.IsSelected = true;
            selectedItem.Focus();
            ResultsListBox.SelectedIndex = index;
            if (thisType == nextItem.GetType())
            {
                continue;
            }
            return;
        }
    }
    
    
    private void SkipPrevResultCommandRun()
    {
        if (ResultsListBox.Items.Count == 0 )
        {
            return;
        }
        
        if (ResultsListBox.SelectedItems is { Count: 0 })
        {
            //no wrapping here.
            return;
        }

        if (ResultsListBox.SelectedItems?[0] is not { } firstOrDefault)
        {
            return;
        }
        if (ResultsListBox.Scroll is not ScrollViewer scroll)
        {
            return;
        }

        ResultsListBox.ScrollIntoView(firstOrDefault);
        var thisType = firstOrDefault.GetType();
        var index = ResultsListBox.Items.IndexOf(firstOrDefault);
        while (index > 0)
        {
            index--;
            var nextItem = ResultsListBox.Items[index];
            
            while (ResultsListBox.ContainerFromIndex(index) == null)
            {
                scroll.LineUp();
                UpdateLayout();
            }

            if (ResultsListBox.ContainerFromIndex(index) is not ListBoxItem selectedItem)
            {
                return;
            }
            selectedItem.Focus();
            ResultsListBox.ScrollIntoView(selectedItem);
            if (thisType == nextItem?.GetType())
            {
                continue;
            }
            selectedItem.IsSelected = true;
            selectedItem.Focus();
            ResultsListBox.SelectedIndex = index;
            return;

        }
    }

    private void Handler(object? sender, PointerWheelEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel
            || !e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            return;
        }
        
        mainWindowViewModel.FontSize += e.Delta.Y > 0 ? 0.8 : -0.8;
        e.Handled = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        if (e.Key == Key.Enter)
        {
            var selected = mainWindowViewModel.SelectedItems.FirstOrDefault();
            if (mainWindowViewModel.SelectedEditorViewModel != null && !mainWindowViewModel.SelectedEditorViewModel.RunTotoOnObjectGoto(selected,false,
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
        
        var addedFirst = e.AddedItems[0];
        if (e.Source is Control && addedFirst != null )
        {
            mainWindowViewModel.UpdatePreviewForItem(addedFirst);
        }
    }
    
    private  void GotoOtherEditorRun(int parameter)
    {
        if (DataContext is not MainWindowViewModel mainWindowViewModel) return;
        var editorVm = mainWindowViewModel.GotoEditorCollection[parameter];
        var selectedFirst = mainWindowViewModel.SelectedItems.FirstOrDefault();
        if (! editorVm.RunTotoOnObjectGoto(selectedFirst, false, out var errorMessage))
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
            if (!mainWindowViewModel.SelectedEditorViewModel.RunTotoOnObjectGoto(control.DataContext,false,
                    out string errorMessage))
            {
                mainWindowViewModel.ShowImportantMessage?.Invoke(errorMessage);
            }
        }
    }

    private async void FixReplacement_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ReplaceTextViewModel replaceTextViewModel })
        {
            return;
        }
        if (DataContext is not MainWindowViewModel mainWindowViewModel)
        {
            return;
        }

        foreach (var replaceFileNameResultFailure in replaceTextViewModel.ReplaceFileNameResultFailures.ToList())
        {
            try
            {
                File.SetAttributes(replaceFileNameResultFailure.FilenameResult.FileName, ~FileAttributes.ReadOnly & File.GetAttributes(replaceFileNameResultFailure.FilenameResult.FileName));
                await mainWindowViewModel.ApplyReplacement(replaceFileNameResultFailure.FilenameResult);
            }
            catch (Exception ex)
            {
                mainWindowViewModel.ResultBoxItems.Add(new ExceptionViewModel( ExceptionResult.CreateFromException(ex)));
                continue;
            }
            mainWindowViewModel.ResultBoxItems.Remove(replaceFileNameResultFailure.ExceptionViewModel);
            replaceTextViewModel.ReplaceFileNameResultFailures.Remove(replaceFileNameResultFailure);
            replaceTextViewModel.Count = replaceTextViewModel.Count + 1;
        }
        
    }
}