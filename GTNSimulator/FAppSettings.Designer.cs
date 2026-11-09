namespace GTNSimulator
{
    partial class FAppSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FAppSettings));
            ExControls.OptionsNode optionsNodeSimulation = new ExControls.OptionsNode();
            this.pSimulation = new ExControls.ExOptionsPanel(this.optionsView);
            this.ppSimulation = new System.Windows.Forms.Panel();
            this.lSimulationHint = new System.Windows.Forms.Label();
            this.gbRoutes = new ExControls.ExGroupBox();
            this.tlpRoutes = new System.Windows.Forms.TableLayoutPanel();
            this.gbModel = new ExControls.ExGroupBox();
            this.tlpModel = new System.Windows.Forms.TableLayoutPanel();
            this.lSeed = new System.Windows.Forms.Label();
            this.nudSeed = new ExControls.ExNumericUpDown();
            this.lOnTime = new System.Windows.Forms.Label();
            this.nudOnTime = new ExControls.ExNumericUpDown();
            this.lMeanDelay = new System.Windows.Forms.Label();
            this.nudMeanDelay = new ExControls.ExNumericUpDown();
            this.lTrackChange = new System.Windows.Forms.Label();
            this.nudTrackChange = new ExControls.ExNumericUpDown();
            this.lReason = new System.Windows.Forms.Label();
            this.nudReason = new ExControls.ExNumericUpDown();
            this.lEntryLead = new System.Windows.Forms.Label();
            this.nudEntryLead = new ExControls.ExNumericUpDown();
            this.lExitLead = new System.Windows.Forms.Label();
            this.nudExitLead = new ExControls.ExNumericUpDown();
            this.cboxLineTrack = new ExControls.ExCheckBox();
            this.tbLineTrack = new ExControls.ExTextBox();
            this.lLineTrackHint = new System.Windows.Forms.Label();
            ExControls.OptionsNode optionsNodeConnection = new ExControls.OptionsNode();
            this.pConnection = new ExControls.ExOptionsPanel(this.optionsView);
            this.ppConnection = new System.Windows.Forms.Panel();
            this.gbGtnLog = new ExControls.ExGroupBox();
            this.tlpGtnLog = new System.Windows.Forms.TableLayoutPanel();
            this.gbIniss = new ExControls.ExGroupBox();
            this.tlpIniss = new System.Windows.Forms.TableLayoutPanel();
            this.lHost = new System.Windows.Forms.Label();
            this.tbHost = new ExControls.ExTextBox();
            this.lPort = new System.Windows.Forms.Label();
            this.nudPort = new ExControls.ExNumericUpDown();
            this.lPortHint = new System.Windows.Forms.Label();
            this.cboxWatchGtnLog = new ExControls.ExCheckBox();
            this.lGtnLog = new System.Windows.Forms.Label();
            this.tbGtnLog = new ExControls.ExTextBox();
            this.bBrowseGtnLog = new ExControls.ExButton();
            this.lGtnLogHint = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.optionsView)).BeginInit();
            this.pSimulation.SuspendLayout();
            this.ppSimulation.SuspendLayout();
            this.gbRoutes.SuspendLayout();
            this.tlpRoutes.SuspendLayout();
            this.gbModel.SuspendLayout();
            this.tlpModel.SuspendLayout();
            this.pConnection.SuspendLayout();
            this.ppConnection.SuspendLayout();
            this.gbGtnLog.SuspendLayout();
            this.tlpGtnLog.SuspendLayout();
            this.gbIniss.SuspendLayout();
            this.tlpIniss.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSeed)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOnTime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMeanDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTrackChange)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudReason)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEntryLead)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudExitLead)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).BeginInit();
            this.SuspendLayout();
            // 
            // optionsView
            // 
            this.optionsView.Panels.Add(this.pSimulation);
            this.optionsView.Panels.Add(this.pConnection);
            // 
            // pSimulation
            // 
            this.pSimulation.Controls.Add(this.ppSimulation);
            this.pSimulation.Name = "pSimulation";
            optionsNodeSimulation.Name = "";
            optionsNodeSimulation.Text = resources.GetString("optionsNodeSimulation.Text");
            this.pSimulation.Node = optionsNodeSimulation;
            this.pSimulation.NodeText = resources.GetString("pSimulation.NodeText");
            this.pSimulation.ParentNode = null;
            // 
            // ppSimulation
            // 
            this.ppSimulation.Controls.Add(this.lSimulationHint);
            this.ppSimulation.Controls.Add(this.gbRoutes);
            this.ppSimulation.Controls.Add(this.gbModel);
            resources.ApplyResources(this.ppSimulation, "ppSimulation");
            this.ppSimulation.Name = "ppSimulation";
            // 
            // lSimulationHint
            // 
            resources.ApplyResources(this.lSimulationHint, "lSimulationHint");
            this.lSimulationHint.Name = "lSimulationHint";
            // 
            // gbRoutes
            // 
            this.gbRoutes.Controls.Add(this.tlpRoutes);
            resources.ApplyResources(this.gbRoutes, "gbRoutes");
            this.gbRoutes.DisabledForeColor = System.Drawing.SystemColors.GrayText;
            this.gbRoutes.Name = "gbRoutes";
            // 
            // tlpRoutes
            // 
            this.tlpRoutes.Controls.Add(this.lEntryLead);
            this.tlpRoutes.Controls.Add(this.nudEntryLead);
            this.tlpRoutes.Controls.Add(this.lExitLead);
            this.tlpRoutes.Controls.Add(this.nudExitLead);
            this.tlpRoutes.Controls.Add(this.cboxLineTrack);
            this.tlpRoutes.Controls.Add(this.tbLineTrack);
            this.tlpRoutes.Controls.Add(this.lLineTrackHint);
            resources.ApplyResources(this.tlpRoutes, "tlpRoutes");
            this.tlpRoutes.ColumnCount = 3;
            this.tlpRoutes.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpRoutes.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpRoutes.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpRoutes.Name = "tlpRoutes";
            // 
            // gbModel
            // 
            this.gbModel.Controls.Add(this.tlpModel);
            resources.ApplyResources(this.gbModel, "gbModel");
            this.gbModel.DisabledForeColor = System.Drawing.SystemColors.GrayText;
            this.gbModel.Name = "gbModel";
            // 
            // tlpModel
            // 
            this.tlpModel.Controls.Add(this.lSeed);
            this.tlpModel.Controls.Add(this.nudSeed);
            this.tlpModel.Controls.Add(this.lOnTime);
            this.tlpModel.Controls.Add(this.nudOnTime);
            this.tlpModel.Controls.Add(this.lMeanDelay);
            this.tlpModel.Controls.Add(this.nudMeanDelay);
            this.tlpModel.Controls.Add(this.lTrackChange);
            this.tlpModel.Controls.Add(this.nudTrackChange);
            this.tlpModel.Controls.Add(this.lReason);
            this.tlpModel.Controls.Add(this.nudReason);
            resources.ApplyResources(this.tlpModel, "tlpModel");
            this.tlpModel.ColumnCount = 3;
            this.tlpModel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpModel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpModel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpModel.Name = "tlpModel";
            // 
            // lSeed
            // 
            resources.ApplyResources(this.lSeed, "lSeed");
            this.tlpModel.SetCellPosition(this.lSeed, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 0));
            this.lSeed.Name = "lSeed";
            // 
            // nudSeed
            // 
            resources.ApplyResources(this.nudSeed, "nudSeed");
            this.tlpModel.SetCellPosition(this.nudSeed, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 0));
            this.nudSeed.HighlightColor = System.Drawing.SystemColors.Highlight;
            this.nudSeed.Maximum = new decimal(new int[] { 999999, 0, 0, 0 });
            this.nudSeed.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudSeed.SelectedButtonColor = System.Drawing.SystemColors.Highlight;
            this.nudSeed.Value = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudSeed.Name = "nudSeed";
            // 
            // lOnTime
            // 
            resources.ApplyResources(this.lOnTime, "lOnTime");
            this.tlpModel.SetCellPosition(this.lOnTime, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 1));
            this.lOnTime.Name = "lOnTime";
            // 
            // nudOnTime
            // 
            resources.ApplyResources(this.nudOnTime, "nudOnTime");
            this.tlpModel.SetCellPosition(this.nudOnTime, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 1));
            this.nudOnTime.HighlightColor = System.Drawing.SystemColors.Highlight;
            this.nudOnTime.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.nudOnTime.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudOnTime.SelectedButtonColor = System.Drawing.SystemColors.Highlight;
            this.nudOnTime.Value = new decimal(new int[] { 75, 0, 0, 0 });
            this.nudOnTime.Name = "nudOnTime";
            // 
            // lMeanDelay
            // 
            resources.ApplyResources(this.lMeanDelay, "lMeanDelay");
            this.tlpModel.SetCellPosition(this.lMeanDelay, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 2));
            this.lMeanDelay.Name = "lMeanDelay";
            // 
            // nudMeanDelay
            // 
            resources.ApplyResources(this.nudMeanDelay, "nudMeanDelay");
            this.tlpModel.SetCellPosition(this.nudMeanDelay, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 2));
            this.nudMeanDelay.HighlightColor = System.Drawing.SystemColors.Highlight;
            this.nudMeanDelay.Maximum = new decimal(new int[] { 240, 0, 0, 0 });
            this.nudMeanDelay.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudMeanDelay.SelectedButtonColor = System.Drawing.SystemColors.Highlight;
            this.nudMeanDelay.Value = new decimal(new int[] { 8, 0, 0, 0 });
            this.nudMeanDelay.Name = "nudMeanDelay";
            // 
            // lTrackChange
            // 
            resources.ApplyResources(this.lTrackChange, "lTrackChange");
            this.tlpModel.SetCellPosition(this.lTrackChange, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 3));
            this.lTrackChange.Name = "lTrackChange";
            // 
            // nudTrackChange
            // 
            resources.ApplyResources(this.nudTrackChange, "nudTrackChange");
            this.tlpModel.SetCellPosition(this.nudTrackChange, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 3));
            this.nudTrackChange.HighlightColor = System.Drawing.SystemColors.Highlight;
            this.nudTrackChange.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.nudTrackChange.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudTrackChange.SelectedButtonColor = System.Drawing.SystemColors.Highlight;
            this.nudTrackChange.Value = new decimal(new int[] { 7, 0, 0, 0 });
            this.nudTrackChange.Name = "nudTrackChange";
            // 
            // lReason
            // 
            resources.ApplyResources(this.lReason, "lReason");
            this.tlpModel.SetCellPosition(this.lReason, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 4));
            this.lReason.Name = "lReason";
            // 
            // nudReason
            // 
            resources.ApplyResources(this.nudReason, "nudReason");
            this.tlpModel.SetCellPosition(this.nudReason, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 4));
            this.nudReason.HighlightColor = System.Drawing.SystemColors.Highlight;
            this.nudReason.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.nudReason.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudReason.SelectedButtonColor = System.Drawing.SystemColors.Highlight;
            this.nudReason.Value = new decimal(new int[] { 60, 0, 0, 0 });
            this.nudReason.Name = "nudReason";
            // 
            // lEntryLead
            // 
            resources.ApplyResources(this.lEntryLead, "lEntryLead");
            this.tlpRoutes.SetCellPosition(this.lEntryLead, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 0));
            this.lEntryLead.Name = "lEntryLead";
            // 
            // nudEntryLead
            // 
            resources.ApplyResources(this.nudEntryLead, "nudEntryLead");
            this.tlpRoutes.SetCellPosition(this.nudEntryLead, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 0));
            this.nudEntryLead.HighlightColor = System.Drawing.SystemColors.Highlight;
            this.nudEntryLead.Maximum = new decimal(new int[] { 119, 0, 0, 0 });
            this.nudEntryLead.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudEntryLead.SelectedButtonColor = System.Drawing.SystemColors.Highlight;
            this.nudEntryLead.Value = new decimal(new int[] { 4, 0, 0, 0 });
            this.nudEntryLead.Name = "nudEntryLead";
            // 
            // lExitLead
            // 
            resources.ApplyResources(this.lExitLead, "lExitLead");
            this.tlpRoutes.SetCellPosition(this.lExitLead, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 1));
            this.lExitLead.Name = "lExitLead";
            // 
            // nudExitLead
            // 
            resources.ApplyResources(this.nudExitLead, "nudExitLead");
            this.tlpRoutes.SetCellPosition(this.nudExitLead, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 1));
            this.nudExitLead.HighlightColor = System.Drawing.SystemColors.Highlight;
            this.nudExitLead.Maximum = new decimal(new int[] { 119, 0, 0, 0 });
            this.nudExitLead.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            this.nudExitLead.SelectedButtonColor = System.Drawing.SystemColors.Highlight;
            this.nudExitLead.Value = new decimal(new int[] { 2, 0, 0, 0 });
            this.nudExitLead.Name = "nudExitLead";
            // 
            // cboxLineTrack
            // 
            resources.ApplyResources(this.cboxLineTrack, "cboxLineTrack");
            this.tlpRoutes.SetCellPosition(this.cboxLineTrack, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 2));
            this.cboxLineTrack.BoxBackColor = System.Drawing.Color.White;
            this.cboxLineTrack.HighlightColor = System.Drawing.SystemColors.Highlight;
            this.cboxLineTrack.CheckedChanged += new System.EventHandler(this.cboxLineTrack_CheckedChanged);
            this.cboxLineTrack.Name = "cboxLineTrack";
            // 
            // tbLineTrack
            // 
            resources.ApplyResources(this.tbLineTrack, "tbLineTrack");
            this.tlpRoutes.SetCellPosition(this.tbLineTrack, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 2));
            this.tbLineTrack.MaxLength = 4;
            this.tbLineTrack.Name = "tbLineTrack";
            // 
            // lLineTrackHint
            // 
            resources.ApplyResources(this.lLineTrackHint, "lLineTrackHint");
            this.tlpRoutes.SetCellPosition(this.lLineTrackHint, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 3));
            this.tlpRoutes.SetColumnSpan(this.lLineTrackHint, 3);
            this.lLineTrackHint.Name = "lLineTrackHint";
            // 
            // pConnection
            // 
            this.pConnection.Controls.Add(this.ppConnection);
            this.pConnection.Name = "pConnection";
            optionsNodeConnection.Name = "";
            optionsNodeConnection.Text = resources.GetString("optionsNodeConnection.Text");
            this.pConnection.Node = optionsNodeConnection;
            this.pConnection.NodeText = resources.GetString("pConnection.NodeText");
            this.pConnection.ParentNode = null;
            // 
            // ppConnection
            // 
            this.ppConnection.Controls.Add(this.gbGtnLog);
            this.ppConnection.Controls.Add(this.gbIniss);
            resources.ApplyResources(this.ppConnection, "ppConnection");
            this.ppConnection.Name = "ppConnection";
            // 
            // gbGtnLog
            // 
            this.gbGtnLog.Controls.Add(this.tlpGtnLog);
            resources.ApplyResources(this.gbGtnLog, "gbGtnLog");
            this.gbGtnLog.DisabledForeColor = System.Drawing.SystemColors.GrayText;
            this.gbGtnLog.Name = "gbGtnLog";
            // 
            // tlpGtnLog
            // 
            this.tlpGtnLog.Controls.Add(this.cboxWatchGtnLog);
            this.tlpGtnLog.Controls.Add(this.lGtnLog);
            this.tlpGtnLog.Controls.Add(this.tbGtnLog);
            this.tlpGtnLog.Controls.Add(this.bBrowseGtnLog);
            this.tlpGtnLog.Controls.Add(this.lGtnLogHint);
            resources.ApplyResources(this.tlpGtnLog, "tlpGtnLog");
            this.tlpGtnLog.ColumnCount = 3;
            this.tlpGtnLog.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpGtnLog.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpGtnLog.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpGtnLog.Name = "tlpGtnLog";
            // 
            // gbIniss
            // 
            this.gbIniss.Controls.Add(this.tlpIniss);
            resources.ApplyResources(this.gbIniss, "gbIniss");
            this.gbIniss.DisabledForeColor = System.Drawing.SystemColors.GrayText;
            this.gbIniss.Name = "gbIniss";
            // 
            // tlpIniss
            // 
            this.tlpIniss.Controls.Add(this.lHost);
            this.tlpIniss.Controls.Add(this.tbHost);
            this.tlpIniss.Controls.Add(this.lPort);
            this.tlpIniss.Controls.Add(this.nudPort);
            this.tlpIniss.Controls.Add(this.lPortHint);
            resources.ApplyResources(this.tlpIniss, "tlpIniss");
            this.tlpIniss.ColumnCount = 3;
            this.tlpIniss.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpIniss.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlpIniss.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpIniss.Name = "tlpIniss";
            // 
            // lHost
            // 
            resources.ApplyResources(this.lHost, "lHost");
            this.tlpIniss.SetCellPosition(this.lHost, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 0));
            this.lHost.Name = "lHost";
            // 
            // tbHost
            // 
            resources.ApplyResources(this.tbHost, "tbHost");
            this.tlpIniss.SetCellPosition(this.tbHost, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 0));
            this.tbHost.Name = "tbHost";
            // 
            // lPort
            // 
            resources.ApplyResources(this.lPort, "lPort");
            this.tlpIniss.SetCellPosition(this.lPort, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 1));
            this.lPort.Name = "lPort";
            // 
            // nudPort
            // 
            resources.ApplyResources(this.nudPort, "nudPort");
            this.tlpIniss.SetCellPosition(this.nudPort, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 1));
            this.nudPort.HighlightColor = System.Drawing.SystemColors.Highlight;
            this.nudPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            this.nudPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.nudPort.SelectedButtonColor = System.Drawing.SystemColors.Highlight;
            this.nudPort.Value = new decimal(new int[] { 4000, 0, 0, 0 });
            this.nudPort.Name = "nudPort";
            // 
            // lPortHint
            // 
            resources.ApplyResources(this.lPortHint, "lPortHint");
            this.tlpIniss.SetCellPosition(this.lPortHint, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 2));
            this.tlpIniss.SetColumnSpan(this.lPortHint, 3);
            this.lPortHint.Name = "lPortHint";
            // 
            // cboxWatchGtnLog
            // 
            resources.ApplyResources(this.cboxWatchGtnLog, "cboxWatchGtnLog");
            this.tlpGtnLog.SetCellPosition(this.cboxWatchGtnLog, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 0));
            this.tlpGtnLog.SetColumnSpan(this.cboxWatchGtnLog, 3);
            this.cboxWatchGtnLog.BoxBackColor = System.Drawing.Color.White;
            this.cboxWatchGtnLog.HighlightColor = System.Drawing.SystemColors.Highlight;
            this.cboxWatchGtnLog.CheckedChanged += new System.EventHandler(this.cboxWatchGtnLog_CheckedChanged);
            this.cboxWatchGtnLog.Name = "cboxWatchGtnLog";
            // 
            // lGtnLog
            // 
            resources.ApplyResources(this.lGtnLog, "lGtnLog");
            this.tlpGtnLog.SetCellPosition(this.lGtnLog, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 1));
            this.lGtnLog.Name = "lGtnLog";
            // 
            // tbGtnLog
            // 
            resources.ApplyResources(this.tbGtnLog, "tbGtnLog");
            this.tlpGtnLog.SetCellPosition(this.tbGtnLog, new System.Windows.Forms.TableLayoutPanelCellPosition(1, 1));
            this.tbGtnLog.Name = "tbGtnLog";
            // 
            // bBrowseGtnLog
            // 
            resources.ApplyResources(this.bBrowseGtnLog, "bBrowseGtnLog");
            this.tlpGtnLog.SetCellPosition(this.bBrowseGtnLog, new System.Windows.Forms.TableLayoutPanelCellPosition(2, 1));
            this.bBrowseGtnLog.UseVisualStyleBackColor = true;
            this.bBrowseGtnLog.Click += new System.EventHandler(this.bBrowseGtnLog_Click);
            this.bBrowseGtnLog.Name = "bBrowseGtnLog";
            // 
            // lGtnLogHint
            // 
            resources.ApplyResources(this.lGtnLogHint, "lGtnLogHint");
            this.tlpGtnLog.SetCellPosition(this.lGtnLogHint, new System.Windows.Forms.TableLayoutPanelCellPosition(0, 2));
            this.tlpGtnLog.SetColumnSpan(this.lGtnLogHint, 3);
            this.lGtnLogHint.Name = "lGtnLogHint";
            // 
            // FAppSettings
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Name = "FAppSettings";
            ((System.ComponentModel.ISupportInitialize)(this.optionsView)).EndInit();
            this.tlpIniss.ResumeLayout(false);
            this.tlpIniss.PerformLayout();
            this.gbIniss.ResumeLayout(false);
            this.gbIniss.PerformLayout();
            this.tlpGtnLog.ResumeLayout(false);
            this.tlpGtnLog.PerformLayout();
            this.gbGtnLog.ResumeLayout(false);
            this.gbGtnLog.PerformLayout();
            this.ppConnection.ResumeLayout(false);
            this.ppConnection.PerformLayout();
            this.pConnection.ResumeLayout(false);
            this.pConnection.PerformLayout();
            this.tlpModel.ResumeLayout(false);
            this.tlpModel.PerformLayout();
            this.gbModel.ResumeLayout(false);
            this.gbModel.PerformLayout();
            this.tlpRoutes.ResumeLayout(false);
            this.tlpRoutes.PerformLayout();
            this.gbRoutes.ResumeLayout(false);
            this.gbRoutes.PerformLayout();
            this.ppSimulation.ResumeLayout(false);
            this.ppSimulation.PerformLayout();
            this.pSimulation.ResumeLayout(false);
            this.pSimulation.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudSeed)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudOnTime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudMeanDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudTrackChange)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudReason)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudEntryLead)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudExitLead)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPort)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ExControls.ExOptionsPanel pSimulation;
        private System.Windows.Forms.Panel ppSimulation;
        private System.Windows.Forms.Label lSimulationHint;
        private ExControls.ExGroupBox gbRoutes;
        private System.Windows.Forms.TableLayoutPanel tlpRoutes;
        private ExControls.ExGroupBox gbModel;
        private System.Windows.Forms.TableLayoutPanel tlpModel;
        private System.Windows.Forms.Label lSeed;
        private ExControls.ExNumericUpDown nudSeed;
        private System.Windows.Forms.Label lOnTime;
        private ExControls.ExNumericUpDown nudOnTime;
        private System.Windows.Forms.Label lMeanDelay;
        private ExControls.ExNumericUpDown nudMeanDelay;
        private System.Windows.Forms.Label lTrackChange;
        private ExControls.ExNumericUpDown nudTrackChange;
        private System.Windows.Forms.Label lReason;
        private ExControls.ExNumericUpDown nudReason;
        private System.Windows.Forms.Label lEntryLead;
        private ExControls.ExNumericUpDown nudEntryLead;
        private System.Windows.Forms.Label lExitLead;
        private ExControls.ExNumericUpDown nudExitLead;
        private ExControls.ExCheckBox cboxLineTrack;
        private ExControls.ExTextBox tbLineTrack;
        private System.Windows.Forms.Label lLineTrackHint;
        private ExControls.ExOptionsPanel pConnection;
        private System.Windows.Forms.Panel ppConnection;
        private ExControls.ExGroupBox gbGtnLog;
        private System.Windows.Forms.TableLayoutPanel tlpGtnLog;
        private ExControls.ExGroupBox gbIniss;
        private System.Windows.Forms.TableLayoutPanel tlpIniss;
        private System.Windows.Forms.Label lHost;
        private ExControls.ExTextBox tbHost;
        private System.Windows.Forms.Label lPort;
        private ExControls.ExNumericUpDown nudPort;
        private System.Windows.Forms.Label lPortHint;
        private ExControls.ExCheckBox cboxWatchGtnLog;
        private System.Windows.Forms.Label lGtnLog;
        private ExControls.ExTextBox tbGtnLog;
        private ExControls.ExButton bBrowseGtnLog;
        private System.Windows.Forms.Label lGtnLogHint;
    }
}
