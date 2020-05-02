using Camelot.Services.Abstractions;
using Camelot.Services.Abstractions.Behaviors;
using Camelot.ViewModels.Services.Interfaces;

namespace Camelot.ViewModels.Implementations.MainWindow.FilePanels
{
    public class DirectoryViewModel : FileSystemNodeViewModelBase
    {
        public bool IsParentDirectory { get; set; }
        
        public DirectoryViewModel(
            IFileSystemNodeOpeningBehavior fileSystemNodeOpeningBehavior,
            IOperationsService operationsService,
            IClipboardOperationsService clipboardOperationsService,
            IFilesOperationsMediator filesOperationsMediator) 
            : base(
                fileSystemNodeOpeningBehavior,
                operationsService,
                clipboardOperationsService,
                filesOperationsMediator)
        {
            
        }
    }
}