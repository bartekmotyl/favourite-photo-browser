using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Favourite_Photo_Browser
{

    public class FolderTreeNode
    {
        private IList<DirectoryInfo> subfolders;
        private ObservableCollection<FolderTreeNode>? children = null;

        public string Name => Dir.Name;
        public string Path => Dir.FullName;
        public DirectoryInfo Dir { get; }
        public bool IsExpanded { get; set; } = false;
        public bool HasChildren
        {
            get
            {
                return (children?.Count ?? subfolders.Count) > 0;
            }
        }

        public ObservableCollection<FolderTreeNode> Children
        {
            get
            {
                if (children != null)
                    return children;

                children = new ObservableCollection<FolderTreeNode>(
                    GetSubdirectoriesSafe(Dir).Select(di => new FolderTreeNode(di)));
                

                return children;
            }
        }

        public FolderTreeNode(DirectoryInfo directory)
        {
            Dir = directory;

            subfolders = GetSubdirectoriesSafe(Dir);
        }

        public static IList<DirectoryInfo> GetDrivesRootDirectories()
        {
            return DriveInfo.GetDrives().Select(d => d.RootDirectory).ToList();
        }
        public static IList<DirectoryInfo> GetSubdirectoriesSafe(DirectoryInfo dir)
        {
            var task = new Task<IList<DirectoryInfo>>(() =>
            {
                try
                {

                    return dir.EnumerateDirectories().OrderBy(di => di.Name.ToLowerInvariant()).ToList();
                }
                catch (Exception)
                {
                    return new List<DirectoryInfo>();
                }
            });
            task.Start();
            if (task.Wait(1000))
                return task.Result;
            else
                return new List<DirectoryInfo>();
        }

    }

    internal class FolderTreeModel
    {

        public HierarchicalTreeDataGridSource<FolderTreeNode> Source { get; }

        public FolderTreeModel(IEnumerable<FolderTreeNode> folders)
        {
            Source = new HierarchicalTreeDataGridSource<FolderTreeNode>(folders)
            {
                Columns =
                {
                    new HierarchicalExpanderColumn<FolderTreeNode>(
                        new TextColumn<FolderTreeNode, string>("Name", x => x.Name),
                        x => x.Children, x => x.HasChildren, x=>x.IsExpanded), 
                },
            };
        }
    }
}
