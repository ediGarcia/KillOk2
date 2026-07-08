using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperExtensions;
using KillOk2.Models;
using KillOk2.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace KillOk2.ViewModels;

public partial class MainViewModel : ObservableObject
{
    #region Properties

    /// <summary>
    /// Gets the collection of closed dialogs.
    /// </summary>
    public ObservableCollection<DialogInfo> ClosedDialogs { get; } = [];

    /// <summary>
    /// Gets the value indicating whether the dialog closing process is currently running.
    /// </summary>
    public bool IsRunning
    {
        get;
        private set
        {
            if (SetProperty(ref field, value))
                ToggleClosingDialogsCommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Gets or sets the name of a new process to add to the filters list.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddNewProcessCommand))]
    private string _newProcessName = String.Empty;

    /// <summary>
    /// Gets the collection of filters.
    /// </summary>
    public ObservableCollection<ProcessFilterViewModel> ProcessesFilters { get; } = [];

    #endregion

    private readonly ISystemService _systemService;
    private readonly DispatcherTimer _timer;

    private ProcessFilter[]? _filter;

    public MainViewModel(ISystemService systemService)
    {
        ClosedDialogs.CollectionChanged += (_, _) => ClearClosedDialogsCommand.NotifyCanExecuteChanged();

        ProcessesFilters.CollectionChanged += (_, args) =>
        {
            _filter = null;
            args.NewItems?.ForEach<ProcessFilterViewModel>(__ => __.PropertyChanged += ProcessInfo_OnPropertyChanged);
            args.OldItems?.ForEach<ProcessFilterViewModel>(__ => __.PropertyChanged -= ProcessInfo_OnPropertyChanged);
            NotifyToggleClosingDialogsCommand();
        };

        _systemService = systemService;

        _timer = new() { Interval = TimeSpan.FromMilliseconds(500) };
        _timer.Tick += Timer_OnTick;
    }

    #region Public Methods

    #region LoadProcesses
    /// <summary>
    /// Loads the filters from the persistent storage and adds them to the collection of filters, or adds a default filter if no filters were loaded.
    /// </summary>
    public void LoadProcesses()
    {
        try
        {
            if (_systemService.LoadPersistedFilters() is { } filters)
                ProcessesFilters.AddRange(filters.Select(_ => new ProcessFilterViewModel(_.ProcessName)
                {
                    AcceptError = _.AcceptError,
                    AcceptInfo = _.AcceptInfo,
                    AcceptOther = _.AcceptOther,
                    AcceptQuestion = _.AcceptQuestion,
                    AcceptWarning = _.AcceptWarning
                }));
            else
                ProcessesFilters.Add(new("npApp"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading data: {ex.Message}");
        }
    }
    #endregion

    #region SaveProcesses
    /// <summary>
    /// Stores the filters from the collection of filters to the persistent storage.
    /// </summary>
    public void SaveProcesses()
    {
        try
        {
            _systemService.PersistFilters(GetProcessFilters());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error saving data: {ex.Message}");
        }
    }
    #endregion

    #endregion

    #region Events

    #region AddNewProcess
    /// <summary>
    /// Adds a new process to the filters list.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddNewProcess))]
    private void AddNewProcess()
    {
        ProcessesFilters.Add(new(NewProcessName));
        NewProcessName = String.Empty;
    }
    #endregion

    #region CanAddNewProcess
    /// <summary>
    /// Indicates whether a new process can be added to the filters.
    /// </summary>
    /// <returns></returns>
    private bool CanAddNewProcess() =>
        !NewProcessName.IsNullOrWhiteSpace()
        && !NewProcessName.IsNumber()
        && !ProcessesFilters.Any(_ => _.Name.Equals(NewProcessName, StringComparison.OrdinalIgnoreCase));
    #endregion

    #region CanClearClosedDialogs
    /// <summary>
    /// Indicates whether the <see cref="ClearClosedDialogs"/> command can be executed.
    /// </summary>
    /// <returns></returns>
    private bool CanClearClosedDialogs() =>
        ClosedDialogs.Count > 0;
    #endregion

    #region CanToggleClosingDialogs
    /// <summary>
    /// Indicates whether the <see cref="ToggleClosingDialogs"/> command can be executed.
    /// </summary>
    /// <returns></returns>
    private bool CanToggleClosingDialogs() =>
        IsRunning
        || ProcessesFilters.Any(_ => _.AcceptInfo || _.AcceptQuestion || _.AcceptWarning || _.AcceptError);
    #endregion

    #region ClearClosedDialogs
    /// <summary>
    /// Clears the collection of closed dialogs.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanClearClosedDialogs))]
    private void ClearClosedDialogs() =>
        ClosedDialogs.Clear();
    #endregion

    #region CopyDialogMessage
    /// <summary>
    /// Copies the message of a dialog to the clipboard.
    /// </summary>
    /// <param name="dialogInfo"></param>
    [RelayCommand]
    private void CopyDialogMessage(DialogInfo dialogInfo) =>
        Clipboard.SetText(dialogInfo.Message);
    #endregion

    #region CopyDialogProcessId
    /// <summary>
    /// Copies the process ID of a dialog to the clipboard.
    /// </summary>
    /// <param name="dialogInfo"></param>
    [RelayCommand]
    private void CopyDialogProcessId(DialogInfo dialogInfo) =>
        Clipboard.SetText(dialogInfo.ProcessId.ToString());
    #endregion

    #region CopyDialogProcessName
    /// <summary>
    /// Copies the process name of a dialog to the clipboard.
    /// </summary>
    /// <param name="dialogInfo"></param>
    [RelayCommand]
    private void CopyDialogProcessName(DialogInfo dialogInfo) =>
        Clipboard.SetText(dialogInfo.ProcessName);
    #endregion

    #region CopyDialogTitle
    /// <summary>
    /// Copies the title of a dialog to the clipboard.
    /// </summary>
    /// <param name="dialogInfo"></param>
    [RelayCommand]
    private void CopyDialogTitle(DialogInfo dialogInfo) =>
        Clipboard.SetText(dialogInfo.Title);
    #endregion

    #region ProcessInfo_OnPropertyChanged
    /// <summary>
    /// Notifies that the ability to execute the <see cref="ToggleClosingDialogs"/> command may have changed when a property of a process filter changes.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ProcessInfo_OnPropertyChanged(object? sender, PropertyChangedEventArgs e) =>
        NotifyToggleClosingDialogsCommand();
    #endregion

    #region RemoveDialogInfo
    /// <summary>
    /// Removes the specified dialog from the collection of closed dialogs.
    /// </summary>
    /// <param name="dialogInfo"></param>
    [RelayCommand]
    private void RemoveDialogInfo(DialogInfo dialogInfo) =>
        ClosedDialogs.Remove(dialogInfo);
    #endregion

    #region RemoveProcess
    /// <summary>
    /// Removes a process from the filters list.
    /// </summary>
    /// <param name="processFilter"></param>
    [RelayCommand]
    private void RemoveProcess(ProcessFilterViewModel processFilter) =>
        ProcessesFilters.Remove(processFilter);
    #endregion

    #region Timer_OnTick
    /// <summary>
    /// Closes dialogs according to the specified filters and adds the information about the closed dialogs to the collection of closed dialogs every time the timer ticks.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Timer_OnTick(object? sender, EventArgs e) =>
        ClosedDialogs.AddRange(await Task.Run(() => _systemService.CloseDialogs(GetProcessFilters())));
    #endregion

    #region ToggleClosingDialogs
    /// <summary>
    /// Starts the process of closing dialogs if it is not currently running, or stops it if it is currently running.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanToggleClosingDialogs))]
    private void ToggleClosingDialogs()
    {
        IsRunning = !IsRunning;
        _timer.IsEnabled = IsRunning;
    }
    #endregion

    #endregion

    #region Private Methods

    #region GetProcessFilters
    /// <summary>
    /// Converts the collection of <see cref="ProcessFilterViewModel"/> to a collection of <see cref="ProcessFilter"/>.
    /// </summary>
    /// <returns></returns>
    private IEnumerable<ProcessFilter> GetProcessFilters() =>
        _filter ??= ProcessesFilters
            .Select(_ => new ProcessFilter(
                _.Name,
                _.AcceptInfo,
                _.AcceptQuestion,
                _.AcceptWarning,
                _.AcceptError,
                _.AcceptOther))
            .ToArray();
    #endregion

    #region NotifyToggleClosingDialogsCommand

    /// <summary>
    /// Updates the <see cref="ToggleClosingDialogsCommand"/> state.
    /// </summary>
    private void NotifyToggleClosingDialogsCommand()
    {
        if (IsRunning &&
            !ProcessesFilters.Any(__ => __.AcceptInfo || __.AcceptQuestion || __.AcceptWarning || __.AcceptError))
            ToggleClosingDialogs();

        ToggleClosingDialogsCommand.NotifyCanExecuteChanged();
    }

    #endregion

    #endregion
}