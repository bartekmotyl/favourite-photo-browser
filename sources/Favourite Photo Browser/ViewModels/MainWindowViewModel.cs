using Avalonia.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Favourite_Photo_Browser.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly AvaloniaList<FolderItemViewModel> folderItems = new();

        public AvaloniaList<FolderItemViewModel> FolderItems => folderItems;
    }
}
