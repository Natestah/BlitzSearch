using System;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Blitz.Avalonia.Controls.Views;

public partial class HelpPanel : UserControl
{
    public HelpPanel()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        ShowHelpMd();
    }

    private void ShowHelpMd()
    {
        HelpBox.SelectedItem = null;
        if (!Configuration.Instance.IsWelcomed)
        {
            HelpBox.SelectedItem = HelpBox.Items[0]; // first is welcome
            Configuration.Instance.IsWelcomed = true;
        }
        else
        {
            foreach (var item in HelpBox.Items.OfType<ListBoxItem>())
            {
                if (item.Content is not "Change Log") continue;
                HelpBox.SelectedItem = item;
                break;
            }
        }
    }

    private void HelpBoxItemChanged(object? sender, SelectionChangedEventArgs e)
    {
        if ((e.AddedItems[0] as ListBoxItem)?.Content is not string text) return;
        text = text.Replace(" ", "_");
        var uri = new Uri(Path.GetFullPath($"Documentation/{text}.md"));

        if (!File.Exists(uri.LocalPath))
        {
            // Deployed and Working dir didn't resolve.. 
            uri = new Uri(Environment.ExpandEnvironmentVariables($"%programfiles%\\blitz\\Documentation\\{text}.md"));
        }
        
        if (Path.Exists(uri.LocalPath))
        {
            MarkdownScrollViewer.AssetPathRoot = Path.GetDirectoryName(uri.LocalPath);
            MarkdownScrollViewer.Source = uri;
        }
        else
        {
            MarkdownScrollViewer.Markdown = uri.LocalPath;
        }
            
    }
}