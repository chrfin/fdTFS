using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace fdTFS.Dialogs
{
    public partial class CheckInForm : Form
    {
        private PluginUI pluginUI;
        private List<string> preselection;

        /// <summary>
        /// Gets the checked in changes.
        /// </summary>
        /// <value>The checked in changes.</value>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public List<PendingChange> CheckedInChanges { get { return pluginUI.CheckedInChanges; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInForm"/> class.
        /// </summary>
        /// <param name="manager">The manager.</param>
        /// <param name="preselection">The preselection.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        public CheckInForm(TfsManager manager, List<string> preselection)
        {
            InitializeComponent();

            pluginUI = new PluginUI(manager);

            pluginUI.Dock = System.Windows.Forms.DockStyle.Fill;
            pluginUI.CheckedIn += new EventHandler(pluginUI_CheckedIn);
            pluginUI.Name = "pluginUI";
            pluginUI.TabIndex = 0;
            Controls.Add(this.pluginUI);

            this.preselection = preselection;
        }

        /// <summary>
        /// Handles the CheckedIn event of the pluginUI control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        void pluginUI_CheckedIn(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// Handles the Shown event of the CheckInForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        /// <remarks>Documented by CFI, 2011-01-07</remarks>
        private void CheckInForm_Shown(object sender, EventArgs e)
        {
            pluginUI.UpdatePendingChanges();
            pluginUI.SetSelectedFiles(preselection);
        }
    }
}
