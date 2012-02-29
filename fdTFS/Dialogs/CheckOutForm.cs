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
    public partial class CheckOutForm : Form
    {
        /// <summary>
        /// Gets the lock level.
        /// </summary>
        /// <value>The lock level.</value>
        /// <remarks>Documented by CFI, 2010-07-13</remarks>
        public LockLevel LockLevel
        {
            get
            {
                switch (comboBoxLock.SelectedIndex)
                {
                    case 0:
                        return LockLevel.Unchanged;
                    case 1:
                        return LockLevel.None;
                    case 2:
                        return LockLevel.CheckOut;
                    case 3:
                        return LockLevel.Checkin;
                }

                return LockLevel.None;
            }
            set
            {
                switch (value)
                {
                    case LockLevel.Unchanged:
                        comboBoxLock.SelectedIndex = 0;
                        break;
                    case LockLevel.None:
                        comboBoxLock.SelectedIndex = 1;
                        break;
                    case LockLevel.CheckOut:
                        comboBoxLock.SelectedIndex = 2;
                        break;
                    case LockLevel.Checkin:
                        comboBoxLock.SelectedIndex = 3;
                        break;
                }
            }
        }

        /// <summary>
        /// Sets the files to check out.
        /// </summary>
        /// <value>The files to check out.</value>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        public List<string> FilesToCheckOut
        {
            set
            {
                listViewFiles.Items.Clear();
                value.ForEach(delegate(string file)
                {
                    Icon smallIcon = Win32.GetFileIcon(file, true);
                    Icon largeIcon = Win32.GetFileIcon(file, false);
                    if (smallIcon != null)
                        listViewFiles.SmallImageList.Images.Add(smallIcon);
                    if (largeIcon != null)
                        listViewFiles.LargeImageList.Images.Add(largeIcon);

                    ListViewItem item;
                    if (smallIcon != null)
                        item = new ListViewItem(Path.GetFileName(file), listViewFiles.LargeImageList.Images.Count - 1);
                    else
                        item = new ListViewItem(Path.GetFileName(file));
                    item.SubItems.Add(Path.GetDirectoryName(file));
                    item.Checked = true;
                    item.Tag = file;
                    item.Checked = true;
                    listViewFiles.Items.Add(item);
                });
                comboBoxLock.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Gets the selected files to check out.
        /// </summary>
        /// <value>The selected files to check out.</value>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        public List<string> SelectedFilesToCheckOut
        {
            get
            {
                List<string> items = new List<string>();
                foreach (ListViewItem item in listViewFiles.CheckedItems)
                    items.Add(item.Tag as string);
                return items;
            }
        }

        public CheckOutForm()
        {
            InitializeComponent();

            listViewFiles.SmallImageList = new ImageList();
            listViewFiles.LargeImageList = new ImageList();
        }

        /// <summary>
        /// Handles the Click event of the buttonCheckOut control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-13</remarks>
        private void buttonCheckOut_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Handles the Click event of the buttonCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-13</remarks>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;
            Close();
        }

        /// <summary>
        /// Handles the Load event of the CheckOutForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-16</remarks>
        private void CheckOutForm_Load(object sender, EventArgs e)
        {
            BringToFront();
            Activate();
        }

        /// <summary>
        /// Handles the Shown event of the CheckOutForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2010-07-16</remarks>
        private void CheckOutForm_Shown(object sender, EventArgs e)
        {
            buttonCheckOut.Focus();
        }
    }
}
