namespace GTNSimulator
{
    partial class FTrackClosure
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FTrackClosure));
            this.tlpMain = new System.Windows.Forms.TableLayoutPanel();
            this.lTrack = new System.Windows.Forms.Label();
            this.cbTrack = new ExControls.ExComboBox();
            this.lFrom = new System.Windows.Forms.Label();
            this.dtpFrom = new ExControls.ExDateTimePicker();
            this.lTo = new System.Windows.Forms.Label();
            this.dtpTo = new ExControls.ExDateTimePicker();
            this.lAffected = new System.Windows.Forms.Label();
            this.lHint = new System.Windows.Forms.Label();
            this.flowButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.bCancel = new ExControls.ExButton();
            this.bOk = new ExControls.ExButton();
            this.tlpMain.SuspendLayout();
            this.flowButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlpMain
            // 
            this.tlpMain.Controls.Add(this.lTrack);
            this.tlpMain.Controls.Add(this.cbTrack);
            this.tlpMain.Controls.Add(this.lFrom);
            this.tlpMain.Controls.Add(this.dtpFrom);
            this.tlpMain.Controls.Add(this.lTo);
            this.tlpMain.Controls.Add(this.dtpTo);
            this.tlpMain.Controls.Add(this.lAffected);
            this.tlpMain.Controls.Add(this.lHint);
            resources.ApplyResources(this.tlpMain, "tlpMain");
            this.tlpMain.ColumnCount = 3;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.RowCount = 5;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpMain.Name = "tlpMain";
            // 
            // lTrack
            // 
            resources.ApplyResources(this.lTrack, "lTrack");
            this.tlpMain.SetCellPosition(this.lTrack, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 0));
            this.lTrack.Name = "lTrack";
            // 
            // cbTrack
            // 
            resources.ApplyResources(this.cbTrack, "cbTrack");
            this.tlpMain.SetCellPosition(this.cbTrack, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 0));
            this.tlpMain.SetColumnSpan(this.cbTrack, 2);
            this.cbTrack.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbTrack.SelectedIndexChanged += new System.EventHandler(this.Range_Changed);
            this.cbTrack.Name = "cbTrack";
            // 
            // lFrom
            // 
            resources.ApplyResources(this.lFrom, "lFrom");
            this.tlpMain.SetCellPosition(this.lFrom, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 1));
            this.lFrom.Name = "lFrom";
            // 
            // dtpFrom
            // 
            resources.ApplyResources(this.dtpFrom, "dtpFrom");
            this.tlpMain.SetCellPosition(this.dtpFrom, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 1));
            this.dtpFrom.CustomFormat = "HH:mm";
            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpFrom.ShowUpDown = true;
            this.dtpFrom.ValueChanged += new System.EventHandler(this.Range_Changed);
            this.dtpFrom.Name = "dtpFrom";
            // 
            // lTo
            // 
            resources.ApplyResources(this.lTo, "lTo");
            this.tlpMain.SetCellPosition(this.lTo, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 2));
            this.lTo.Name = "lTo";
            // 
            // dtpTo
            // 
            resources.ApplyResources(this.dtpTo, "dtpTo");
            this.tlpMain.SetCellPosition(this.dtpTo, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 2));
            this.dtpTo.CustomFormat = "HH:mm";
            this.dtpTo.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpTo.ShowUpDown = true;
            this.dtpTo.ValueChanged += new System.EventHandler(this.Range_Changed);
            this.dtpTo.Name = "dtpTo";
            // 
            // lAffected
            // 
            resources.ApplyResources(this.lAffected, "lAffected");
            this.tlpMain.SetCellPosition(this.lAffected, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 3));
            this.tlpMain.SetColumnSpan(this.lAffected, 3);
            this.lAffected.Name = "lAffected";
            // 
            // lHint
            // 
            resources.ApplyResources(this.lHint, "lHint");
            this.tlpMain.SetCellPosition(this.lHint, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 4));
            this.tlpMain.SetColumnSpan(this.lHint, 3);
            this.lHint.Name = "lHint";
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
            // FTrackClosure
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            resources.ApplyResources(this, "$this");
            this.Controls.Add(this.tlpMain);
            this.Controls.Add(this.flowButtons);
            this.AcceptButton = this.bOk;
            this.CancelButton = this.bCancel;
            this.Name = "FTrackClosure";
            this.flowButtons.ResumeLayout(false);
            this.flowButtons.PerformLayout();
            this.tlpMain.ResumeLayout(false);
            this.tlpMain.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlpMain;
        private System.Windows.Forms.Label lTrack;
        private ExControls.ExComboBox cbTrack;
        private System.Windows.Forms.Label lFrom;
        private ExControls.ExDateTimePicker dtpFrom;
        private System.Windows.Forms.Label lTo;
        private ExControls.ExDateTimePicker dtpTo;
        private System.Windows.Forms.Label lAffected;
        private System.Windows.Forms.Label lHint;
        private System.Windows.Forms.FlowLayoutPanel flowButtons;
        private ExControls.ExButton bCancel;
        private ExControls.ExButton bOk;
    }
}
