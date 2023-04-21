namespace SRA_Debugger
{
    partial class VectorRegisterForm
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
            menuStrip1 = new MenuStrip();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            vectorDataTypeToolStripMenuItem = new ToolStripMenuItem();
            int32ToolStripMenuItem = new ToolStripMenuItem();
            int64ToolStripMenuItem = new ToolStripMenuItem();
            uint32ToolStripMenuItem = new ToolStripMenuItem();
            uint64ToolStripMenuItem = new ToolStripMenuItem();
            floatToolStripMenuItem = new ToolStripMenuItem();
            doubleToolStripMenuItem = new ToolStripMenuItem();
            vectorRegDisplay = new TextBox();
            menuStrip1.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = SystemColors.ControlLightLight;
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { settingsToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1578, 33);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { vectorDataTypeToolStripMenuItem });
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(93, 29);
            settingsToolStripMenuItem.Text = "Settings";
            // 
            // vectorDataTypeToolStripMenuItem
            // 
            vectorDataTypeToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { int32ToolStripMenuItem, int64ToolStripMenuItem, uint32ToolStripMenuItem, uint64ToolStripMenuItem, floatToolStripMenuItem, doubleToolStripMenuItem });
            vectorDataTypeToolStripMenuItem.Name = "vectorDataTypeToolStripMenuItem";
            vectorDataTypeToolStripMenuItem.Size = new Size(250, 34);
            vectorDataTypeToolStripMenuItem.Text = "Vector data type";
            // 
            // int32ToolStripMenuItem
            // 
            int32ToolStripMenuItem.Name = "int32ToolStripMenuItem";
            int32ToolStripMenuItem.Size = new Size(173, 34);
            int32ToolStripMenuItem.Text = "Int32";
            int32ToolStripMenuItem.Click += int32ToolStripMenuItem_Click;
            // 
            // int64ToolStripMenuItem
            // 
            int64ToolStripMenuItem.Name = "int64ToolStripMenuItem";
            int64ToolStripMenuItem.Size = new Size(173, 34);
            int64ToolStripMenuItem.Text = "Int64";
            int64ToolStripMenuItem.Click += int64ToolStripMenuItem_Click;
            // 
            // uint32ToolStripMenuItem
            // 
            uint32ToolStripMenuItem.Name = "uint32ToolStripMenuItem";
            uint32ToolStripMenuItem.Size = new Size(173, 34);
            uint32ToolStripMenuItem.Text = "Uint32";
            uint32ToolStripMenuItem.Click += uint32ToolStripMenuItem_Click;
            // 
            // uint64ToolStripMenuItem
            // 
            uint64ToolStripMenuItem.Checked = true;
            uint64ToolStripMenuItem.CheckState = CheckState.Checked;
            uint64ToolStripMenuItem.Name = "uint64ToolStripMenuItem";
            uint64ToolStripMenuItem.Size = new Size(173, 34);
            uint64ToolStripMenuItem.Text = "Uint64";
            uint64ToolStripMenuItem.Click += uint64ToolStripMenuItem_Click;
            // 
            // floatToolStripMenuItem
            // 
            floatToolStripMenuItem.Name = "floatToolStripMenuItem";
            floatToolStripMenuItem.Size = new Size(173, 34);
            floatToolStripMenuItem.Text = "Float";
            floatToolStripMenuItem.Click += floatToolStripMenuItem_Click;
            // 
            // doubleToolStripMenuItem
            // 
            doubleToolStripMenuItem.Name = "doubleToolStripMenuItem";
            doubleToolStripMenuItem.Size = new Size(173, 34);
            doubleToolStripMenuItem.Text = "Double";
            doubleToolStripMenuItem.Click += doubleToolStripMenuItem_Click;
            // 
            // vectorRegDisplay
            // 
            vectorRegDisplay.BackColor = SystemColors.Window;
            vectorRegDisplay.BorderStyle = BorderStyle.FixedSingle;
            vectorRegDisplay.Dock = DockStyle.Fill;
            vectorRegDisplay.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point);
            vectorRegDisplay.Location = new Point(0, 33);
            vectorRegDisplay.Multiline = true;
            vectorRegDisplay.Name = "vectorRegDisplay";
            vectorRegDisplay.ReadOnly = true;
            vectorRegDisplay.Size = new Size(1578, 1311);
            vectorRegDisplay.TabIndex = 1;
            // 
            // VectorRegisterForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1578, 1344);
            Controls.Add(vectorRegDisplay);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MainMenuStrip = menuStrip1;
            Name = "VectorRegisterForm";
            Text = "Vector registers";
            FormClosing += VectorRegisterForm_FormClosing;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem vectorDataTypeToolStripMenuItem;
        private ToolStripMenuItem int32ToolStripMenuItem;
        private ToolStripMenuItem int64ToolStripMenuItem;
        private ToolStripMenuItem uint32ToolStripMenuItem;
        private ToolStripMenuItem uint64ToolStripMenuItem;
        private ToolStripMenuItem floatToolStripMenuItem;
        private ToolStripMenuItem doubleToolStripMenuItem;
        private TextBox vectorRegDisplay;
    }
}