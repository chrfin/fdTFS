using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SourceControl.Sources;
using System.Windows.Forms;
using PluginCore.Localization;
using PluginCore;
using fdTFS.Properties;
using ProjectManager.Controls.TreeView;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.IO;
using fdTFS.Dialogs;

namespace fdTFS.Sources.Tfs
{
    class MenuItems : IVCMenuItems
    {
        private TreeNode[] currentNodes;
        private IVCManager currentManager;

        /// <summary>
        /// Set by SC plugin to provide the selected files/dirs
        /// </summary>
        /// <value></value>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public TreeNode[] CurrentNodes { set { currentNodes = value; } }
        /// <summary>
        /// Set by SC plugin to provide the manager instance
        /// </summary>
        /// <value></value>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public IVCManager CurrentManager { set { currentManager = value; } }
        internal TfsManager Manager { get { return currentManager as TfsManager; } }

        public ToolStripItem Update { get { return null; } }
        public ToolStripItem Commit { get { return null; } }
        public ToolStripItem Push { get { return null; } }
        public ToolStripItem ShowLog { get { return null; } }
        public ToolStripItem MidSeparator { get { return null; } }
        public ToolStripItem Diff { get { return null; } }
        public ToolStripItem DiffChange { get { return null; } }
        public ToolStripItem Add { get { return null; } }
        public ToolStripItem Ignore { get { return null; } }
        public ToolStripItem UndoAdd { get { return null; } }
        public ToolStripItem Revert { get { return null; } }
        public ToolStripItem EditConflict { get { return null; } }

        private Dictionary<ToolStripItem, VCMenutItemProperties> items;
        public Dictionary<ToolStripItem, VCMenutItemProperties> Items { get { return items; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuItems"/> class.
        /// </summary>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public MenuItems()
        {
            items = new Dictionary<ToolStripItem, VCMenutItemProperties>();
            items.Add(new ToolStripMenuItem("&Add", Resources.add, add_Clicked), new VCMenutItemProperties() { Show = s => s.Ignored > s.Dirs });
            items.Add(new ToolStripMenuItem("Get &Latest Version", Resources.getLatest, getLatestVersion_Clicked), new VCMenutItemProperties() { Show = s => s.Ignored < s.Files + s.Dirs || s.Dirs > 0 });
            items.Add(new ToolStripMenuItem("&Get Specific Version...", Resources.getVersion, getSpecificVersion_Clicked), new VCMenutItemProperties() { Show = s => s.Ignored < s.Files + s.Dirs || s.Dirs > 0 });
            items.Add(new ToolStripMenuItem("Chec&k Out for Edit...", Resources.checkOut, checkOut_Clicked), new VCMenutItemProperties() { Show = s => s.Files > s.Modified + s.Ignored + s.Added || s.Dirs > 0 });
            items.Add(new ToolStripMenuItem("Check &In...", Resources.CheckIn, checkIn_Clicked), new VCMenutItemProperties() { Show = s => s.Modified > 0 || s.Replaced > 0 || s.Added > 0 });
            items.Add(new ToolStripMenuItem("&Undo Pending Changes...", Resources.Undo, undo_Clicked), new VCMenutItemProperties() { Show = s => s.Modified > 0 || s.Replaced > 0 });
            items.Add(new ToolStripMenuItem("View &History", Resources.history, history_Clicked), new VCMenutItemProperties() { Show = s => true, Enable = s => s.Dirs == 0 && s.Files == 1 && s.Ignored == 0 || s.Dirs == 1 && s.Files == 0 });
            items.Add(new ToolStripMenuItem("&Compare...", Resources.compare, compare_Clicked), new VCMenutItemProperties() { Show = s => true, Enable = s => s.Dirs == 0 && s.Files == 1 && s.Ignored == 0 });
        }

        /// <summary>
        /// Handles the Clicked event of the add control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-13</remarks>
        public void add_Clicked(object sender, EventArgs e) { Manager.CurrentWorkspace.PendAdd(GetPaths().ToArray(), true); Manager.TriggerOnChange(); }
        /// <summary>
        /// Handles the Clicked event of the getLatestVersion control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public void getLatestVersion_Clicked(object sender, EventArgs e) { Manager.Get(GetPaths()); }
        /// <summary>
        /// Handles the Clicked event of the getSpecificVersion control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public void getSpecificVersion_Clicked(object sender, EventArgs e) { Manager.GetSpecificVersion(GetPaths().First()); }
        /// <summary>
        /// Handles the Clicked event of the checkOut control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public void checkOut_Clicked(object sender, EventArgs e)
        {
            List<string> paths = GetPaths(true);
            CheckOutForm checkOutForm = new CheckOutForm();
            checkOutForm.FilesToCheckOut = paths.FindAll(f => Manager.IsPathUnderVC(f));
            checkOutForm.LockLevel = (Manager.PluginMain.Settings as Settings).DefaultLockLevel;
            if (checkOutForm.ShowDialog() != DialogResult.Cancel)
            {
                Manager.CurrentWorkspace.PendEdit(checkOutForm.SelectedFilesToCheckOut.ToArray(), RecursionType.None, null, checkOutForm.LockLevel);

                foreach (ITabbedDocument document in PluginBase.MainForm.Documents)
                {
                    if (checkOutForm.SelectedFilesToCheckOut.FirstOrDefault(p => p == document.FileName) != null)
                        document.Reload(false);
                }
            }
        }
        /// <summary>
        /// Handles the Clicked event of the checkIn control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public void checkIn_Clicked(object sender, EventArgs e) 
        {
            CheckInForm checkIn = new CheckInForm(Manager, GetPaths());
            if (checkIn.ShowDialog() == DialogResult.OK)
            {
                foreach (ITabbedDocument document in PluginBase.MainForm.Documents)
                {
                    if (checkIn.CheckedInChanges.FirstOrDefault(c => c.LocalItem == document.FileName) != null)
                        document.Reload(false);
                }
            }
        }
        /// <summary>
        /// Handles the Clicked event of the undo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public void undo_Clicked(object sender, EventArgs e)
        {
            List<PendingChange> changes = new List<PendingChange>(Manager.CurrentWorkspace.GetPendingChangesEnumerable(Manager.CurrentWorkingFolder.LocalItem, RecursionType.Full));
            UndoPendingChangesForm undoForm = new UndoPendingChangesForm();
            undoForm.PendingChanges = changes;
            foreach (string path in GetPaths())
                undoForm.PreCheckPendingChange(changes.Find(c => c.LocalItem == path));
            if (undoForm.ShowDialog() == DialogResult.OK)
                Manager.UndoCheckOut(undoForm.SelectedPendingChanges);
        }
        /// <summary>
        /// Handles the Clicked event of the history control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public void history_Clicked(object sender, EventArgs e) { Manager.ViewHistory(GetPaths().First()); }
        /// <summary>
        /// Handles the Clicked event of the compare control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public void compare_Clicked(object sender, EventArgs e) { Manager.Compare(GetPaths().First()); }

        private List<string> GetPaths() { return GetPaths(false); }
        private List<string> GetPaths(bool recursive)
        {
            List<string> paths = new List<string>();
            if (currentNodes != null)
            {
                foreach (TreeNode node in currentNodes)
                {
                    if (node is GenericNode)
                    {
                        string path = (node as GenericNode).BackingPath;
                        if (!recursive || (File.GetAttributes(path) & FileAttributes.Directory) != FileAttributes.Directory)
                            paths.Add(path);
                        else
                            paths.AddRange(Directory.GetFiles(path, "*.*", SearchOption.AllDirectories).ToList());
                    }
                }
            }
            return paths;
        }
    }
}
