using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ApplicationDispatcher.Interfaces;
using Camelot.DataAccess.Models;
using Camelot.Extensions;
using Camelot.Services.Interfaces;
using Camelot.ViewModels.Factories.Interfaces;
using Camelot.ViewModels.Interfaces.MainWindow;
using DynamicData;
using ReactiveUI;

namespace Camelot.ViewModels.Implementations.MainWindow
{
    public class FilesPanelViewModel : ViewModelBase, IFilesPanelViewModel
    {
        private const string ParentDirectoryName = "[..]";
        
        private readonly IFileService _fileService;
        private readonly IDirectoryService _directoryService;
        private readonly IFilesSelectionService _filesSelectionService;
        private readonly IFileSystemNodeViewModelFactory _fileSystemNodeViewModelFactory;
        private readonly IFileSystemWatchingService _fileSystemWatchingService;
        private readonly IApplicationDispatcher _applicationDispatcher;
        private readonly IFilesPanelStateService _filesPanelStateService;
        private readonly ITabViewModelFactory _tabViewModelFactory;

        private readonly ObservableCollection<IFileSystemNodeViewModel> _fileSystemNodes;
        private readonly ObservableCollection<IFileSystemNodeViewModel> _selectedFileSystemNodes;
        private readonly ObservableCollection<ITabViewModel> _tabs;

        private string _currentDirectory;
        private ITabViewModel _selectedTab;

        public string CurrentDirectory
        {
            get => _currentDirectory;
            set
            {
                var hasChanged = _currentDirectory != value;
                this.RaiseAndSetIfChanged(ref _currentDirectory, value);

                if (hasChanged)
                {
                    ReloadFiles();
                    SelectedTab.CurrentDirectory = _currentDirectory;
                }
            }
        }

        public IEnumerable<IFileSystemNodeViewModel> FileSystemNodes => _fileSystemNodes;

        public IList<IFileSystemNodeViewModel> SelectedFileSystemNodes => _selectedFileSystemNodes;

        public IEnumerable<ITabViewModel> Tabs => _tabs;

        public ITabViewModel SelectedTab
        {
            get => _selectedTab;
            set => this.RaiseAndSetIfChanged(ref _selectedTab, value);
        }

        public ICommand ActivateCommand { get; }

        public ICommand RefreshCommand { get; }

        public event EventHandler<EventArgs> ActivatedEvent;

        public event EventHandler<EventArgs> DeactivatedEvent;

        public FilesPanelViewModel(
            IFileService fileService,
            IDirectoryService directoryService,
            IFilesSelectionService filesSelectionService,
            IFileSystemNodeViewModelFactory fileSystemNodeViewModelFactory,
            IFileSystemWatchingService fileSystemWatchingService,
            IApplicationDispatcher applicationDispatcher,
            IFilesPanelStateService filesPanelStateService,
            ITabViewModelFactory tabViewModelFactory)
        {
            _fileService = fileService;
            _directoryService = directoryService;
            _filesSelectionService = filesSelectionService;
            _fileSystemNodeViewModelFactory = fileSystemNodeViewModelFactory;
            _fileSystemWatchingService = fileSystemWatchingService;
            _applicationDispatcher = applicationDispatcher;
            _filesPanelStateService = filesPanelStateService;
            _tabViewModelFactory = tabViewModelFactory;

            _fileSystemNodes = new ObservableCollection<IFileSystemNodeViewModel>();
            _selectedFileSystemNodes = new ObservableCollection<IFileSystemNodeViewModel>();

            ActivateCommand = ReactiveCommand.Create(Activate);
            RefreshCommand = ReactiveCommand.Create(ReloadFiles);

            var state = _filesPanelStateService.GetPanelState();
            if (!state.Tabs.Any())
            {
                // TODO: get all roots
                state.Tabs = new List<string>
                {
                    _directoryService.GetAppRootDirectory()
                };
            }
            _tabs = new ObservableCollection<ITabViewModel>(state.Tabs.Select(Create));
            _tabs.CollectionChanged += TabsOnCollectionChanged;

            SelectTab(_tabs[state.SelectedTabIndex]);

            this.WhenAnyValue(x => x.CurrentDirectory, x => x.SelectedTab)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Subscribe(_ => SaveState());

            ReloadFiles();
            SubscribeToEvents();
        }

        public void Deactivate()
        {
            var selectedFiles = SelectedFileSystemNodes.Select(f => f.FullPath).ToArray();
            SelectedFileSystemNodes.Clear();
            _filesSelectionService.UnselectFiles(selectedFiles);

            DeactivatedEvent.Raise(this, EventArgs.Empty);
        }
        
        private void TabsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SaveState();
        }

        private ITabViewModel Create(string directory)
        {
            var tabViewModel = _tabViewModelFactory.Create(directory);
            SubscribeToEvents(tabViewModel);

            return tabViewModel;
        }

        private void Remove(ITabViewModel tabViewModel)
        {
            UnsubscribeFromEvents(tabViewModel);

            _tabs.Remove(tabViewModel);
            
            if (SelectedTab == tabViewModel)
            {
                SelectTab(_tabs.First());
            }
        }

        private void SubscribeToEvents(ITabViewModel tabViewModel)
        {
            tabViewModel.ActivationRequested += TabViewModelOnActivationRequested;
            tabViewModel.NewTabRequested += TabViewModelOnNewTabRequested;
            tabViewModel.CloseRequested += TabViewModelOnCloseRequested;
            tabViewModel.ClosingTabsToTheLeftRequested += TabViewModelOnClosingTabsToTheLeftRequested;
            tabViewModel.ClosingTabsToTheRightRequested += TabViewModelOnClosingTabsToTheRightRequested;
            tabViewModel.ClosingAllTabsButThisRequested += TabViewModelOnClosingAllTabsButThisRequested;
        }

        private void UnsubscribeFromEvents(ITabViewModel tabViewModel)
        {
            tabViewModel.ActivationRequested -= TabViewModelOnActivationRequested;
            tabViewModel.NewTabRequested -= TabViewModelOnNewTabRequested;
            tabViewModel.CloseRequested -= TabViewModelOnCloseRequested;
            tabViewModel.ClosingTabsToTheLeftRequested -= TabViewModelOnClosingTabsToTheLeftRequested;
            tabViewModel.ClosingTabsToTheRightRequested -= TabViewModelOnClosingTabsToTheRightRequested;
            tabViewModel.ClosingAllTabsButThisRequested -= TabViewModelOnClosingAllTabsButThisRequested;
        }

        private void TabViewModelOnActivationRequested(object sender, EventArgs e)
        {
            var tabViewModel = (ITabViewModel) sender;

            SelectTab(tabViewModel);
        }
        
        private void TabViewModelOnNewTabRequested(object sender, EventArgs e)
        {
            var tabViewModel = (ITabViewModel) sender;
            var tabPosition = _tabs.IndexOf(tabViewModel);
            var newTabViewModel = Create(tabViewModel.CurrentDirectory);
            
            _tabs.Insert(tabPosition, newTabViewModel);
        }
        
        private void TabViewModelOnCloseRequested(object sender, EventArgs e)
        {
            var tabViewModel = (ITabViewModel) sender;
            if (_tabs.Count > 1)
            {
                Remove(tabViewModel);
            }
        }

        private void TabViewModelOnClosingTabsToTheLeftRequested(object sender, EventArgs e)
        {
            var tabViewModel = (ITabViewModel) sender;
            var tabPosition = _tabs.IndexOf(tabViewModel);
            var tabsToClose = _tabs.Take(tabPosition).ToArray();

            tabsToClose.ForEach(Remove);
        }
        
        private void TabViewModelOnClosingTabsToTheRightRequested(object sender, EventArgs e)
        {
            var tabViewModel = (ITabViewModel) sender;
            var tabPosition = _tabs.IndexOf(tabViewModel);
            var tabsToClose = _tabs.Skip(tabPosition + 1).ToArray();

            tabsToClose.ForEach(Remove);
        }

        private void TabViewModelOnClosingAllTabsButThisRequested(object sender, EventArgs e)
        {
            var tabViewModel = (ITabViewModel) sender;
            var tabsToClose = _tabs.Where(t => t != tabViewModel).ToArray();

            tabsToClose.ForEach(Remove);
        }
        
        private void SelectTab(ITabViewModel tabViewModel)
        {
            if (SelectedTab != null)
            {
                SelectedTab.IsActive = false;
            }

            tabViewModel.IsActive = true;
            SelectedTab = tabViewModel;
            CurrentDirectory = tabViewModel.CurrentDirectory;
        }

        private void SubscribeToEvents()
        {
            _selectedFileSystemNodes.CollectionChanged += SelectedFileSystemNodesOnCollectionChanged;

            void ReloadInUiThread() => _applicationDispatcher.Dispatch(ReloadFiles);

            // TODO: don't reload all files, process update properly
            _fileSystemWatchingService.FileCreated += (sender, args) => ReloadInUiThread();
            _fileSystemWatchingService.FileChanged += (sender, args) => ReloadInUiThread();
            _fileSystemWatchingService.FileRenamed += (sender, args) => ReloadInUiThread();
            _fileSystemWatchingService.FileDeleted += (sender, args) => ReloadInUiThread();
        }

        private void Activate()
        {
            ActivatedEvent.Raise(this, EventArgs.Empty);
        }

        private void ReloadFiles()
        {
            if (!_directoryService.CheckIfDirectoryExists(CurrentDirectory))
            {
                return;
            }

            _fileSystemWatchingService.StopWatching();

            var parentDirectory = _directoryService.GetParentDirectory(CurrentDirectory);
            var directories = _directoryService.GetDirectories(CurrentDirectory);
            var files = _fileService.GetFiles(CurrentDirectory);

            var directoriesViewModels = directories
                .Select(_fileSystemNodeViewModelFactory.Create);
            var filesViewModels = files
                .Select(_fileSystemNodeViewModelFactory.Create);
            var models = directoriesViewModels.Concat(filesViewModels);

            _fileSystemNodes.Clear();
            _fileSystemNodes.AddRange(models);

            if (parentDirectory != null)
            {
                var parentDirectoryViewModel = _fileSystemNodeViewModelFactory.Create(parentDirectory);
                parentDirectoryViewModel.Name = ParentDirectoryName; // TODO: FIX
                
                _fileSystemNodes.Insert(0, parentDirectoryViewModel);
            }

            _fileSystemWatchingService.StartWatching(CurrentDirectory);
        }

        private void SelectedFileSystemNodesOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var filesToAdd = e.NewItems?
                .Cast<IFileSystemNodeViewModel>()
                .Select(f => f.FullPath);
            if (filesToAdd != null)
            {
                _filesSelectionService.SelectFiles(filesToAdd);
            }

            var filesToRemove = e.OldItems?
                .Cast<IFileSystemNodeViewModel>()
                .Select(f => f.FullPath);
            if (filesToRemove != null)
            {
                _filesSelectionService.UnselectFiles(filesToRemove);
            }
        }

        private void SaveState()
        {
            Task.Run(() =>
            {
                var tabs = _tabs.Select(t => t.CurrentDirectory).ToList();
                var selectedTabIndex = _tabs.IndexOf(_selectedTab);
                var state = new PanelState
                {
                    Tabs = tabs,
                    SelectedTabIndex = selectedTabIndex
                };

                _filesPanelStateService.SavePanelState(state);
            });
        }
    }
}