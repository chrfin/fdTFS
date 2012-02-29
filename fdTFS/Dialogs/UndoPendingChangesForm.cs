using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.IO;

namespace fdTFS
{
    public partial class UndoPendingChangesForm : Form
    {
        /// <summary>
        /// Sets the pending changes.
        /// </summary>
        /// <value>The pending changes.</value>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        public List<PendingChange> PendingChanges
        {
            set
            {
                listViewFiles.Items.Clear();
                value.ForEach(c => listViewFiles.Items.Add(GenerateListViewItem(c)));
            }
        }

        /// <summary>
        /// Gets the selected pending changes.
        /// </summary>
        /// <value>The selected pending changes.</value>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        public List<PendingChange> SelectedPendingChanges
        {
            get
            {
                List<PendingChange> changes = new List<PendingChange>();
                foreach (ListViewItem item in listViewFiles.CheckedItems)
                    changes.Add(item.Tag as PendingChange);
                return changes;
            }
        }

        public UndoPendingChangesForm()
        {
            InitializeComponent();

            listViewFiles.SmallImageList = new ImageList();
            listViewFiles.LargeImageList = new ImageList();
        }

        /// <summary>
        /// Handles the Click event of the buttonUndo control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        private void buttonUndo_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Handles the Click event of the buttonCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Pres the check a pending change.
        /// </summary>
        /// <param name="change">The change.</param>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        public void PreCheckPendingChange(PendingChange change)
        {
            ListViewItem item = listViewFiles.Items.Cast<ListViewItem>().FirstOrDefault(i => i.Tag == change);
            if (item != null)
                item.Checked = true;
        }

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
                listViewFiles.SmallImageList.Images.Add(smallIcon);
            if (largeIcon != null)
                listViewFiles.LargeImageList.Images.Add(largeIcon);

            ListViewItem item;
            if (smallIcon != null)
                item = new ListViewItem(change.FileName, listViewFiles.LargeImageList.Images.Count - 1);
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
            item.Tag = change;
            return item;
        }

        /// <summary>
        /// Handles the Load event of the UndoPendingChangesForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-16</remarks>
        private void UndoPendingChangesForm_Load(object sender, EventArgs e)
        {
            BringToFront();
            Activate();
        }

        /// <summary>
        /// Handles the Shown event of the UndoPendingChangesForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-16</remarks>
        private void UndoPendingChangesForm_Shown(object sender, EventArgs e)
        {
            buttonCancel.Focus();
        }
    }
}
