using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Camelot.ViewModels.Implementations;
using Camelot.ViewModels.Implementations.Menu;
using Camelot.Views.Dialogs;
using Xunit;

namespace Camelot.Tests
{
    public class MainWindowTests : IDisposable
    {
        private const int LoadDelayMs = 3000;


        public MainWindowTests()
        {
            AvaloniaApp.RegisterDependencies();
        }
        //
        // [Fact]
        // public async Task TestMainWindowLoading()
        // {
        //     await LoadAppAsync();
        //     var window = AvaloniaApp.GetMainWindow();
        //
        //     Assert.NotNull(window);
        // }
        //
        // [Fact]
        // public async Task TestOpenSettings()
        // {
        //     await LoadAppAsync();
        //
        //     var window = AvaloniaApp.GetMainWindow();
        //     var openMenuTaskCompletionSource = new TaskCompletionSource<bool>();
        //     AvaloniaApp.PostAction(() =>
        //     {
        //         var viewModel = (MainWindowViewModel) window.DataContext;
        //         Assert.NotNull(viewModel);
        //         var menuViewModel = (MenuViewModel) viewModel.MenuViewModel;
        //         menuViewModel.OpenSettingsCommand.Execute(null);
        //         openMenuTaskCompletionSource.SetResult(true);
        //     });
        //
        //     await openMenuTaskCompletionSource.Task;
        //
        //     SettingsDialog dialog = null;
        //     var getDialogTaskCompletionSource = new TaskCompletionSource<bool>();
        //     AvaloniaApp.PostAction(() =>
        //     {
        //         dialog = AvaloniaApp.GetApp().Windows.OfType<SettingsDialog>().SingleOrDefault();
        //         getDialogTaskCompletionSource.SetResult(true);
        //     });
        //
        //     await getDialogTaskCompletionSource.Task;
        //
        //     Assert.NotNull(dialog);
        // }

        [Fact]
        public void TestOpenAbout()
        {
            var dialogExists = false;

            AvaloniaApp
                .BuildAvaloniaApp()
                .AfterSetup(_ =>
                {
                    DispatcherTimer.RunOnce(async () =>
                    {
                        try
                        {
                            var window = AvaloniaApp.GetMainWindow();
                            var menu = window.GetVisualDescendants().OfType<Menu>().First();
                            var menuItem = menu.GetVisualDescendants().OfType<MenuItem>().Skip(2).First();
                            menuItem.IsSubMenuOpen = true;
                            var aboutMenuItem = menuItem.GetLogicalDescendants().OfType<MenuItem>().First();
                            aboutMenuItem.Command?.Execute(null);
                            var dialog = AvaloniaApp.GetApp().Windows.OfType<AboutDialog>().SingleOrDefault();
                            dialogExists = dialog != null;
                        }
                        finally
                        {
                            AvaloniaApp.Stop();
                        }
                    }, TimeSpan.FromSeconds(3));

                })
                .StartWithClassicDesktopLifetime(new[] {"--full-headless"});
            Assert.True(dialogExists);
            //
            //
            // var window = AvaloniaApp.GetMainWindow();
            // var openMenuTaskCompletionSource = new TaskCompletionSource<bool>();
            // AvaloniaApp.PostAction(() =>
            // {
            //     var viewModel = (MainWindowViewModel) window.DataContext;
            //     Assert.NotNull(viewModel);
            //     var menuViewModel = (MenuViewModel) viewModel.MenuViewModel;
            //     menuViewModel.AboutCommand.Execute(null);
            //     openMenuTaskCompletionSource.SetResult(true);
            // });
            //
            // await openMenuTaskCompletionSource.Task;
            //
            // AboutDialog dialog = null;
            // var getDialogTaskCompletionSource = new TaskCompletionSource<bool>();
            // AvaloniaApp.PostAction(() =>
            // {
            //     dialog = AvaloniaApp.GetApp().Windows.OfType<AboutDialog>().SingleOrDefault();
            //     getDialogTaskCompletionSource.SetResult(true);
            // });
            //
            // await getDialogTaskCompletionSource.Task;
            //
            // Assert.NotNull(dialog);
        }

        [Fact]
        public void TestOpenSettings()
        {
            var dialogExists = false;

            AvaloniaApp
                .BuildAvaloniaApp()
                .AfterSetup(_ =>
                {
                    DispatcherTimer.RunOnce(async () =>
                    {
                        try
                        {
                            var window = AvaloniaApp.GetMainWindow();
                            var menu = window.GetVisualDescendants().OfType<Menu>().First();
                            var menuItem = menu.GetVisualDescendants().OfType<MenuItem>().Skip(1).First();
                            menuItem.IsSubMenuOpen = true;
                            var aboutMenuItem = menuItem.GetLogicalDescendants().OfType<MenuItem>().First();
                            aboutMenuItem.Command?.Execute(null);
                            var dialog = AvaloniaApp.GetApp().Windows.OfType<SettingsDialog>().SingleOrDefault();
                            dialogExists = dialog != null;
                        }
                        finally
                        {
                            AvaloniaApp.Stop();
                        }
                    }, TimeSpan.FromSeconds(3));

                })
                .StartWithClassicDesktopLifetime(new[] {"--full-headless"});
            Assert.True(dialogExists);
        }

        public void Dispose() => AvaloniaApp.Stop();

        private static async Task LoadAppAsync()
        {
            AvaloniaApp.RegisterDependencies();
            AvaloniaApp.Start();

            await Task.Delay(LoadDelayMs);
        }
    }
}