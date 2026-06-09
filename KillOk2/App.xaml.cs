using KillOk2.Services;
using KillOk2.ViewModels;
using KillOk2.Views;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace KillOk2;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    private ServiceProvider _serviceProvider = null!;

    #region Events

    #region OnExit
    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
    #endregion

    #region OnStartup
    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ServiceCollection services = new();
        services.AddSingleton<ISystemService, SystemService>();
        services.AddTransient<MainViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        new MainWindow { DataContext = _serviceProvider.GetRequiredService<MainViewModel>() }.Show();
    }
    #endregion

    #endregion
}