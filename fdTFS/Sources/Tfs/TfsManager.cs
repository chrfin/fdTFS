using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SourceControl.Sources;
using System.IO;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.Client;
using System.Windows.Forms;
using PluginCore;
using WeifenLuo.WinFormsUI.Docking;
using fdTFS.Properties;
using System.Threading;
using SourceControl.Actions;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using fdTFS.Sources.Tfs;
using PluginCore.Managers;

namespace fdTFS
{
    public class TfsManager : IVCManager
    {
        #region IVCManger
        /// <summary>
        /// Notify that some versionned files' state changed (to update treeview)
        /// </summary>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public event VCManagerStatusChange OnChange;

        private IVCMenuItems menuItems;
        /// <summary>
        /// Gets the menu items.
        /// </summary>
        /// <value>The menu items.</value>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public IVCMenuItems MenuItems { get { return menuItems; } }

        private IVCFileActions fileActions;
        /// <summary>
        /// Gets the file actions.
        /// </summary>
        /// <value>The file actions.</value>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public IVCFileActions FileActions
        {
            get { return fileActions; }
        }

        /// <summary>
        /// Return if the location is under VC
        /// - if true, all the subtree will be considered under VC too.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public bool IsPathUnderVC(string path)
        {
            return CurrentWorkspace.VersionControlServer.ServerItemExists(path, ItemType.Any);
        }

        /// <summary>
        /// Return a file/dir status
        /// </summary>
        /// <param name="path"></param>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public VCItemStatus GetOverlay(string path, string rootPath)
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.Directory) == FileAttributes.Directory)
                return VCItemStatus.Ignored;
            PendingChange change = GetPendingChange(path, rootPath);
            if (change != null)
            {
                if (change.IsAdd)
                    return VCItemStatus.Added;
                if (change.IsEdit)
                    return VCItemStatus.Modified;
                if (change.IsRename)
                    return VCItemStatus.Replaced;
            }
            if (CurrentWorkspace.VersionControlServer.ServerItemExists(path, ItemType.File))
            {
                return VCItemStatus.UpToDate;
                //string serverPath = CurrentWorkspace.GetServerItemForLocalItem(path);
                //Item item = CurrentWorkspace.VersionControlServer.GetItem(serverPath);
            }
            return VCItemStatus.Ignored;
        }
        public List<VCStatusReport> GetAllOverlays(string path, string rootPath)
        {
            List<VCStatusReport> result = new List<VCStatusReport>();
            foreach (string file in Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories))
                result.Add(new VCStatusReport(file, GetOverlay(file, rootPath)));
            return result;
        }

        /// <summary>
        /// SC request for refreshing status of items
        /// - expected that OnChange is fired to notify when status has been updated
        /// </summary>
        /// <param name="rootPath"></param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public void GetStatus(string rootPath)
        {
            UpdatePendingChanges(rootPath);
            TriggerOnChange();
        }

        /// <summary>
        /// SC notification that IO changes happened in a location under VC
        /// </summary>
        /// <param name="path"></param>
        /// <param name="rootPath"></param>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public bool SetPathDirty(string path, string rootPath)
        {
            if (changesCache.ContainsKey(rootPath))
                changesCache.Remove(rootPath);
            return UpdatePendingChanges(rootPath);
        }
        #endregion

        #region constants

        /// <summary>
        /// The lifetime of the cache in minutes.
        /// </summary>
        const int CACHELIFETIME = 1;

        #endregion

        #region private variables

        private PluginMain pluginMain;
        private PluginUI pluginUI;
        private DockContent pluginPanel;

        private Dictionary<string, WorkspaceInfo> workspaceInfos = new Dictionary<string, WorkspaceInfo>();
        private Dictionary<string, TfsTeamProjectCollection> tfsServers = new Dictionary<string, TfsTeamProjectCollection>();
        private Dictionary<Uri, VersionControlServer> versionControlServers = new Dictionary<Uri, VersionControlServer>();
        private Dictionary<Uri, WorkItemStore> workItemStores = new Dictionary<Uri, WorkItemStore>();
        private Dictionary<string, TeamProject> teamProjects = new Dictionary<string, TeamProject>();
        private Dictionary<string, Workspace> workspaces = new Dictionary<string, Workspace>();
        private Dictionary<string, WorkingFolder> workingFolders = new Dictionary<string, WorkingFolder>();

        #endregion

        #region caches

        private Dictionary<Uri, TfsTeamProjectCollection> tfsCache = new Dictionary<Uri, TfsTeamProjectCollection>();

        private Dictionary<string, Dictionary<string, PendingChange>> changesCache = new Dictionary<string, Dictionary<string, PendingChange>>();
        private Dictionary<string, DateTime> cacheLifetime = new Dictionary<string, DateTime>();

        #endregion

        #region Properties

        /// <summary>
        /// Gets the plugin main.
        /// </summary>
        /// <value>The plugin main.</value>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public PluginMain PluginMain { get { return pluginMain; } }

        /// <summary>
        /// Gets the TFS servers.
        /// </summary>
        /// <value>The TFS servers.</value>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        public Dictionary<string, TfsTeamProjectCollection> TfsServers { get { return tfsServers; } }
        /// <summary>
        /// Gets the work item stores.
        /// </summary>
        /// <value>The work item stores.</value>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        public Dictionary<Uri, WorkItemStore> WorkItemStores { get { return workItemStores; } }
        /// <summary>
        /// Gets the team projects.
        /// </summary>
        /// <value>The team projects.</value>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        public Dictionary<string, TeamProject> TeamProjects { get { return teamProjects; } }
        /// <summary>
        /// Gets the version control servers.
        /// </summary>
        /// <value>The version control servers.</value>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        public Dictionary<Uri, VersionControlServer> VersionControlServers { get { return versionControlServers; } }
        /// <summary>
        /// Gets the workspaces.
        /// </summary>
        /// <value>The workspaces.</value>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        public Dictionary<string, Workspace> Workspaces { get { return workspaces; } }
        /// <summary>
        /// Gets the working folders.
        /// </summary>
        /// <value>The working folders.</value>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        public Dictionary<string, WorkingFolder> WorkingFolders { get { return workingFolders; } }

        #endregion

        #region internal methods

        /// <summary>
        /// Gets the workspace info.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal WorkspaceInfo GetWorkspaceInfo(string path)
        {
            if (workspaceInfos.ContainsKey(path))
                return workspaceInfos[path];

            Workstation ws = Workstation.Current;
            WorkspaceInfo wsi = ws.GetLocalWorkspaceInfo(path);
            workspaceInfos[path] = wsi;
            return wsi;
        }
        /// <summary>
        /// Gets the TFS server.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal TfsTeamProjectCollection GetTfsServer(string path)
        {
            if (tfsServers.ContainsKey(path))
                return tfsServers[path];

            TfsTeamProjectCollection tfs = GetTfsServer(GetWorkspaceInfo(path).ServerUri);
            tfsServers[path] = tfs;

            workingFolders[path] = GetWorkspace(path).Folders.FirstOrDefault(f => f.LocalItem != null && path.StartsWith(f.LocalItem));
            if (workingFolders[path] == null)
                return tfs;

            workspaces[path] = versionControlServers[tfs.Uri].GetWorkspace(workingFolders[path].LocalItem);
            teamProjects[path] = versionControlServers[tfs.Uri].GetTeamProjectForServerPath(workingFolders[path].ServerItem);

            return tfs;
        }
        /// <summary>
        /// Gets the TFS server.
        /// </summary>
        /// <param name="serverUri">The server URI.</param>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal TfsTeamProjectCollection GetTfsServer(Uri serverUri)
        {
            if (tfsCache.ContainsKey(serverUri))
                return tfsCache[serverUri];

            TfsTeamProjectCollection tfs = new TfsTeamProjectCollection(serverUri);
            tfsCache[serverUri] = tfs;

            VersionControlServer versionControlServer = tfs.GetService<VersionControlServer>();
            versionControlServers[serverUri] = versionControlServer;

            versionControlServer.NewPendingChange += new PendingChangeEventHandler(versionControlServer_NewPendingChange);
            versionControlServer.PendingChangesChanged += new WorkspaceEventHandler(versionControlServer_PendingChangesChanged);
            versionControlServer.UndonePendingChange += new PendingChangeEventHandler(versionControlServer_UndonePendingChange);
            versionControlServer.AfterWorkItemsUpdated += new AfterWorkItemsUpdatedEventHandler(versionControlServer_AfterWorkItemsUpdated);
            versionControlServer.ChangesetReconciled += new ChangesetReconciledEventHandler(versionControlServer_ChangesetReconciled);
            versionControlServer.CommitCheckin += new CommitCheckinEventHandler(versionControlServer_CommitCheckin);
            versionControlServer.CommitShelveset += new CommitShelvesetEventHandler(versionControlServer_CommitShelveset);
            versionControlServer.Conflict += new ConflictEventHandler(versionControlServer_Conflict);
            versionControlServer.GetCompleted += new WorkspaceEventHandler(versionControlServer_GetCompleted);
            versionControlServer.Getting += new GettingEventHandler(versionControlServer_Getting);
            versionControlServer.Merging += new MergeEventHandler(versionControlServer_Merging);
            versionControlServer.NonFatalError += new ExceptionEventHandler(versionControlServer_NonFatalError);
            versionControlServer.OperationFinished += new OperationEventHandler(versionControlServer_OperationFinished);
            versionControlServer.ResolvedConflict += new ResolvedConflictEventHandler(versionControlServer_ResolvedConflict);
            versionControlServer.UnshelveShelveset += new UnshelveShelvesetEventHandler(versionControlServer_UnshelveShelveset);
            versionControlServer.UpdatedWorkspace += new WorkspaceEventHandler(versionControlServer_UpdatedWorkspace);
            versionControlServer.WorkItemUpdated += new WorkItemUpdatedEventHandler(versionControlServer_WorkItemUpdated);

            workItemStores[serverUri] = tfs.GetService<WorkItemStore>();

            pluginUI.UpdatePendingChanges();

            return tfs;
        }
        /// <summary>
        /// Gets the workspace.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal Workspace GetWorkspace(string path)
        {
            if (workspaces.ContainsKey(path))
                return workspaces[path];

            Workspace workspace = GetWorkspaceInfo(path).GetWorkspace(GetTfsServer(path));
            workspaces[path] = workspace;
            return workspace;
        }
        /// <summary>
        /// Gets the working folder.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal WorkingFolder GetWorkingFolder(string path)
        {
            if (workingFolders.ContainsKey(path))
                return workingFolders[path];

            WorkingFolder wf = GetWorkspace(path).Folders.FirstOrDefault(f => f.LocalItem != null && path.StartsWith(f.LocalItem));
            workingFolders[path] = wf;
            return wf;
        }

        /// <summary>
        /// Gets the current workspace.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal Workspace CurrentWorkspace { get { return GetWorkspace(ProjectWatcher.CurrentProject.Directory); } }
        /// <summary>
        /// Gets the current working folder.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal WorkingFolder CurrentWorkingFolder { get { return GetWorkingFolder(ProjectWatcher.CurrentProject.Directory); } }
        /// <summary>
        /// Gets the current work item store.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal WorkItemStore CurrentWorkItemStore { get { TfsTeamProjectCollection tfs = GetTfsServer(ProjectWatcher.CurrentProject.Directory); return workItemStores[tfs.Uri]; } }
        /// <summary>
        /// Gets the current team project.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal TeamProject CurrentTeamProject
        {
            get
            {
                string path = ProjectWatcher.CurrentProject.Directory;
                if (!teamProjects.ContainsKey(path))
                    GetTfsServer(path);

                return teamProjects[path];
            }
        }

        /// <summary>
        /// Gets the pending change.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="rootPath">The root path.</param>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal PendingChange GetPendingChange(string path, string rootPath)
        {
            Dictionary<string, PendingChange> changes = GetPendingChanges(rootPath);

            if (changes.ContainsKey(path))
                return changes[path];
            return null;
        }
        /// <summary>
        /// Gets the pending change.
        /// </summary>
        /// <param name="rootPath">The root path.</param>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal Dictionary<string, PendingChange> GetPendingChanges(string rootPath)
        {
            if (changesCache.ContainsKey(rootPath))
            {
                if (cacheLifetime[rootPath] < DateTime.Now)
                    changesCache.Remove(rootPath);
                else
                    return changesCache[rootPath];
            }

            PendingChange[] changes = GetWorkspace(rootPath).GetPendingChanges(rootPath, RecursionType.Full);
            changesCache[rootPath] = new Dictionary<string, PendingChange>();
            foreach (PendingChange change in changes)
                changesCache[rootPath][change.LocalItem] = change;

            cacheLifetime[rootPath] = DateTime.Now + new TimeSpan(0, CACHELIFETIME, 0);

            return changesCache[rootPath];
        }

        #endregion

        #region Version Control Server Events

        /// <summary>
        /// Handles the NewPendingChange event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.PendingChangeEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_NewPendingChange(object sender, PendingChangeEventArgs e) { UpdatePendingChanges(CurrentWorkingFolder.LocalItem); }
        /// <summary>
        /// Handles the PendingChangesChanged event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.WorkspaceEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_PendingChangesChanged(object sender, WorkspaceEventArgs e) { UpdatePendingChanges(CurrentWorkingFolder.LocalItem); }
        /// <summary>
        /// Handles the UndonePendingChange event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.PendingChangeEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_UndonePendingChange(object sender, PendingChangeEventArgs e) { UpdatePendingChanges(CurrentWorkingFolder.LocalItem); }
        /// <summary>
        /// Handles the WorkItemUpdated event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.WorkItemUpdatedEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_WorkItemUpdated(object sender, WorkItemUpdatedEventArgs e) { pluginUI.UpdatePendingChanges(); }
        /// <summary>
        /// Handles the UpdatedWorkspace event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.WorkspaceEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_UpdatedWorkspace(object sender, WorkspaceEventArgs e) { pluginUI.UpdatePendingChanges(); }
        /// <summary>
        /// Handles the UnshelveShelveset event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.UnshelveShelvesetEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_UnshelveShelveset(object sender, UnshelveShelvesetEventArgs e) { pluginUI.UpdatePendingChanges(); }
        /// <summary>
        /// Handles the OperationFinished event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.OperationEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_OperationFinished(object sender, OperationEventArgs e) { pluginUI.UpdatePendingChanges(); }
        /// <summary>
        /// Handles the Getting event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.GettingEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_Getting(object sender, GettingEventArgs e) { }
        /// <summary>
        /// Handles the GetCompleted event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.WorkspaceEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_GetCompleted(object sender, WorkspaceEventArgs e) { pluginUI.UpdatePendingChanges(); }
        /// <summary>
        /// Handles the CommitShelveset event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.CommitShelvesetEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_CommitShelveset(object sender, CommitShelvesetEventArgs e) { pluginUI.UpdatePendingChanges(); }
        /// <summary>
        /// Handles the CommitCheckin event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.CommitCheckinEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_CommitCheckin(object sender, CommitCheckinEventArgs e) { pluginUI.UpdatePendingChanges(); }
        /// <summary>
        /// Handles the AfterWorkItemsUpdated event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.WorkItemsUpdateEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_AfterWorkItemsUpdated(object sender, WorkItemsUpdateEventArgs e) { pluginUI.UpdatePendingChanges(); }
        /// <summary>
        /// Handles the ChangesetReconciled event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.ChangesetReconciledEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_ChangesetReconciled(object sender, ChangesetReconciledEventArgs e) { pluginUI.UpdatePendingChanges(); }

        /// <summary>
        /// Handles the Merging event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.MergeEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_Merging(object sender, MergeEventArgs e)
        {
            MessageBox.Show("Merging");
        }

        /// <summary>
        /// Handles the Conflict event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.ConflictEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_Conflict(object sender, ConflictEventArgs e)
        {
            MessageBox.Show("Conflict: " + e.Message);
        }
        /// <summary>
        /// Handles the ResolvedConflict event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.ResolvedConflictEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_ResolvedConflict(object sender, ResolvedConflictEventArgs e)
        {
            MessageBox.Show("Conflict solved: " + e.Conflict.MergedFileName);
        }

        /// <summary>
        /// Handles the NonFatalError event of the versionControlServer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.TeamFoundation.VersionControl.Client.ExceptionEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        protected void versionControlServer_NonFatalError(object sender, ExceptionEventArgs e)
        {
            if (e.Exception != null)
            {
                TraceManager.Add("Non Fatal Exception: " + e.Exception.Message);
                if ((PluginMain.Settings as Settings).PopupOnNonFatalException)
                    MessageBox.Show(e.Exception.Message, "Non Fatal Exception");
            }
            if (e.Failure != null)
            {
                TraceManager.Add("Non Fatal Failure: " + e.Failure.Message);
                if ((PluginMain.Settings as Settings).PopupOnNonFatalFailure)
                    MessageBox.Show(e.Failure.Message, "Non Fatal Failure");
            }
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TfsManager"/> class.
        /// </summary>
        /// <param name="pluginMain">The plugin main.</param>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        public TfsManager(PluginMain pluginMain)
        {
            this.pluginMain = pluginMain;

            pluginUI = new PluginUI(this);
            pluginUI.Text = "Pending Changes";
            pluginPanel = PluginBase.MainForm.CreateDockablePanel(pluginUI, pluginMain.Guid, Resources.icon, DockState.DockBottomAutoHide);
            pluginPanel.FormClosing += new FormClosingEventHandler(pluginPanel_FormClosing);

            ToolStripMenuItem viewMenu = (ToolStripMenuItem)PluginBase.MainForm.FindMenuItem("ViewMenu");
            viewMenu.DropDownItems.Add(new ToolStripMenuItem("Pending Changes", Resources.icon, new EventHandler(this.OpenPanel)));

            menuItems = new MenuItems();
            fileActions = new FileActions(this);
        }

        /// <summary>
        /// Opens the plugin panel if closed
        /// </summary>
        public void OpenPanel(Object sender, System.EventArgs e) { pluginPanel.Show(); }

        /// <summary>
        /// Handles the FormClosing event of the pluginPanel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.FormClosingEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        private void pluginPanel_FormClosing(object sender, FormClosingEventArgs e) { }

        /// <summary>
        /// Updates the pending changes.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        internal bool UpdatePendingChanges(string path)
        {
            if (!IsPathUnderVC(path))
                return false;

            pluginUI.UpdatePendingChanges(true);
            if (OnChange != null)
                OnChange(this);

            return true;
        }

        /// <summary>
        /// Triggers the on change.
        /// </summary>
        /// <remarks>Documented by CFI, 2011-01-13</remarks>
        internal void TriggerOnChange()
        {
            changesCache.Clear();
            cacheLifetime.Clear();

            if (OnChange != null)
                OnChange(this);
        }

        /// <summary>
        /// Gets the specified paths.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        internal void Get(List<string> paths)
        {
            CurrentWorkspace.Get(paths.ToArray(), VersionSpec.Latest, RecursionType.Full, GetOptions.GetAll);

            foreach (ITabbedDocument document in PluginBase.MainForm.Documents)
            {
                paths.ForEach(delegate(string path)
                {
                    if (document.FileName.StartsWith(path))
                    {
                        document.Reload(true);
                        return;
                    }
                });
            }
        }

        /// <summary>
        /// Gets the specific version.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        internal void GetSpecificVersion(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Views the history.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        internal void ViewHistory(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Compares the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        internal void Compare(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Undoes the check out.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        public void UndoCheckOut(List<PendingChange> items)
        {
            bool oldValue = PluginBase.Settings.AutoReloadModifiedFiles;
            PluginBase.Settings.AutoReloadModifiedFiles = true;
            CurrentWorkspace.Undo(items.ToArray());
            items.ForEach(delegate(PendingChange change)
            {
                if (change.IsAdd)
                {
                    foreach (ITabbedDocument document in PluginBase.MainForm.Documents)
                    {
                        if (change.LocalItem == document.FileName)
                            document.Close();
                    }
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(change.LocalItem, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }
            });

            foreach (ITabbedDocument document in PluginBase.MainForm.Documents)
            {
                if (items.FirstOrDefault(c => c.LocalItem == document.FileName) != null)
                    document.Reload(false);
            }

            pluginUI.UpdatePendingChanges();
            Thread resetThread = new Thread(new ThreadStart(delegate()
            {
                Thread.Sleep((int)PluginBase.Settings.GetType().GetProperty("FilePollInterval").GetValue(PluginBase.Settings, null) + 250);
                PluginBase.Settings.AutoReloadModifiedFiles = oldValue;
            }));
            resetThread.IsBackground = true;
            resetThread.Name = "Reset AutoLoad";
            resetThread.Priority = ThreadPriority.Lowest;
            resetThread.Start();
        }
    }
}
