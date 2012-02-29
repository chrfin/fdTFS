using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using BrightIdeasSoftware;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using System.Threading;
using PluginCore;
using fdTFS.Sources.Tfs;

namespace fdTFS
{
    public partial class PluginUI : UserControl
    {
        /// <summary>
        /// Occurs when[checked in.
        /// </summary>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public event EventHandler CheckedIn;
        /// <summary>
        /// Raises the <see cref="E:CheckedIn"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        protected virtual void OnCheckedIn(EventArgs e)
        {
            if (CheckedIn != null)
                CheckedIn(this, e);
        }

        private bool storedQueriesLoaded = false;
        private bool updating = false;

        /// <summary>
        /// Gets or sets the main plugin.
        /// </summary>
        /// <value>The main plugin.</value>
        /// <remarks>Documented by CFI, 2010-06-26</remarks>
        public TfsManager Manager { get; set; }

        private List<PendingChange> pendingChanges;
        /// <summary>
        /// Gets or sets the pending changes.
        /// </summary>
        /// <value>The pending changes.</value>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        public List<PendingChange> PendingChanges
        {
            get { return pendingChanges; }
            set
            {
                if (pendingChanges != null && value.Count == pendingChanges.Count)
                {

                    bool changed = false;
                    foreach (PendingChange change in value)
                    {
                        PendingChange c = pendingChanges.Find(pc => pc.LocalItem == change.LocalItem);
                        if (c == null || c.IsAdd != change.IsAdd || c.IsDelete != change.IsDelete || c.IsEdit != change.IsEdit || c.IsLock != change.IsLock || c.IsMerge != change.IsMerge || c.IsRename != change.IsRename)
                        {
                            changed = true;
                            break;
                        }
                    }

                    if (!changed)
                        return;
                }

                pendingChanges = value;

                listViewSourceFiles.Invoke((MethodInvoker)delegate()
                {
                    listViewSourceFiles.BeginUpdate();
                    listViewSourceFiles.Items.Clear();
                    pendingChanges.ForEach(delegate(PendingChange change)
                    {
                        ListViewItem item = GenerateListViewItem(change);
                        listViewSourceFiles.Items.Add(item);
                    });
                    listViewSourceFiles.EndUpdate();
                });
            }
        }

        /// <summary>
        /// Gets or sets the checked in changes.
        /// </summary>
        /// <value>The checked in changes.</value>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public List<PendingChange> CheckedInChanges { get; set; }

        /// <summary>
        /// Generates the list view item.
        /// </summary>
        /// <param name="change">The change.</param>
        /// <returns></returns>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        private ListViewItem GenerateListViewItem(PendingChange change)
        {
            Icon smallIcon = Win32.GetFileIcon(change.LocalItem, true);
            Icon largeIcon = Win32.GetFileIcon(change.LocalItem, false);
            if (smallIcon != null)
                listViewSourceFiles.SmallImageList.Images.Add(smallIcon);
            if (largeIcon != null)
                listViewSourceFiles.LargeImageList.Images.Add(largeIcon);

            ListViewItem item;
            if (smallIcon != null)
                item = new ListViewItem(change.FileName, listViewSourceFiles.LargeImageList.Images.Count - 1);
            else
                item = new ListViewItem(change.FileName);
            string mode = "";
            if (change.IsAdd)
                mode += "add, ";
            if (change.IsDelete)
                mode += "delete, ";
            if (change.IsEdit)
                mode += "edit, ";
            if (change.IsLock)
                mode += "lock, ";
            if (change.IsMerge)
                mode += "merge, ";
            if (change.IsRename)
                mode += "rename, ";
            mode = mode.TrimEnd(',', ' ');
            item.SubItems.Add(mode);
            item.SubItems.Add(Path.GetDirectoryName(change.LocalItem));
            item.Checked = true;
            item.Tag = change;
            return item;
        }

        private PluginUI() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUI"/> class.
        /// </summary>
        /// <param name="main">The main plugin instance.</param>
        /// <remarks>Documented by CFI, 2010-06-26</remarks>
        public PluginUI(TfsManager manager)
        {
            Manager = manager;

            InitializeComponent();

            Enabled = false;

            listViewSourceFiles.SmallImageList = new ImageList();
            listViewSourceFiles.LargeImageList = new ImageList();

            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(radioButtonSourceFiles, "Source Files");
            toolTip.SetToolTip(radioButtonWorkItems, "Work Items");
            toolTip.SetToolTip(radioButtonCheckInNotes, "Check-in Notes");
            toolTip.SetToolTip(radioButtonPolicyWarnings, "Policy Warnings");
            toolTip.SetToolTip(radioButtonConflicts, "Conflicts");

            olvColumnCheckInAction.AspectToStringConverter = delegate(object x)
            {
                if (x is WorkItemCheckinAction)
                {
                    WorkItemCheckinAction action = (WorkItemCheckinAction)x;
                    if (action == WorkItemCheckinAction.None)
                        return "";
                    else
                        return action.ToString();
                }
                else
                    return x.ToString();
            };
            objectListViewWorkItems.BooleanCheckStatePutter = delegate(Object rowObject, bool newValue)
            {
                (rowObject as WorkItemCheckinInfo).CheckinAction = newValue ? WorkItemCheckinAction.Associate : WorkItemCheckinAction.None;
                return newValue;
            };

            olvColumnDescription.AspectGetter = delegate(object item)
            {
                if (item is PolicyFailure)
                    return (item as PolicyFailure).Message;
                else if (item is Exception)
                    return (item as Exception).Message;
                else
                    return item.ToString();
            };

            ShowSourceFiles();
        }

        /// <summary>
        /// Sets the selected files.
        /// </summary>
        /// <param name="selection">The selection.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public void SetSelectedFiles(List<string> selection)
        {
            foreach (ListViewItem item in listViewSourceFiles.Items)
                item.Checked = selection.Contains((item.Tag as PendingChange).LocalItem);
        }

        /// <summary>
        /// Handles the Click event of the toolStripButtonCheckIn control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        private void toolStripButtonCheckIn_Click(object sender, EventArgs e)
        {
            CheckIn();
        }

        /// <summary>
        /// Checks the in.
        /// </summary>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        public void CheckIn()
        {
            foreach (ITabbedDocument document in PluginBase.MainForm.Documents)
            {
                if (document.IsModified)
                    document.Save();
            }

            List<PendingChange> changes = GetSelectedChanges();

            if (pendingChanges.Count <= 0 || changes.Count <= 0)
            {
                MessageBox.Show("Nothing to Check In!", "Nothing to Check In", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            CheckinNote note = GetCurrentCheckinNotes();
            WorkItemCheckinInfo[] workItemsArray = GetSelectedWorkItems();

            PolicyOverrideInfo policyInfo = null;

            CheckinEvaluationResult result = Manager.CurrentWorkspace.EvaluateCheckin(CheckinEvaluationOptions.Conflicts, PendingChanges.ToArray(), changes.ToArray(), textBoxComment.Text, note, workItemsArray);
            if (result.Conflicts.Length > 0)
            {
                MessageBox.Show("Checkin cannot proceed because there are some conflicts.", "Conflict Failure", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowConfilcts();

                return;
            }

            result = Manager.CurrentWorkspace.EvaluateCheckin(CheckinEvaluationOptions.Notes, PendingChanges.ToArray(), changes.ToArray(), textBoxComment.Text, note, workItemsArray);
            if (result.NoteFailures.Length > 0)
            {
                MessageBox.Show("Checkin cannot proceed because there are errors in the Check-in Notes.", "Check-in Notes Failure", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ShowCheckInNotes();

                return;
            }

            result = Manager.CurrentWorkspace.EvaluateCheckin(CheckinEvaluationOptions.Policies, PendingChanges.ToArray(), changes.ToArray(), textBoxComment.Text, note, workItemsArray);
            if (result.PolicyEvaluationException != null || result.PolicyFailures.Length > 0)
            {
                string errors = string.Empty;
                foreach (PolicyFailure failure in result.PolicyFailures)
                {
                    if (failure.Message.Contains("CheckForComments.cs"))
                    {
                        if (string.IsNullOrEmpty(textBoxComment.Text))
                            errors += Environment.NewLine + " - " + "Please provide comments for your check-in.";
                    }
                    else
                        errors += Environment.NewLine + " - " + failure.Message;
                }
                if (result.PolicyEvaluationException != null)
                    errors += Environment.NewLine + " - " + result.PolicyEvaluationException.Message;

                if (errors != string.Empty)
                {
                    DialogResult boxResult = MessageBox.Show("Checkin cannot proceed because the policy requirements have not been satisfied." + Environment.NewLine + errors, "Policy Failure", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Warning);
                    if (boxResult == DialogResult.Abort)
                    {
                        ShowPolicyWarnings();
                        return;
                    }
                    else if (boxResult == DialogResult.Retry)
                    {
                        CheckIn();
                        return;
                    }
                }
            }

            Manager.CurrentWorkspace.CheckIn(changes.ToArray(), textBoxComment.Text, note, workItemsArray, policyInfo);
            CheckedInChanges = changes;

            textBoxComment.Text = string.Empty;
            textBoxCodeReviewer.Text = string.Empty;
            textBoxSecurityReviewer.Text = string.Empty;
            textBoxPerformanceReviewer.Text = string.Empty;

            foreach (ITabbedDocument document in PluginBase.MainForm.Documents)
                if (changes.FirstOrDefault(c => c.LocalItem == document.FileName) != null)
                    document.Reload(false);

            UpdatePendingChanges();

            OnCheckedIn(EventArgs.Empty);
        }

        private List<PendingChange> GetSelectedChanges()
        {
            List<PendingChange> changes = new List<PendingChange>();
            foreach (ListViewItem item in listViewSourceFiles.Items)
            {
                if (item.Checked)
                    changes.Add(item.Tag as PendingChange);
            }
            return changes;
        }

        private WorkItemCheckinInfo[] GetSelectedWorkItems()
        {
            List<WorkItemCheckinInfo> workItems = new List<WorkItemCheckinInfo>();
            TypedObjectListView<WorkItemCheckinInfo> list = new TypedObjectListView<WorkItemCheckinInfo>(objectListViewWorkItems);
            workItems.AddRange(list.CheckedObjects);
            WorkItemCheckinInfo[] workItemsArray = workItems.Count > 0 ? workItems.ToArray() : null;
            return workItemsArray;
        }

        private CheckinNote GetCurrentCheckinNotes()
        {
            List<CheckinNoteFieldValue> notes = new List<CheckinNoteFieldValue>();
            if (textBoxCodeReviewer.Text.Length > 0)
                notes.Add(new CheckinNoteFieldValue("Code Reviewer", textBoxCodeReviewer.Text));
            if (textBoxSecurityReviewer.Text.Length > 0)
                notes.Add(new CheckinNoteFieldValue("Security Reviewer", textBoxSecurityReviewer.Text));
            if (textBoxPerformanceReviewer.Text.Length > 0)
                notes.Add(new CheckinNoteFieldValue("Performance Reviewer", textBoxPerformanceReviewer.Text));
            CheckinNote note = notes.Count > 0 ? new CheckinNote(notes.ToArray()) : null;
            return note;
        }

        /// <summary>
        /// Updates the pending changes.
        /// </summary>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        public void UpdatePendingChanges() { UpdatePendingChanges(false); }
        /// <summary>
        /// Updates the pending changes.
        /// </summary>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        public void UpdatePendingChanges(bool filesOnly)
        {
            if (updating)
                return;
            updating = true;
            Thread.Sleep(250);
            
            PendingChanges = new List<PendingChange>(Manager.CurrentWorkspace.GetPendingChangesEnumerable(Manager.CurrentWorkingFolder.LocalItem, RecursionType.Full));

            if (!filesOnly)
            {
                UpdateWorkItems();
                UpdatePolicyWarnings();
                UpdateConflicts();
            }

            if (!Enabled)
                Invoke((MethodInvoker)delegate() { Enabled = true; });
            updating = false;
        }
        /// <summary>
        /// Updates the work items.
        /// </summary>
        /// <remarks>Documented by CFI, 2011-01-08</remarks>
        private void UpdateWorkItems()
        {
            Project workItemProject = Manager.CurrentWorkItemStore.Projects[Manager.CurrentTeamProject.Name];
            if (!storedQueriesLoaded)
            {
                int idToSelect = 0;
                toolStripComboBoxQuery.Items.Clear();
                foreach (QueryFolder folder in workItemProject.QueryHierarchy)
                {
                    foreach (QueryItem item in folder)
                    {
                        toolStripComboBoxQuery.Items.Add(new FdQueryItem(item));
                        if (item.Name == "My Work Items")
                            idToSelect = toolStripComboBoxQuery.Items.Count - 1;
                    }
                }
                toolStripComboBoxQuery.SelectedIndex = idToSelect;
                storedQueriesLoaded = true;
            }

            string queryString = "SELECT [System.Id], [System.WorkItemType], [System.State], [System.Title], [System.AssignedTo] FROM WorkItems WHERE [System.TeamProject] = @project AND [System.State] <> 'Closed' AND [System.State] <> 'Resolved' ORDER BY [System.WorkItemType], [System.Id]";
            QueryItem currentQuery = (toolStripComboBoxQuery.SelectedItem as FdQueryItem).Item;
            if (currentQuery != null)
                queryString = (currentQuery as QueryDefinition).QueryText;
            WorkItemCollection workItems = Manager.CurrentWorkItemStore.Query(queryString.Replace("@project", "'" + workItemProject.Name + "'"));

            List<WorkItemCheckinInfo> workItemsInfo = new List<WorkItemCheckinInfo>();
            foreach (WorkItem w in workItems)
                workItemsInfo.Add(new WorkItemCheckinInfo(w, WorkItemCheckinAction.None));

            objectListViewWorkItems.Invoke((MethodInvoker)delegate()
            {
                objectListViewWorkItems.BeginUpdate();

                objectListViewWorkItems.SetObjects(null);
                while (objectListViewWorkItems.Columns.Count > 2)
                    objectListViewWorkItems.Columns.RemoveAt(2);
                objectListViewWorkItems.AllColumns.RemoveRange(2, objectListViewWorkItems.AllColumns.Count - 2);
            });

            foreach (FieldDefinition field in workItems.Query.DisplayFieldList)
            {
                string referenceName = field.ReferenceName;
                OLVColumn column = new OLVColumn(field.Name, "");
                column.AspectGetter = delegate(object item)
                {
                    try
                    {
                        return (item as WorkItemCheckinInfo).WorkItem.Fields[referenceName].OriginalValue;
                    }
                    catch { return string.Empty; }
                };
                column.MinimumWidth = 25;
                column.FillsFreeSpace = true;
                column.FreeSpaceProportion = field.Name == "Title" ? 6 : 1;
                column.IsEditable = false;
                column.ToolTipText = field.HelpText;

                objectListViewWorkItems.Invoke((MethodInvoker)delegate()
                {
                    objectListViewWorkItems.AllColumns.Add(column);
                    objectListViewWorkItems.Columns.Add(column);
                });
            }

            objectListViewWorkItems.Invoke((MethodInvoker)delegate()
            {
                objectListViewWorkItems.SetObjects(workItemsInfo);

                objectListViewWorkItems.EndUpdate();
                objectListViewWorkItems.Width = objectListViewWorkItems.Width - 1;
            });
        }
        /// <summary>
        /// Updates the policy warnings.
        /// </summary>
        /// <remarks>Documented by CFI, 2011-01-08</remarks>
        private void UpdatePolicyWarnings()
        {
            List<object> items = new List<object>();
            List<PendingChange> changes = GetSelectedChanges();
            if (pendingChanges.Count > 0 && changes.Count > 0)
            {
                CheckinEvaluationResult result = Manager.CurrentWorkspace.EvaluateCheckin(CheckinEvaluationOptions.Policies,
                    PendingChanges.ToArray(), changes.ToArray(), textBoxComment.Text, GetCurrentCheckinNotes(), GetSelectedWorkItems());
                if (result.PolicyEvaluationException != null || result.PolicyFailures.Length > 0)
                {
                    labelPolicyWarning.Text = "The following check-in policies have not been satisfied";
                    pictureBoxWarning.Visible = true;

                    foreach (PolicyFailure failure in result.PolicyFailures)
                    {
                        if (failure.Message.Contains("CheckForComments.cs"))
                        {
                            if (string.IsNullOrEmpty(textBoxComment.Text))
                                items.Add("Please provide some comments about your check-in.");
                        }
                        else
                            items.Add(failure);
                    }
                    if (result.PolicyEvaluationException != null)
                        items.Add(result.PolicyEvaluationException);

                    objectListViewPolicyWarnings.SetObjects(items);
                }
                else
                {
                    labelPolicyWarning.Text = "All check-in policies are satisfied";
                    pictureBoxWarning.Visible = false;
                }
            }
        }
        /// <summary>
        /// Updates the conflicts.
        /// </summary>
        /// <remarks>Documented by CFI, 2011-01-08</remarks>
        private void UpdateConflicts()
        {
            objectListViewConflicts.SetObjects(null);

            if (PendingChanges.Count <= 0 || GetSelectedChanges().Count <= 0)
                return;

            CheckinEvaluationResult result = Manager.CurrentWorkspace.EvaluateCheckin(CheckinEvaluationOptions.Conflicts, PendingChanges.ToArray(), 
                GetSelectedChanges().ToArray(), textBoxComment.Text, GetCurrentCheckinNotes(), GetSelectedWorkItems());

            if (result.Conflicts.Length == 0)
                labelConflicts.Text = "No Conflicts found";
            else
            {
                labelConflicts.Text = result.Conflicts.Length < 2 ? "1 Conflict found" : result.Conflicts.Length + " Conflicts found";
                objectListViewConflicts.SetObjects(result.Conflicts);
            }
        }

        /// <summary>
        /// Shows the source files.
        /// </summary>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        public void ShowSourceFiles()
        {
            splitContainerMain.Panel2.Controls.Clear();
            splitContainerMain.Panel2.Controls.Add(splitContainerSourceFiles);

            radioButtonSourceFiles.Checked = true;
        }
        /// <summary>
        /// Shows the work items.
        /// </summary>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        public void ShowWorkItems()
        {
            splitContainerMain.Panel2.Controls.Clear();
            splitContainerMain.Panel2.Controls.Add(panelWorkItems);

            radioButtonWorkItems.Checked = true;
        }
        /// <summary>
        /// Shows the check in notes.
        /// </summary>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        public void ShowCheckInNotes()
        {
            splitContainerMain.Panel2.Controls.Clear();
            splitContainerMain.Panel2.Controls.Add(panelCheckInNotes);

            radioButtonCheckInNotes.Checked = true;
        }
        /// <summary>
        /// Shows the policy warnings.
        /// </summary>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        public void ShowPolicyWarnings()
        {
            splitContainerMain.Panel2.Controls.Clear();
            splitContainerMain.Panel2.Controls.Add(panelPolicyWarnings);

            radioButtonPolicyWarnings.Checked = true;
        }
        /// <summary>
        /// Shows the confilcts.
        /// </summary>
        /// <remarks>Documented by CFI, 2010-06-27</remarks>
        public void ShowConfilcts()
        {
            splitContainerMain.Panel2.Controls.Clear();
            splitContainerMain.Panel2.Controls.Add(panelConflicts);

            radioButtonConflicts.Checked = true;
        }

        /// <summary>
        /// Handles the CheckedChanged event of the radioButtonSourceFiles control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-30</remarks>
        private void radioButtonSourceFiles_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonSourceFiles.Checked)
                ShowSourceFiles();
        }
        /// <summary>
        /// Handles the CheckedChanged event of the radioButtonWorkItems control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-30</remarks>
        private void radioButtonWorkItems_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonWorkItems.Checked)
                ShowWorkItems();
        }
        /// <summary>
        /// Handles the CheckedChanged event of the radioButtonCheckInNotes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-30</remarks>
        private void radioButtonCheckInNotes_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonCheckInNotes.Checked)
                ShowCheckInNotes();
        }
        /// <summary>
        /// Handles the CheckedChanged event of the radioButtonPolicyWarnings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-30</remarks>
        private void radioButtonPolicyWarnings_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonPolicyWarnings.Checked)
                ShowPolicyWarnings();
        }
        /// <summary>
        /// Handles the CheckedChanged event of the radioButtonConflicts control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-06-30</remarks>
        private void radioButtonConflicts_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonConflicts.Checked)
                ShowConfilcts();
        }

        /// <summary>
        /// Handles the CellEditStarting event of the objectListViewWorkItems control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="BrightIdeasSoftware.CellEditEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-01</remarks>
        private void objectListViewWorkItems_CellEditStarting(object sender, CellEditEventArgs e)
        {
            if (e.Value is WorkItemCheckinAction && !e.ListViewItem.Checked)
                e.Cancel = true;
        }

        /// <summary>
        /// Handles the Click event of the toolStripButtonWorkItemsRefresh control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-01</remarks>
        private void toolStripButtonWorkItemsRefresh_Click(object sender, EventArgs e) { UpdateWorkItems(); }

        /// <summary>
        /// Handles the Click event of the undoToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UndoPendingChangesForm undoForm = new UndoPendingChangesForm();
            undoForm.PendingChanges = PendingChanges;
            foreach (ListViewItem item in listViewSourceFiles.SelectedItems)
                undoForm.PreCheckPendingChange(item.Tag as PendingChange);
            if (undoForm.ShowDialog() == DialogResult.OK)
                Manager.UndoCheckOut(undoForm.SelectedPendingChanges);
        }

        /// <summary>
        /// Handles the Click event of the compareToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        private void compareToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewSourceFiles.SelectedItems.Count > 0)
                Manager.Compare((listViewSourceFiles.SelectedItems[0].Tag as PendingChange).LocalItem);
        }

        /// <summary>
        /// Handles the Click event of the historyToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        private void historyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewSourceFiles.SelectedItems.Count > 0)
                Manager.ViewHistory((listViewSourceFiles.SelectedItems[0].Tag as PendingChange).LocalItem);
        }

        /// <summary>
        /// Handles the Opening event of the contextMenuStripsourceFiles control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        private void contextMenuStripsourceFiles_Opening(object sender, CancelEventArgs e)
        {
            if (listViewSourceFiles.SelectedItems.Count == 0)
                e.Cancel = true;
        }

        /// <summary>
        /// Handles the MouseUp event of the listViewSourceFiles control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        private void listViewSourceFiles_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                ListViewItem item = listViewSourceFiles.GetItemAt(e.X, e.Y);
                if (item == null || !item.Selected)
                    listViewSourceFiles.SelectedItems.Clear();
                if (item != null)
                    item.Selected = true;
            }
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the toolStripComboBoxQuery control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-04</remarks>
        private void toolStripComboBoxQuery_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (storedQueriesLoaded)
                UpdatePendingChanges();
        }

        /// <summary>
        /// Handles the DoubleClick event of the listViewPolicyWarnings control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-08</remarks>
        private void listViewPolicyWarnings_DoubleClick(object sender, EventArgs e)
        {
            if (objectListViewPolicyWarnings.SelectedIndex >= 0)
            {
                object item = objectListViewPolicyWarnings.Items[objectListViewPolicyWarnings.SelectedIndex];
                string message;
                if (item is PolicyFailure)
                    message = (item as PolicyFailure).Message;
                else if (item is Exception)
                    message = (item as Exception).Message;
                else if (item is ListViewItem)
                    message = (item as ListViewItem).Text;
                else
                    message = item.ToString();
                MessageBox.Show(message, "How to fix your policy failure", MessageBoxButtons.OK);
            }
        }

        /// <summary>
        /// Handles the Click event of the evaluateToolStripMenuItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-08</remarks>
        private void evaluateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdatePolicyWarnings();
        }

        /// <summary>
        /// Handles the Click event of the toolStripButtonRefreshConflicts control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-08</remarks>
        private void toolStripButtonRefreshConflicts_Click(object sender, EventArgs e)
        {
            UpdateConflicts();
        }
    }
}
