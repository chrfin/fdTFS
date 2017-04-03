using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SourceControl.Sources;
using System.IO;
using PluginCore;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Windows.Forms;

namespace fdTFS.Sources.Tfs
{
    class FileActions : IVCFileActions
    {
        private TfsManager manager;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileActions"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public FileActions(TfsManager manager)
        {
            this.manager = manager;
        }

        public bool FileBeforeRename(string path)
        {
            return false;
        }

        public bool FileDelete(string[] paths, bool confirm)
        {
            foreach (ITabbedDocument document in PluginBase.MainForm.Documents)
            {
                if (paths.Contains(document.FileName))
                {
                    document.Save();
                    document.Close();
                }
            }
            List<string> pathList = new List<string>();
            foreach (string path in paths)
            {
                PendingChange change = manager.GetPendingChange(path, Path.GetDirectoryName(path));
                if (change == null)
                {
                    pathList.Add(path);
                    continue;
                }
                if (change.IsAdd)
                {
                    manager.CurrentWorkspace.Undo(path);
                    pathList.Remove(path);
                    continue;
                }
            }
            if (pathList.Count > 0)
                manager.CurrentWorkspace.PendDelete(pathList.ToArray(), RecursionType.Full, LockLevel.None, false);
            foreach (string path in paths)
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(path, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            return true;
        }

        public bool FileRename(string path, string newName)
        {
            string newPath = Path.Combine(Path.GetDirectoryName(path), newName);
            return Move(path, newPath);
        }
        public bool FileMove(string fromPath, string toPath)
        {
            return Move(fromPath, Path.Combine(toPath, Path.GetFileName(fromPath)));
        }
        public bool Move(string source, string target)
        {
            if (!manager.IsPathUnderVC(source))
                return false;

            foreach (ITabbedDocument document in PluginBase.MainForm.Documents)
            {
                if (source == document.FileName && document.IsModified)
                    document.Save();
            }

            manager.CurrentWorkspace.PendRename(source, target, Microsoft.TeamFoundation.VersionControl.Client.LockLevel.None, true, false);

            foreach (ITabbedDocument document in PluginBase.MainForm.Documents.ToArray())
            {
                if (source == document.FileName)
                    document.Close();
            }
            return true;
        }

        public bool FileNew(string path)
        {
            manager.CurrentWorkspace.PendAdd(path);
            return false;
        }
        public bool FileOpen(string path)
        {
            return CheckOpenDocumentText(path);
        }
        public bool FileReload(string path)
        {
            return CheckOpenDocumentText(path);
        }
        public bool CheckOpenDocumentText(string path)
        {
            string symbol = " " + (manager.PluginMain.Settings as Settings).CheckedInCharacter;
            if ((new FileInfo(path)).IsReadOnly)
            {
                foreach (ITabbedDocument document in PluginBase.MainForm.Documents)
                {
                    if (path == document.FileName && !document.Text.EndsWith(symbol))
                        document.Text += symbol;
                }
            }
            else
            {
                foreach (ITabbedDocument document in PluginBase.MainForm.Documents)
                {
                    if (path == document.FileName && document.Text.EndsWith(symbol))
                        document.Text = document.Text.Remove(document.Text.Length - symbol.Length);
                }
            }
            return false;
        }

        public bool FileModifyRO(string path)
        {
            if (manager.CurrentWorkingFolder != null)
            {
                PendingChange change = manager.GetPendingChange(path, manager.CurrentWorkingFolder.LocalItem);
                if (change == null || !change.IsEdit)
                {
                    CheckOutForm checkOutForm = new CheckOutForm();
                    checkOutForm.FilesToCheckOut = new List<string>() { PluginBase.MainForm.CurrentDocument.FileName };
                    checkOutForm.LockLevel = (manager.PluginMain.Settings as Settings).DefaultLockLevel;
                    if (!(manager.PluginMain.Settings as Settings).PromptForLockLevel || checkOutForm.ShowDialog() != DialogResult.Cancel)
                    {
                        manager.CurrentWorkspace.PendEdit(checkOutForm.SelectedFilesToCheckOut.ToArray(), RecursionType.None, null, checkOutForm.LockLevel);
                        PluginBase.MainForm.CurrentDocument.Reload(false);
                    }
                }

                return true;
            }

            return false;
        }

        public bool BuildProject() { return false; }
        public bool TestProject() { return false; }

        public bool SaveProject()
        {
            if (manager.CurrentWorkingFolder == null)
                return false;

            ProjectManager.Projects.Project project = PluginBase.CurrentProject as ProjectManager.Projects.Project;
            Workstation ws = Workstation.Current;
            WorkspaceInfo wsi = ws.GetLocalWorkspaceInfo(project.ProjectPath);
            if (wsi == null)
                return false;

            PendingChange[] changes = manager.CurrentWorkspace.GetPendingChanges(project.ProjectPath);
            if (changes.Length > 0)
                return false;

            CheckOutForm checkOutForm = new CheckOutForm();
            checkOutForm.FilesToCheckOut = new List<string>() { project.ProjectPath };
            checkOutForm.LockLevel = (manager.PluginMain.Settings as Settings).DefaultLockLevel;
            if (checkOutForm.ShowDialog() != DialogResult.Cancel)
            {
                manager.CurrentWorkspace.PendEdit(checkOutForm.SelectedFilesToCheckOut.ToArray(), RecursionType.None, null, checkOutForm.LockLevel);
                return false;
            }
            else
                return true;
        }
    }
}
