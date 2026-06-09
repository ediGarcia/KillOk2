using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using KillOk2.ViewModels;
using Wpf.Ui.Appearance;

namespace KillOk2.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        SystemThemeWatcher.Watch(this);
        InitializeComponent();
    }

    #region Events

    #region MainWindow_OnClosing
    /// <summary>
    /// Saves the processes to a file when the application is closing.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MainWindow_OnClosing(object? sender, CancelEventArgs e) =>
        (DataContext as MainViewModel)!.SaveProcesses();
    #endregion

    #region MainWindow_OnLoaded
    /// <summary>
    /// Loads the processes from a file when the application is loaded.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) =>
        (DataContext as MainViewModel)!.LoadProcesses();
    #endregion

    #region ScrollViewer_OnScrollChanged
    /// <summary>
    /// Scrolls to the bottom of the ScrollViewer new items are added.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentHeightChange > 0)
            (sender as ScrollViewer)!.ScrollToBottom();
    }
    #endregion

    #endregion
}