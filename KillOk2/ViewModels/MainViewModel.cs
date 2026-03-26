using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperExtensions;
using KillOk2.Services;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace KillOk2.ViewModels;

public partial class MainViewModel : ObservableObject
{
    #region Properties

    /// <summary>
    /// Gets the collection of closed dialogs info.
    /// </summary>
    public ObservableCollection<DialogInfoViewModel> ClosedDialogs { get; } = [];

    [ObservableProperty] 
    private bool _autoScroll = true;

    /// <summary>
    /// Gets whether the closing dialog process is running.
    /// </summary>
    public bool IsRunning => _timer?.IsEnabled == true;

    #endregion

    private bool _isBusy;
    private readonly DispatcherTimer _timer;

    public MainViewModel()
    {
        _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += Timer_Tick;
    }

    #region Events

    #region CanClear
    /// <summary>
    /// Determines whether the <see cref="ClearDialogsCommand"/> can be executed.
    /// </summary>
    /// <returns></returns>
    private bool CanClear() =>
        ClosedDialogs.Count > 0;
    #endregion

    #region ClearDialogs
    /// <summary>
    /// Clears the closed dialog list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanClear))]
    private void ClearDialogs()
    {
        ClosedDialogs.Clear();
        ClearDialogsCommand.NotifyCanExecuteChanged();
    }
    #endregion

    #region Timer_Tick
    /// <summary>
    /// Attempts to close an open dialog at each timer tick.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void Timer_Tick(object? sender, EventArgs e)
    {
        if (_isBusy)
            return;

        _isBusy = true;

        try
        {
            IReadOnlyCollection<DialogInfoViewModel> closedDialogs = await Task.Run(SystemService.CloseDialogs);

            if (closedDialogs.Count > 0)
            {
                bool needsNotification = ClosedDialogs.IsEmpty();
                ClosedDialogs.AddRange(closedDialogs);

                if (needsNotification)
                    ClearDialogsCommand.NotifyCanExecuteChanged();
            }
        }
        catch (Exception ex)
        {
            LoggerService.WriteDebugLine($"Error closing dialogs: {ex.Message}.");
        }

        _isBusy = false;
    }
    #endregion

    #region ToggleMonitoring
    /// <summary>
    /// Toggles the closing dialog monitoring on or off.
    /// </summary>
    [RelayCommand]
    private void ToggleMonitoring()
    {
        _timer.IsEnabled = !_timer.IsEnabled;
        OnPropertyChanged(nameof(IsRunning));
    }
    #endregion

    #endregion
}