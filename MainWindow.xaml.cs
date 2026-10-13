using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EverythingModern.ViewModels;
using Wpf.Ui.Controls;

namespace EverythingModern
{
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            PreviewKeyDown += MainWindow_PreviewKeyDown;
        }

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            // Global Ctrl+F to focus search box
            if (e.Key == Key.F && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                SearchBox.Focus();
                SearchBox.SelectAll();
                e.Handled = true;
                return;
            }

            // Alt+C for Match Case
            if (e.Key == Key.C && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                vm.ToggleMatchCaseCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Alt+W for Match Whole Word
            if (e.Key == Key.W && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                vm.ToggleMatchWholeWordCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Alt+P for Match Path
            if (e.Key == Key.P && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                vm.ToggleMatchPathCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Alt+R for Regex
            if (e.Key == Key.R && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                vm.ToggleRegexCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Alt+A for Analytics
            if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt)
            {
                vm.ToggleAnalyticsCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // F1 for Syntax Helper Cheatsheet
            if (e.Key == Key.F1)
            {
                vm.ToggleSyntaxHelperCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Global Escape key
            if (e.Key == Key.Escape)
            {
                if (vm.IsQuickLookOpen)
                {
                    vm.IsQuickLookOpen = false;
                    e.Handled = true;
                    return;
                }
                if (vm.IsSyntaxHelperOpen)
                {
                    vm.IsSyntaxHelperOpen = false;
                    e.Handled = true;
                    return;
                }
                if (!string.IsNullOrEmpty(vm.SearchText))
                {
                    vm.SearchText = string.Empty;
                    e.Handled = true;
                    return;
                }
            }

            // F5 to refresh
            if (e.Key == Key.F5)
            {
                vm.TriggerSearch();
                e.Handled = true;
                return;
            }
        }

        private void ListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.SelectedItem != null)
            {
                vm.OpenFileCommand.Execute(vm.SelectedItem);
            }
        }

        private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is MainViewModel vm && sender is System.Windows.Controls.ListBox lb)
            {
                vm.UpdateSelection(lb.SelectedItems);
            }
        }

        private void ListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataContext is not MainViewModel vm) return;

            if (e.Key == Key.Enter)
            {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) || Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
                {
                    vm.OpenContainingFolderCommand.Execute(vm.SelectedItem);
                }
                else
                {
                    vm.OpenFileCommand.Execute(vm.SelectedItem);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Space)
            {
                vm.ToggleQuickLookCommand.Execute(null);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                var selected = (sender as System.Windows.Controls.ListBox)?.SelectedItems;
                vm.DeleteSelectedFilesCommand.Execute(selected);
                e.Handled = true;
            }
            else if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                var selected = (sender as System.Windows.Controls.ListBox)?.SelectedItems;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    vm.CopySelectedQuotedPathsCommand.Execute(selected);
                }
                else
                {
                    vm.CopySelectedFilesCommand.Execute(selected);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.P && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                vm.TogglePreviewPaneCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void ColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not GridViewColumnHeader header || DataContext is not MainViewModel vm) return;
            string? tag = header.Tag as string;
            if (!string.IsNullOrEmpty(tag))
            {
                vm.SortByCommand.Execute(tag);
            }
        }

        private void QuickLookOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender && DataContext is MainViewModel vm)
            {
                vm.IsQuickLookOpen = false;
            }
        }
    }
}