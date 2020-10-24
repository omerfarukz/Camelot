using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.ReactiveUI;
using Avalonia.Threading;
using Camelot.DependencyInjection;
using Splat;

namespace Camelot.Tests
{
    public static class AvaloniaApp
    {
        public static void Start(params string[] args) =>
            Task.Run(() => BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args, ShutdownMode.OnMainWindowClose));

        public static void RegisterDependencies() =>
            Bootstrapper.Register(Locator.CurrentMutable, Locator.Current);

        public static Window GetMainWindow() => GetApp().MainWindow;

        public static IClassicDesktopStyleApplicationLifetime GetApp() =>
            (IClassicDesktopStyleApplicationLifetime) Application.Current.ApplicationLifetime;

        public static void Stop() => GetApp().Shutdown();

        public static void PostAction(Action action) => Dispatcher.UIThread.Post(action);

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder
                .Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .UseHeadless();
    }
}