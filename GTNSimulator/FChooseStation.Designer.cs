namespace GTNSimulator
{
    partial class FChooseStation
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FChooseStation));
            this.lHint = new System.Windows.Forms.Label();
            this.lbStations = new ExControls.ExListBox();
            this.flowButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.bCancel = new ExControls.ExButton();
            this.bOk = new ExControls.ExButton();
            this.flowButtons.SuspendLayout();
            this.SuspendLayout();
            //
            // lHint
            //
            resources.ApplyResources(this.lHint, "lHint");
            this.lHint.Name = "lHint";
            //
            // lbStations
            //
            resources.ApplyResources(this.lbStations, "lbStations");
            this.lbStations.IntegralHeight = false;
            this.lbStations.DoubleClick += new System.EventHandler(this.lbStations_DoubleClick);
            this.lbStations.Name = "lbStations";
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
            this.bOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.bOk.Name = "bOk";
            //
            // FChooseStation
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.lbStations);
            this.Controls.Add(this.flowButtons);
            this.Controls.Add(this.lHint);
            this.AcceptButton = this.bOk;
            this.CancelButton = this.bCancel;
            this.Name = "FChooseStation";
            this.flowButtons.ResumeLayout(false);
            this.flowButtons.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lHint;
        private ExControls.ExListBox lbStations;
        private System.Windows.Forms.FlowLayoutPanel flowButtons;
        private ExControls.ExButton bCancel;
        private ExControls.ExButton bOk;
    }
}
