using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using EverythingModern.Models;
using EverythingModern.Services;
using EverythingModern.ViewModels;
using Wpf.Ui.Controls;

namespace EverythingModern
{
    public partial class MainWindow : FluentWindow
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint RegisterWindowMessage(string lpString);

        public static readonly uint WM_EVERYVIEW_SUMMON = RegisterWindowMessage("EVERYVIEW_SUMMON_INSTANCE_MSG");

        private GlobalHotkeyManager? _hotkeyManager;
        private TrayIconManager? _trayIconManager;
        private Point _dragStartPoint;
        private bool _isDragging;

        public MainWindow()
        {
            InitializeComponent();
            PreviewKeyDown += MainWindow_PreviewKeyDown;
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            StateChanged += MainWindow_StateChanged;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            try
            {
                var helper = new WindowInteropHelper(this);
                var source = HwndSource.FromHwnd(helper.Handle);
                source?.AddHook(WndProc);
            }
            catch { }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_EVERYVIEW_SUMMON)
            {
                SummonAndFocus();
                handled = true;
                return (IntPtr)1;
            }
            return IntPtr.Zero;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _hotkeyManager = new GlobalHotkeyManager(this, SummonAndFocus);
                _trayIconManager = new TrayIconManager(this, SummonAndFocus);
            }
            catch { }
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized && UserSettingsService.Current.MinimizeToTray)
            {
                Hide();
            }
        }

        public void SummonAndFocus()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(SummonAndFocus);
                return;
            }

            Show();
            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }
            Activate();
            Focus();
            SearchBox.Focus();
            SearchBox.SelectAll();
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            UserSettingsService.SaveNow();
            _hotkeyManager?.Dispose();
            _trayIconManager?.Dispose();
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

            // Ctrl+H for Search History
            if (e.Key == Key.H && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                vm.ToggleHistoryCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Ctrl+B for Bookmarks
            if (e.Key == Key.B && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                vm.ToggleBookmarksCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // Ctrl+D to bookmark current search
            if (e.Key == Key.D && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                vm.OpenAddBookmarkDialogCommand.Execute(null);
                e.Handled = true;
                return;
            }

            // F2 for Rename
            if (e.Key == Key.F2 && vm.SelectedItem != null)
            {
                vm.StartRenameCommand.Execute(vm.SelectedItem);
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
                if (vm.IsRenameDialogOpen)
                {
                    vm.CancelRenameCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
                if (vm.IsAddBookmarkDialogOpen)
                {
                    vm.CancelAddBookmarkCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
                if (vm.IsHistoryOpen)
                {
                    vm.IsHistoryOpen = false;
                    e.Handled = true;
                    return;
                }
                if (vm.IsBookmarksOpen)
                {
                    vm.IsBookmarksOpen = false;
                    e.Handled = true;
                    return;
                }
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

            if (e.Key == Key.Up && (sender as System.Windows.Controls.ListBox)?.SelectedIndex <= 0)
            {
                SearchBox.Focus();
                SearchBox.CaretIndex = SearchBox.Text.Length;
                e.Handled = true;
                return;
            }

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
            else if (e.Key == Key.F2)
            {
                vm.StartRenameCommand.Execute(vm.SelectedItem);
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
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
                {
                    vm.DeletePermanentlyCommand.Execute(selected);
                }
                else
                {
                    vm.DeleteSelectedFilesCommand.Execute(selected);
                }
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

        private void SearchBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                var targetList = (DataContext as MainViewModel)?.IsGridView == true
                    ? (System.Windows.Controls.ListBox)CardListBox
                    : ResultsListView;

                if (targetList != null && targetList.Items.Count > 0)
                {
                    targetList.Focus();
                    if (targetList.SelectedIndex < 0)
                    {
                        targetList.SelectedIndex = 0;
                    }
                    var item = targetList.ItemContainerGenerator.ContainerFromIndex(targetList.SelectedIndex) as ListBoxItem;
                    item?.Focus();
                    e.Handled = true;
                }
            }
        }

        private void FilterScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scroller)
            {
                if (e.Delta < 0)
                {
                    scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset + 60);
                }
                else
                {
                    scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset - 60);
                }
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

        private void RenameOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender && DataContext is MainViewModel vm)
            {
                vm.IsRenameDialogOpen = false;
            }
        }

        private void AddBookmarkOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == sender && DataContext is MainViewModel vm)
            {
                vm.IsAddBookmarkDialogOpen = false;
            }
        }

        // Native Drag-and-Drop file export support for Windows Explorer, Desktop, Discord, etc.
        private void ListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
        }

        private void ListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                Point current = e.GetPosition(this);
                Vector diff = _dragStartPoint - current;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (sender is System.Windows.Controls.ListBox lb && lb.SelectedItems.Count > 0)
                    {
                        var paths = new List<string>();
                        foreach (var obj in lb.SelectedItems)
                        {
                            if (obj is SearchResultItem itm && !string.IsNullOrWhiteSpace(itm.FullPath))
                            {
                                paths.Add(itm.FullPath);
                            }
                        }

                        if (paths.Count > 0)
                        {
                            _isDragging = true;
                            try
                            {
                                var dataObject = new DataObject();
                                var sc = new System.Collections.Specialized.StringCollection();
                                foreach (var p in paths)
                                {
                                    if (File.Exists(p) || Directory.Exists(p)) sc.Add(p);
                                }

                                if (sc.Count > 0)
                                {
                                    dataObject.SetFileDropList(sc);
                                    string text = string.Join(Environment.NewLine, paths);
                                    dataObject.SetData(DataFormats.Text, text);
                                    dataObject.SetData(DataFormats.UnicodeText, text);

                                    DragDrop.DoDragDrop(lb, dataObject, DragDropEffects.Copy | DragDropEffects.Link);
                                }
                            }
                            catch { }
                            finally
                            {
                                _isDragging = false;
                            }
                        }
                    }
                }
            }
        }
    }
}