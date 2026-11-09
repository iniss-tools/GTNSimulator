namespace GTNSimulator
{
    partial class FTracks
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FTracks));
            this.lHint = new System.Windows.Forms.Label();
            this.clbTracks = new ExControls.ExCheckedListBox();
            this.flowSelect = new System.Windows.Forms.FlowLayoutPanel();
            this.bAll = new ExControls.ExButton();
            this.bNone = new ExControls.ExButton();
            this.lCount = new System.Windows.Forms.Label();
            this.flowButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.bCancel = new ExControls.ExButton();
            this.bOk = new ExControls.ExButton();
            this.flowSelect.SuspendLayout();
            this.flowButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // lHint
            // 
            resources.ApplyResources(this.lHint, "lHint");
            this.lHint.Name = "lHint";
            // 
            // clbTracks
            // 
            resources.ApplyResources(this.clbTracks, "clbTracks");
            this.clbTracks.CheckOnClick = true;
            this.clbTracks.IntegralHeight = false;
            this.clbTracks.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbTracks_ItemCheck);
            this.clbTracks.Name = "clbTracks";
            // 
            // flowSelect
            // 
            this.flowSelect.Controls.Add(this.bAll);
            this.flowSelect.Controls.Add(this.bNone);
            this.flowSelect.Controls.Add(this.lCount);
            resources.ApplyResources(this.flowSelect, "flowSelect");
            this.flowSelect.Name = "flowSelect";
            // 
            // bAll
            // 
            resources.ApplyResources(this.bAll, "bAll");
            this.bAll.UseVisualStyleBackColor = true;
            this.bAll.Click += new System.EventHandler(this.bAll_Click);
            this.bAll.Name = "bAll";
            // 
            // bNone
            // 
            resources.ApplyResources(this.bNone, "bNone");
            this.bNone.UseVisualStyleBackColor = true;
            this.bNone.Click += new System.EventHandler(this.bNone_Click);
            this.bNone.Name = "bNone";
            // 
            // lCount
            // 
            resources.ApplyResources(this.lCount, "lCount");
            this.lCount.Name = "lCount";
            // 
            // flowButtons
            // 
            this.flowButtons.Controls.Add(this.bCancel);
            this.flowButtons.Controls.Add(this.bOk);
            resources.ApplyResources(this.flowButtons, "flowButtons");
            this.flowButtons.Name = "flowButtons";
            // 
            // bCancel
            // 
            resources.ApplyResources(this.bCancel, "bCancel");
            this.bCancel.UseVisualStyleBackColor = true;
            this.bCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.bCancel.Name = "bCancel";
            // 
            // bOk
            // 
            resources.ApplyResources(this.bOk, "bOk");
            this.bOk.UseVisualStyleBackColor = true;
            this.bOk.Click += new System.EventHandler(this.bOk_Click);
            this.bOk.Name = "bOk";
            // 
            // FTracks
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.clbTracks);
            this.Controls.Add(this.flowSelect);
            this.Controls.Add(this.flowButtons);
            this.Controls.Add(this.lHint);
            this.AcceptButton = this.bOk;
            this.CancelButton = this.bCancel;
            this.Name = "FTracks";
            this.flowButtons.ResumeLayout(false);
            this.flowButtons.PerformLayout();
            this.flowSelect.ResumeLayout(false);
            this.flowSelect.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lHint;
        private ExControls.ExCheckedListBox clbTracks;
        private System.Windows.Forms.FlowLayoutPanel flowSelect;
        private ExControls.ExButton bAll;
        private ExControls.ExButton bNone;
        private System.Windows.Forms.Label lCount;
        private System.Windows.Forms.FlowLayoutPanel flowButtons;
        private ExControls.ExButton bCancel;
        private ExControls.ExButton bOk;
    }
}
