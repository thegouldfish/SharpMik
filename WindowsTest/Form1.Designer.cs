namespace SharpMilk
{
	partial class Form1
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
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.OpenMod = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.PlayPauseMod = new System.Windows.Forms.ToolStripButton();
			this.StopMod = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton2 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton3 = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.CloseApp = new System.Windows.Forms.ToolStripButton();
			this.listBox1 = new System.Windows.Forms.ListBox();
			this.trackBar1 = new System.Windows.Forms.TrackBar();
			this.toolStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
			this.SuspendLayout();
			// 
			// toolStrip1
			// 
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.OpenMod,
            this.toolStripButton1,
            this.toolStripSeparator1,
            this.PlayPauseMod,
            this.StopMod,
            this.toolStripButton2,
            this.toolStripButton3,
            this.toolStripSeparator2,
            this.toolStripLabel1,
            this.toolStripSeparator3,
            this.CloseApp});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(510, 25);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.Text = "toolStrip1";
			this.toolStrip1.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.toolStrip1_ItemClicked);
			// 
			// OpenMod
			// 
			this.OpenMod.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.OpenMod.Image = global::SharpMikTester.Properties.Resources.openHS;
			this.OpenMod.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.OpenMod.Name = "OpenMod";
			this.OpenMod.Size = new System.Drawing.Size(23, 22);
			this.OpenMod.Text = "Open";
			this.OpenMod.Click += new System.EventHandler(this.OpenMod_Click);
			// 
			// toolStripButton1
			// 
			this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton1.Image = global::SharpMikTester.Properties.Resources._042b_AddCategory_16x16_72;
			this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(23, 22);
			this.toolStripButton1.Text = "toolStripButton1";
			this.toolStripButton1.Click += new System.EventHandler(this.toolStripButton1_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// PlayPauseMod
			// 
			this.PlayPauseMod.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.PlayPauseMod.Image = global::SharpMikTester.Properties.Resources.PlayHS;
			this.PlayPauseMod.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.PlayPauseMod.Name = "PlayPauseMod";
			this.PlayPauseMod.Size = new System.Drawing.Size(23, 22);
			this.PlayPauseMod.Text = "Play";
			this.PlayPauseMod.Click += new System.EventHandler(this.PlayPauseMod_Click);
			// 
			// StopMod
			// 
			this.StopMod.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.StopMod.Image = global::SharpMikTester.Properties.Resources.StopHS;
			this.StopMod.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.StopMod.Name = "StopMod";
			this.StopMod.Size = new System.Drawing.Size(23, 22);
			this.StopMod.Text = "toolStripButton3";
			this.StopMod.Click += new System.EventHandler(this.StopMod_Click);
			// 
			// toolStripButton2
			// 
			this.toolStripButton2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton2.Image = global::SharpMikTester.Properties.Resources.NavBack;
			this.toolStripButton2.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton2.Name = "toolStripButton2";
			this.toolStripButton2.RightToLeft = System.Windows.Forms.RightToLeft.No;
			this.toolStripButton2.Size = new System.Drawing.Size(23, 22);
			this.toolStripButton2.Text = "toolStripButton2";
			this.toolStripButton2.Click += new System.EventHandler(this.toolStripButton2_Click);
			// 
			// toolStripButton3
			// 
			this.toolStripButton3.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButton3.Image = global::SharpMikTester.Properties.Resources.NavForward;
			this.toolStripButton3.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButton3.Name = "toolStripButton3";
			this.toolStripButton3.Size = new System.Drawing.Size(23, 22);
			this.toolStripButton3.Text = "toolStripButton3";
			this.toolStripButton3.Click += new System.EventHandler(this.toolStripButton3_Click);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// toolStripLabel1
			// 
			this.toolStripLabel1.AutoSize = false;
			this.toolStripLabel1.Name = "toolStripLabel1";
			this.toolStripLabel1.Size = new System.Drawing.Size(200, 22);
			// 
			// toolStripSeparator3
			// 
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
			// 
			// CloseApp
			// 
			this.CloseApp.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.CloseApp.Image = global::SharpMikTester.Properties.Resources._305_Close_16x16_72;
			this.CloseApp.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.CloseApp.Name = "CloseApp";
			this.CloseApp.Size = new System.Drawing.Size(23, 22);
			this.CloseApp.Text = "toolStripButton4";
			this.CloseApp.Click += new System.EventHandler(this.CloseApp_Click);
			// 
			// listBox1
			// 
			this.listBox1.FormattingEnabled = true;
			this.listBox1.Location = new System.Drawing.Point(0, 59);
			this.listBox1.Margin = new System.Windows.Forms.Padding(0);
			this.listBox1.Name = "listBox1";
			this.listBox1.Size = new System.Drawing.Size(509, 212);
			this.listBox1.TabIndex = 2;
			// 
			// trackBar1
			// 
			this.trackBar1.Enabled = false;
			this.trackBar1.Location = new System.Drawing.Point(0, 28);
			this.trackBar1.Name = "trackBar1";
			this.trackBar1.Size = new System.Drawing.Size(509, 45);
			this.trackBar1.TabIndex = 3;
			this.trackBar1.TickStyle = System.Windows.Forms.TickStyle.None;
			// 
			// Form1
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(510, 268);
			this.ControlBox = false;
			this.Controls.Add(this.listBox1);
			this.Controls.Add(this.trackBar1);
			this.Controls.Add(this.toolStrip1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Name = "Form1";
			this.Text = "  ";
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton OpenMod;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripButton PlayPauseMod;
		private System.Windows.Forms.ToolStripButton StopMod;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripLabel toolStripLabel1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripButton CloseApp;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.ToolStripButton toolStripButton2;
		private System.Windows.Forms.ToolStripButton toolStripButton3;
		private System.Windows.Forms.ListBox listBox1;
		private System.Windows.Forms.TrackBar trackBar1;
	}
}

