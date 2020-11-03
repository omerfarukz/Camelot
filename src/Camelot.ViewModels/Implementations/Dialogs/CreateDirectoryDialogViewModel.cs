using System.Threading.Tasks;
using System.Windows.Input;
using Camelot.Services.Abstractions;
using Camelot.ViewModels.Implementations.Dialogs.NavigationParameters;
using Camelot.ViewModels.Implementations.Dialogs.Results;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Camelot.ViewModels.Implementations.Dialogs
{
    public class CreateDirectoryDialogViewModel : ParameterizedDialogViewModelBase<CreateDirectoryDialogResult, CreateNodeNavigationParameter>
    {
        private readonly IDirectoryService _directoryService;
        private readonly IFileService _fileService;
        private readonly IPathService _pathService;

        private string _directoryPath;

        [Reactive]
        public string DirectoryName { get; set; }

        public ICommand CreateCommand { get; }

        public ICommand CancelCommand { get; }

        public CreateDirectoryDialogViewModel(
            IDirectoryService directoryService,
            IFileService fileService,
            IPathService pathService)
        {
            _directoryService = directoryService;
            _fileService = fileService;
            _pathService = pathService;

            var canCreate = this.WhenAnyValue(x => x.DirectoryName,
                CheckIfNameIsValid);

            CreateCommand = ReactiveCommand.Create(CreateDirectory, canCreate);
            CancelCommand = ReactiveCommand.Create(Close);
        }

        public override Task ActivateAsync(CreateNodeNavigationParameter navigationParameter)
        {
            _directoryPath = navigationParameter.DirectoryPath;

            return Task.CompletedTask;
        }

        private void CreateDirectory() => Close(new CreateDirectoryDialogResult(DirectoryName));

        private bool CheckIfNameIsValid(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            var newFullPath = _pathService.Combine(_directoryPath, name);

            return !_fileService.CheckIfExists(newFullPath) && !_directoryService.CheckIfExists(newFullPath);
        }
    }
}