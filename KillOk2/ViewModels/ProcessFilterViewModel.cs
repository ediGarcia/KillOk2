using CommunityToolkit.Mvvm.ComponentModel;

namespace KillOk2.ViewModels;

public partial class ProcessFilterViewModel(string name) : ObservableObject
{
    #region Properties

    /// <summary>
    /// Gets or sets a value indicating whether to accept error dialogs.
    /// </summary>
    [ObservableProperty]
    private bool _acceptError = true;

    /// <summary>
    /// Gets or sets a value indicating whether to accept information dialogs.
    /// </summary>
    [ObservableProperty]
    private bool _acceptInfo = true;

    /// <summary>
    /// Gets or sets a value indicating whether to accept other dialogs.
    /// </summary>
    [ObservableProperty] 
    private bool _acceptOther;

    /// <summary>
    /// Gets or sets a value indicating whether to accept question dialogs.
    /// </summary>
    [ObservableProperty]
    private bool _acceptQuestion;

    /// <summary>
    /// Gets or sets a value indicating whether to accept warning dialogs.
    /// </summary>
    [ObservableProperty]
    private bool _acceptWarning = true;

    /// <summary>
    /// Gets or sets the value indicating whether the dialog closing process is currently running for this process filter.
    /// </summary>
    [ObservableProperty] 
    private bool _isSelected = true;

    /// <summary>
    /// Gets or sets the process name.
    /// </summary>
    [ObservableProperty] 
    private string _name = name;

    #endregion
}