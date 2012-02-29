using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace fdTFS
{
    /// <summary>
    /// Settings for the plugin
    /// </summary>
    /// <remarks>Documented by CFI, 2011-01-08</remarks>
    [Serializable]
    public class Settings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Settings"/> class.
        /// </summary>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        public Settings()
        {
            DefaultLockLevel = LockLevel.None;
            CheckedInCharacter = "◊";
            PromptForLockLevel = true;
            PopupOnNonFatalException = true;
            PopupOnNonFatalFailure = true;
        }

        /// <summary>
        /// Gets or sets the default lock level.
        /// </summary>
        /// <value>The default lock level.</value>
        /// <remarks>Documented by CFI, 2010-07-14</remarks>
        [Description("The Lock Level which will be preselected in the Check Out Dialog."), DefaultValue(LockLevel.None)]
        public LockLevel DefaultLockLevel { get; set; }

        /// <summary>
        /// Gets or sets the checked in character.
        /// </summary>
        /// <value>The checked in character.</value>
        /// <remarks>Documented by CFI, 2011-01-05</remarks>
        [Description("The character which is appendet to checked in files in the Tab header."), DefaultValue("◊")]
        public string CheckedInCharacter { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to prompt for lock level.
        /// </summary>
        /// <value>
        ///   <c>true</c> if prompt for lock level; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Documented by ChrFin00, 2011-07-25</remarks>
        [Description("If this is set to true, the will be a Check Out Dialog where you " +
            "can change the lock level before a file is checked out."), DefaultValue(true)]
        public bool PromptForLockLevel { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether a popup should apear on non fatal exceptions.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if a popup should apear on non fatal exceptions; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Documented by ChrFin00, 2011-07-25</remarks>
        [Description("If this is set to true, there will be a Popup when a non fatal " +
            "exception occures."), DefaultValue(true)]
        public bool PopupOnNonFatalException { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether a popup should apear on non fatal failures.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if a popup should apear on non fatal failures; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>Documented by ChrFin00, 2011-07-25</remarks>
        [Description("If this is set to true, there will be a Popup when a non fatal " +
            "failure occures (e.g. file is already checked out by someone else)."), DefaultValue(true)]
        public bool PopupOnNonFatalFailure { get; set; }
    }
}
