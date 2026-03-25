using KillOk2.ViewModels;
using System.Collections.Specialized;
using System.Windows;

namespace KillOk2.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel = new();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = _viewModel;
        _viewModel.ClosedDialogs.CollectionChanged += ClosedDialogs_CollectionChanged;
    }

    #region Events

    #region ClosedDialogs_CollectionChanged
    /// <summary>
    /// Scrolls to the last dialog if the auto-scroll is enabled.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ClosedDialogs_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_viewModel.AutoScroll)
            ScvClosedDialogs.ScrollToBottom();
    }
    #endregion

    #endregion
}