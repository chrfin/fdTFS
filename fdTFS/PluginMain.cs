using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using fdTFS.Properties;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using PluginCore;
using PluginCore.Helpers;
using PluginCore.Managers;
using PluginCore.Utilities;
using ProjectManager;
using ProjectManager.Projects;
using SourceControl.Actions;
using SourceControl.Sources;
using WeifenLuo.WinFormsUI.Docking;

namespace fdTFS
{
    public class PluginMain : IPlugin
    {
        private string settingsFilename;
        private TfsManager tfsManager;

        #region IPlugin Properties

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        /// <remarks>Documented by CFI, 2010-06-26</remarks>
        public string Name { get { return "TFS Plugin"; } }

        /// <summary>
        /// Gets the API version.
        /// </summary>
        /// <remarks>Documented by ChrFin00, 2011-07-13</remarks>
        public int Api
        {
            get { return 1; }
        }

        /// <summary>
        /// Gets the GUID.
        /// </summary>
        /// <value>The GUID.</value>
        /// <remarks>Documented by CFI, 2010-06-26</remarks>
        public string Guid { get { return "15abfe9c-b048-44f4-815e-1a2e555f24bf"; } }

        /// <summary>
        /// Gets the help.
        /// </summary>
        /// <value>The help.</value>
        /// <remarks>Documented by CFI, 2010-06-26</remarks>
        public string Help { get { return "www.flashdevelop.org/community/"; } }

        /// <summary>
        /// Gets the author.
        /// </summary>
        /// <value>The author.</value>
        /// <remarks>Documented by CFI, 2010-06-26</remarks>
        public string Author { get { return "Fink Christoph - OMICRON electronics GmbH"; } }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        /// <remarks>Documented by CFI, 2010-06-26</remarks>
        public string Description { get { return "Plugin to check out files from TFS before editing them."; } }

        /// <summary>
        /// Gets the settings.
        /// </summary>
        /// <value>The settings.</value>
        /// <remarks>Documented by CFI, 2010-06-26</remarks>
        public object Settings { get; set; }

        #endregion

        #region IPlugin Methods

        /// <summary>
        /// Initializes this plugin.
        /// </summary>
        /// <remarks>Documented by CFI, 2010-06-26</remarks>
        public void Initialize()
        {
            LoadSettings();

            ProjectWatcher.Skin = Resources.icons;
            tfsManager = new TfsManager(this);
            Thread setVC = new Thread(new ThreadStart(delegate
            {
                while (!ProjectWatcher.Initialized)
                    Thread.Sleep(100);
                ProjectWatcher.VcManager.AddVCManager(tfsManager);
            }));
            setVC.IsBackground = true;
            setVC.Name = "Set TFS VC";
            setVC.Priority = ThreadPriority.BelowNormal;
            setVC.Start();
        }

        public void HandleEvent(object sender, NotifyEvent e, HandlingPriority priority) { }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <remarks>Documented by CFI, 2010-06-26</remarks>
        public void Dispose()
        {
            SaveSettings();
        }

        #endregion

        /// <summary>
        /// Loads the plugin settings
        /// </summary>
        public void LoadSettings()
        {
            string dataPath = Path.Combine(PathHelper.DataDir, "fdTFS");
            if (!Directory.Exists(dataPath)) Directory.CreateDirectory(dataPath);
            settingsFilename = Path.Combine(dataPath, "Settings.fdb");

            Settings = new Settings();
            if (!File.Exists(settingsFilename)) this.SaveSettings();
            else
            {
                object obj = ObjectSerializer.Deserialize(this.settingsFilename, Settings);
                Settings = (Settings)obj;
            }
        }

        /// <summary>
        /// Saves the plugin settings
        /// </summary>
        public void SaveSettings()
        {
            ObjectSerializer.Serialize(settingsFilename, Settings);
        }
    }
}