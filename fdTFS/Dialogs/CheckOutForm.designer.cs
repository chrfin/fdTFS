namespace fdTFS
{
    partial class CheckOutForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelFiles = new System.Windows.Forms.Label();
            this.labelLockType = new System.Windows.Forms.Label();
            this.labelCheckOut = new System.Windows.Forms.Label();
            this.listViewFiles = new System.Windows.Forms.ListView();
            this.columnHeaderName = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeaderFolder = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.comboBoxLock = new System.Windows.Forms.ComboBox();
            this.buttonCheckOut = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // labelFiles
            // 
            this.labelFiles.AutoSize = true;
            this.labelFiles.Location = new System.Drawing.Point(12, 9);
            this.labelFiles.Name = "labelFiles";
            this.labelFiles.Size = new System.Drawing.Size(31, 13);
            this.labelFiles.TabIndex = 0;
            this.labelFiles.Text = "Files:";
            // 
            // labelLockType
            // 
            this.labelLockType.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelLockType.AutoSize = true;
            this.labelLockType.Location = new System.Drawing.Point(12, 261);
            this.labelLockType.Name = "labelLockType";
            this.labelLockType.Size = new System.Drawing.Size(61, 13);
            this.labelLockType.TabIndex = 1;
            this.labelLockType.Text = "Lock Type:";
            // 
            // labelCheckOut
            // 
            this.labelCheckOut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelCheckOut.AutoSize = true;
            this.labelCheckOut.Location = new System.Drawing.Point(12, 291);
            this.labelCheckOut.Name = "labelCheckOut";
            this.labelCheckOut.Size = new System.Drawing.Size(213, 13);
            this.labelCheckOut.TabIndex = 2;
            this.labelCheckOut.Text = "Check Out Local Items from Source control.";
            // 
            // listViewFiles
            // 
            this.listViewFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewFiles.CheckBoxes = true;
            this.listViewFiles.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName,
            this.columnHeaderFolder});
            this.listViewFiles.Location = new System.Drawing.Point(15, 30);
            this.listViewFiles.Name = "listViewFiles";
            this.listViewFiles.Size = new System.Drawing.Size(557, 212);
            this.listViewFiles.TabIndex = 3;
            this.listViewFiles.UseCompatibleStateImageBehavior = false;
            this.listViewFiles.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderName
            // 
            this.columnHeaderName.Text = "Name";
            this.columnHeaderName.Width = 150;
            // 
            // columnHeaderFolder
            // 
            this.columnHeaderFolder.Text = "Folder";
            this.columnHeaderFolder.Width = 375;
            // 
            // comboBoxLock
            // 
            this.comboBoxLock.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxLock.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxLock.FormattingEnabled = true;
            this.comboBoxLock.Items.AddRange(new object[] {
            "Unchanged - Keep any exsisting lock",
            "None - Allow shared checkout",
            "Check Out - Prevent other users form checking out and checking in",
            "Check In - Allow other users to check out but prevent them from checking in"});
            this.comboBoxLock.Location = new System.Drawing.Point(79, 258);
            this.comboBoxLock.Name = "comboBoxLock";
            this.comboBoxLock.Size = new System.Drawing.Size(493, 21);
            this.comboBoxLock.TabIndex = 4;
            // 
            // buttonCheckOut
            // 
            this.buttonCheckOut.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCheckOut.Location = new System.Drawing.Point(374, 307);
            this.buttonCheckOut.Name = "buttonCheckOut";
            this.buttonCheckOut.Size = new System.Drawing.Size(96, 23);
            this.buttonCheckOut.TabIndex = 5;
            this.buttonCheckOut.Text = "&Check Out";
            this.buttonCheckOut.UseVisualStyleBackColor = true;
            this.buttonCheckOut.Click += new System.EventHandler(this.buttonCheckOut_Click);
            // 
            // buttonCancel
            // 
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.Location = new System.Drawing.Point(476, 307);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(96, 23);
            this.buttonCancel.TabIndex = 6;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            // 
            // CheckOutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(584, 342);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonCheckOut);
            this.Controls.Add(this.comboBoxLock);
            this.Controls.Add(this.listViewFiles);
            this.Controls.Add(this.labelCheckOut);
            this.Controls.Add(this.labelLockType);
            this.Controls.Add(this.labelFiles);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(400, 300);
            this.Name = "CheckOutForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Check Out";
            this.Load += new System.EventHandler(this.CheckOutForm_Load);
            this.Shown += new System.EventHandler(this.CheckOutForm_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelFiles;
        private System.Windows.Forms.Label labelLockType;
        private System.Windows.Forms.Label labelCheckOut;
        private System.Windows.Forms.ListView listViewFiles;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.ColumnHeader columnHeaderFolder;
        private System.Windows.Forms.ComboBox comboBoxLock;
        private System.Windows.Forms.Button buttonCheckOut;
        private System.Windows.Forms.Button buttonCancel;
    }
}