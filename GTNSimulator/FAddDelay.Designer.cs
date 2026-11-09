namespace GTNSimulator
{
    partial class FAddDelay
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FAddDelay));
            this.tlpMain = new System.Windows.Forms.TableLayoutPanel();
            this.lMinutes = new System.Windows.Forms.Label();
            this.nudMinutes = new ExControls.ExNumericUpDown();
            this.rbSelected = new ExControls.ExRadioButton();
            this.rbFrom = new ExControls.ExRadioButton();
            this.dtpFrom = new ExControls.ExDateTimePicker();
            this.lHint = new System.Windows.Forms.Label();
            this.flowButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.bCancel = new ExControls.ExButton();
            this.bOk = new ExControls.ExButton();
            this.tlpMain.SuspendLayout();
            this.flowButtons.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudMinutes)).BeginInit();
            this.SuspendLayout();
            // 
            // tlpMain
            // 
            this.tlpMain.Controls.Add(this.lMinutes);
            this.tlpMain.Controls.Add(this.nudMinutes);
            this.tlpMain.Controls.Add(this.rbSelected);
            this.tlpMain.Controls.Add(this.rbFrom);
            this.tlpMain.Controls.Add(this.dtpFrom);
            this.tlpMain.Controls.Add(this.lHint);
            resources.ApplyResources(this.tlpMain, "tlpMain");
            this.tlpMain.ColumnCount = 3;
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpMain.RowCount = 4;
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlpMain.Name = "tlpMain";
            // 
            // lMinutes
            // 
            resources.ApplyResources(this.lMinutes, "lMinutes");
            this.tlpMain.SetCellPosition(this.lMinutes, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 0));
            this.lMinutes.Name = "lMinutes";
            // 
            // nudMinutes
            // 
            resources.ApplyResources(this.nudMinutes, "nudMinutes");
            this.tlpMain.SetCellPosition(this.nudMinutes, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 0));
            this.nudMinutes.Maximum = new decimal(new int[] { 600, 0, 0, 0 });
            this.nudMinutes.Minimum = new decimal(new int[] { 60, 0, 0, -2147483648 });
            this.nudMinutes.Value = new decimal(new int[] { 5, 0, 0, 0 });
            this.nudMinutes.Name = "nudMinutes";
            // 
            // rbSelected
            // 
            resources.ApplyResources(this.rbSelected, "rbSelected");
            this.tlpMain.SetCellPosition(this.rbSelected, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 1));
            this.tlpMain.SetColumnSpan(this.rbSelected, 3);
            this.rbSelected.Checked = true;
            this.rbSelected.TabStop = true;
            this.rbSelected.Name = "rbSelected";
            // 
            // rbFrom
            // 
            resources.ApplyResources(this.rbFrom, "rbFrom");
            this.tlpMain.SetCellPosition(this.rbFrom, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 2));
            this.rbFrom.CheckedChanged += new System.EventHandler(this.rbFrom_CheckedChanged);
            this.rbFrom.Name = "rbFrom";
            // 
            // dtpFrom
            // 
            resources.ApplyResources(this.dtpFrom, "dtpFrom");
            this.tlpMain.SetCellPosition(this.dtpFrom, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 2));
            this.dtpFrom.CustomFormat = "HH:mm";
            this.dtpFrom.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtpFrom.ShowUpDown = true;
            this.dtpFrom.Name = "dtpFrom";
            // 
            // lHint
            // 
            resources.ApplyResources(this.lHint, "lHint");
            this.tlpMain.SetCellPosition(this.lHint, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 3));
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
            // FAddDelay
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
            this.Name = "FAddDelay";
            this.flowButtons.ResumeLayout(false);
            this.flowButtons.PerformLayout();
            this.tlpMain.ResumeLayout(false);
            this.tlpMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudMinutes)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tlpMain;
        private System.Windows.Forms.Label lMinutes;
        private ExControls.ExNumericUpDown nudMinutes;
        private ExControls.ExRadioButton rbSelected;
        private ExControls.ExRadioButton rbFrom;
        private ExControls.ExDateTimePicker dtpFrom;
        private System.Windows.Forms.Label lHint;
        private System.Windows.Forms.FlowLayoutPanel flowButtons;
        private ExControls.ExButton bCancel;
        private ExControls.ExButton bOk;
    }
}
