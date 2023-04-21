namespace SRA_Debugger
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            Label label3;
            Label label4;
            Label label2;
            Label label1;
            Label label5;
            menuStrip1 = new MenuStrip();
            fileToolStripMenuItem = new ToolStripMenuItem();
            openToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            settingsToolStripMenuItem = new ToolStripMenuItem();
            registerRepToolStripMenuItem = new ToolStripMenuItem();
            hexToolStripMenuItem = new ToolStripMenuItem();
            decimalToolStripMenuItem = new ToolStripMenuItem();
            registerDataTypeToolStripMenuItem = new ToolStripMenuItem();
            int32ToolStripMenuItem = new ToolStripMenuItem();
            int64ToolStripMenuItem = new ToolStripMenuItem();
            uint32ToolStripMenuItem = new ToolStripMenuItem();
            uint64ToolStripMenuItem = new ToolStripMenuItem();
            floatToolStripMenuItem = new ToolStripMenuItem();
            doubleToolStripMenuItem = new ToolStripMenuItem();
            showIntermediateStateToolStripMenuItem = new ToolStripMenuItem();
            windowsToolStripMenuItem = new ToolStripMenuItem();
            vectorRegistersToolStripMenuItem = new ToolStripMenuItem();
            splitContainer1 = new SplitContainer();
            splitContainer5 = new SplitContainer();
            flowLayoutPanel1 = new FlowLayoutPanel();
            run = new Button();
            progress = new Button();
            stop = new Button();
            runFor = new Button();
            runCnt = new NumericUpDown();
            splitContainer6 = new SplitContainer();
            splitContainer7 = new SplitContainer();
            splitContainer8 = new SplitContainer();
            memoryAddr = new TextBox();
            memory = new TextBox();
            splitContainer2 = new SplitContainer();
            splitContainer4 = new SplitContainer();
            disassemble = new RichTextBox();
            splitContainer3 = new SplitContainer();
            regDisplay = new TextBox();
            openFileDialog = new OpenFileDialog();
            label3 = new Label();
            label4 = new Label();
            label2 = new Label();
            label1 = new Label();
            label5 = new Label();
            menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer5).BeginInit();
            splitContainer5.Panel1.SuspendLayout();
            splitContainer5.Panel2.SuspendLayout();
            splitContainer5.SuspendLayout();
            flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)runCnt).BeginInit();
            ((System.ComponentModel.ISupportInitialize)splitContainer6).BeginInit();
            splitContainer6.Panel1.SuspendLayout();
            splitContainer6.Panel2.SuspendLayout();
            splitContainer6.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer7).BeginInit();
            splitContainer7.Panel1.SuspendLayout();
            splitContainer7.Panel2.SuspendLayout();
            splitContainer7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer8).BeginInit();
            splitContainer8.Panel1.SuspendLayout();
            splitContainer8.Panel2.SuspendLayout();
            splitContainer8.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer2).BeginInit();
            splitContainer2.Panel1.SuspendLayout();
            splitContainer2.Panel2.SuspendLayout();
            splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer4).BeginInit();
            splitContainer4.Panel1.SuspendLayout();
            splitContainer4.Panel2.SuspendLayout();
            splitContainer4.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).BeginInit();
            splitContainer3.Panel1.SuspendLayout();
            splitContainer3.Panel2.SuspendLayout();
            splitContainer3.SuspendLayout();
            SuspendLayout();
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Dock = DockStyle.Bottom;
            label3.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label3.Location = new Point(0, 12);
            label3.Name = "label3";
            label3.Size = new Size(90, 28);
            label3.TabIndex = 2;
            label3.Text = "Memory";
            label3.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Dock = DockStyle.Bottom;
            label4.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label4.Location = new Point(0, 5);
            label4.Name = "label4";
            label4.Size = new Size(103, 28);
            label4.TabIndex = 2;
            label4.Text = "Address";
            label4.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Dock = DockStyle.Bottom;
            label2.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label2.Location = new Point(0, 8);
            label2.Name = "label2";
            label2.Size = new Size(155, 28);
            label2.TabIndex = 1;
            label2.Text = "Disassemble";
            label2.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Dock = DockStyle.Bottom;
            label1.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label1.Location = new Point(0, 8);
            label1.Name = "label1";
            label1.Size = new Size(129, 28);
            label1.TabIndex = 0;
            label1.Text = "Registers";
            label1.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Dock = DockStyle.Fill;
            label5.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point);
            label5.Location = new Point(645, 0);
            label5.Name = "label5";
            label5.Size = new Size(90, 44);
            label5.TabIndex = 5;
            label5.Text = "Cycles";
            label5.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // menuStrip1
            // 
            menuStrip1.BackColor = SystemColors.ButtonHighlight;
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripMenuItem, settingsToolStripMenuItem, windowsToolStripMenuItem });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(1898, 33);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            fileToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { openToolStripMenuItem, exitToolStripMenuItem });
            fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            fileToolStripMenuItem.Size = new Size(55, 29);
            fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            openToolStripMenuItem.Name = "openToolStripMenuItem";
            openToolStripMenuItem.Size = new Size(159, 34);
            openToolStripMenuItem.Text = "Open";
            openToolStripMenuItem.Click += openToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(159, 34);
            exitToolStripMenuItem.Text = "Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { registerRepToolStripMenuItem, registerDataTypeToolStripMenuItem, showIntermediateStateToolStripMenuItem });
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(93, 29);
            settingsToolStripMenuItem.Text = "Settings";
            // 
            // registerRepToolStripMenuItem
            // 
            registerRepToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { hexToolStripMenuItem, decimalToolStripMenuItem });
            registerRepToolStripMenuItem.Name = "registerRepToolStripMenuItem";
            registerRepToolStripMenuItem.Size = new Size(311, 34);
            registerRepToolStripMenuItem.Text = "Register Representation";
            // 
            // hexToolStripMenuItem
            // 
            hexToolStripMenuItem.Checked = true;
            hexToolStripMenuItem.CheckState = CheckState.Checked;
            hexToolStripMenuItem.Name = "hexToolStripMenuItem";
            hexToolStripMenuItem.Size = new Size(179, 34);
            hexToolStripMenuItem.Text = "Hex";
            hexToolStripMenuItem.Click += hexToolStripMenuItem_Click;
            // 
            // decimalToolStripMenuItem
            // 
            decimalToolStripMenuItem.Name = "decimalToolStripMenuItem";
            decimalToolStripMenuItem.Size = new Size(179, 34);
            decimalToolStripMenuItem.Text = "Decimal";
            decimalToolStripMenuItem.Click += decimalToolStripMenuItem_Click;
            // 
            // registerDataTypeToolStripMenuItem
            // 
            registerDataTypeToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { int32ToolStripMenuItem, int64ToolStripMenuItem, uint32ToolStripMenuItem, uint64ToolStripMenuItem, floatToolStripMenuItem, doubleToolStripMenuItem });
            registerDataTypeToolStripMenuItem.Name = "registerDataTypeToolStripMenuItem";
            registerDataTypeToolStripMenuItem.Size = new Size(311, 34);
            registerDataTypeToolStripMenuItem.Text = "Register data type";
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
            // showIntermediateStateToolStripMenuItem
            // 
            showIntermediateStateToolStripMenuItem.Checked = true;
            showIntermediateStateToolStripMenuItem.CheckOnClick = true;
            showIntermediateStateToolStripMenuItem.CheckState = CheckState.Checked;
            showIntermediateStateToolStripMenuItem.Name = "showIntermediateStateToolStripMenuItem";
            showIntermediateStateToolStripMenuItem.Size = new Size(311, 34);
            showIntermediateStateToolStripMenuItem.Text = "Show intermediate state";
            showIntermediateStateToolStripMenuItem.ToolTipText = "If checked, show register and memory value for every clock when running program.";
            // 
            // windowsToolStripMenuItem
            // 
            windowsToolStripMenuItem.DropDownItems.AddRange(new ToolStripItem[] { vectorRegistersToolStripMenuItem });
            windowsToolStripMenuItem.Name = "windowsToolStripMenuItem";
            windowsToolStripMenuItem.Size = new Size(102, 29);
            windowsToolStripMenuItem.Text = "Windows";
            // 
            // vectorRegistersToolStripMenuItem
            // 
            vectorRegistersToolStripMenuItem.Name = "vectorRegistersToolStripMenuItem";
            vectorRegistersToolStripMenuItem.Size = new Size(242, 34);
            vectorRegistersToolStripMenuItem.Text = "Vector registers";
            vectorRegistersToolStripMenuItem.Click += vectorRegistersToolStripMenuItem_Click;
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 33);
            splitContainer1.Name = "splitContainer1";
            splitContainer1.Orientation = Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(splitContainer5);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(splitContainer2);
            splitContainer1.Size = new Size(1898, 991);
            splitContainer1.SplitterDistance = 328;
            splitContainer1.TabIndex = 1;
            // 
            // splitContainer5
            // 
            splitContainer5.Dock = DockStyle.Fill;
            splitContainer5.Location = new Point(0, 0);
            splitContainer5.Name = "splitContainer5";
            splitContainer5.Orientation = Orientation.Horizontal;
            // 
            // splitContainer5.Panel1
            // 
            splitContainer5.Panel1.Controls.Add(flowLayoutPanel1);
            // 
            // splitContainer5.Panel2
            // 
            splitContainer5.Panel2.Controls.Add(splitContainer6);
            splitContainer5.Size = new Size(1898, 328);
            splitContainer5.SplitterDistance = 46;
            splitContainer5.TabIndex = 0;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(run);
            flowLayoutPanel1.Controls.Add(progress);
            flowLayoutPanel1.Controls.Add(stop);
            flowLayoutPanel1.Controls.Add(runFor);
            flowLayoutPanel1.Controls.Add(runCnt);
            flowLayoutPanel1.Controls.Add(label5);
            flowLayoutPanel1.Dock = DockStyle.Fill;
            flowLayoutPanel1.Location = new Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1898, 46);
            flowLayoutPanel1.TabIndex = 0;
            // 
            // run
            // 
            run.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            run.Location = new Point(5, 5);
            run.Margin = new Padding(5);
            run.Name = "run";
            run.Size = new Size(112, 34);
            run.TabIndex = 0;
            run.Text = "Run";
            run.UseVisualStyleBackColor = true;
            run.Click += run_Click;
            // 
            // progress
            // 
            progress.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            progress.Location = new Point(127, 5);
            progress.Margin = new Padding(5);
            progress.Name = "progress";
            progress.Size = new Size(112, 34);
            progress.TabIndex = 1;
            progress.Text = "Progress";
            progress.UseVisualStyleBackColor = true;
            progress.Click += progress_Click;
            // 
            // stop
            // 
            stop.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            stop.Location = new Point(249, 5);
            stop.Margin = new Padding(5);
            stop.Name = "stop";
            stop.Size = new Size(112, 34);
            stop.TabIndex = 2;
            stop.Text = "Stop";
            stop.UseVisualStyleBackColor = true;
            stop.Click += stop_Click;
            // 
            // runFor
            // 
            runFor.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            runFor.Location = new Point(371, 5);
            runFor.Margin = new Padding(5);
            runFor.Name = "runFor";
            runFor.Size = new Size(147, 34);
            runFor.TabIndex = 3;
            runFor.Text = "Run For...";
            runFor.UseVisualStyleBackColor = true;
            runFor.Click += runFor_Click;
            // 
            // runCnt
            // 
            runCnt.Anchor = AnchorStyles.Left;
            runCnt.Location = new Point(526, 6);
            runCnt.Maximum = new decimal(new int[] { 1000, 0, 0, 0 });
            runCnt.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            runCnt.Name = "runCnt";
            runCnt.Size = new Size(113, 31);
            runCnt.TabIndex = 4;
            runCnt.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // splitContainer6
            // 
            splitContainer6.Dock = DockStyle.Fill;
            splitContainer6.Location = new Point(0, 0);
            splitContainer6.Name = "splitContainer6";
            splitContainer6.Orientation = Orientation.Horizontal;
            // 
            // splitContainer6.Panel1
            // 
            splitContainer6.Panel1.BackColor = SystemColors.ControlLight;
            splitContainer6.Panel1.Controls.Add(label3);
            // 
            // splitContainer6.Panel2
            // 
            splitContainer6.Panel2.Controls.Add(splitContainer7);
            splitContainer6.Size = new Size(1898, 278);
            splitContainer6.SplitterDistance = 40;
            splitContainer6.TabIndex = 0;
            // 
            // splitContainer7
            // 
            splitContainer7.Dock = DockStyle.Fill;
            splitContainer7.Location = new Point(0, 0);
            splitContainer7.Name = "splitContainer7";
            splitContainer7.Orientation = Orientation.Horizontal;
            // 
            // splitContainer7.Panel1
            // 
            splitContainer7.Panel1.Controls.Add(splitContainer8);
            // 
            // splitContainer7.Panel2
            // 
            splitContainer7.Panel2.Controls.Add(memory);
            splitContainer7.Size = new Size(1898, 234);
            splitContainer7.SplitterDistance = 33;
            splitContainer7.TabIndex = 0;
            // 
            // splitContainer8
            // 
            splitContainer8.Dock = DockStyle.Fill;
            splitContainer8.Location = new Point(0, 0);
            splitContainer8.Name = "splitContainer8";
            // 
            // splitContainer8.Panel1
            // 
            splitContainer8.Panel1.Controls.Add(label4);
            // 
            // splitContainer8.Panel2
            // 
            splitContainer8.Panel2.Controls.Add(memoryAddr);
            splitContainer8.Size = new Size(1898, 33);
            splitContainer8.SplitterDistance = 114;
            splitContainer8.TabIndex = 0;
            // 
            // memoryAddr
            // 
            memoryAddr.BorderStyle = BorderStyle.FixedSingle;
            memoryAddr.Dock = DockStyle.Fill;
            memoryAddr.Font = new Font("Consolas", 10F, FontStyle.Regular, GraphicsUnit.Point);
            memoryAddr.Location = new Point(0, 0);
            memoryAddr.Name = "memoryAddr";
            memoryAddr.Size = new Size(1780, 31);
            memoryAddr.TabIndex = 0;
            memoryAddr.KeyDown += memoryAddr_KeyDown;
            // 
            // memory
            // 
            memory.BackColor = SystemColors.Window;
            memory.BorderStyle = BorderStyle.FixedSingle;
            memory.Dock = DockStyle.Fill;
            memory.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point);
            memory.Location = new Point(0, 0);
            memory.Margin = new Padding(20);
            memory.Multiline = true;
            memory.Name = "memory";
            memory.ReadOnly = true;
            memory.Size = new Size(1898, 197);
            memory.TabIndex = 1;
            // 
            // splitContainer2
            // 
            splitContainer2.Dock = DockStyle.Fill;
            splitContainer2.Location = new Point(0, 0);
            splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            splitContainer2.Panel1.Controls.Add(splitContainer4);
            // 
            // splitContainer2.Panel2
            // 
            splitContainer2.Panel2.Controls.Add(splitContainer3);
            splitContainer2.Size = new Size(1898, 659);
            splitContainer2.SplitterDistance = 981;
            splitContainer2.TabIndex = 0;
            // 
            // splitContainer4
            // 
            splitContainer4.Dock = DockStyle.Fill;
            splitContainer4.Location = new Point(0, 0);
            splitContainer4.Name = "splitContainer4";
            splitContainer4.Orientation = Orientation.Horizontal;
            // 
            // splitContainer4.Panel1
            // 
            splitContainer4.Panel1.BackColor = SystemColors.ControlLight;
            splitContainer4.Panel1.Controls.Add(label2);
            // 
            // splitContainer4.Panel2
            // 
            splitContainer4.Panel2.Controls.Add(disassemble);
            splitContainer4.Size = new Size(981, 659);
            splitContainer4.SplitterDistance = 36;
            splitContainer4.TabIndex = 0;
            // 
            // disassemble
            // 
            disassemble.BackColor = SystemColors.Window;
            disassemble.BorderStyle = BorderStyle.FixedSingle;
            disassemble.Dock = DockStyle.Fill;
            disassemble.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point);
            disassemble.Location = new Point(0, 0);
            disassemble.Name = "disassemble";
            disassemble.ReadOnly = true;
            disassemble.Size = new Size(981, 619);
            disassemble.TabIndex = 0;
            disassemble.Text = "";
            // 
            // splitContainer3
            // 
            splitContainer3.Dock = DockStyle.Fill;
            splitContainer3.Location = new Point(0, 0);
            splitContainer3.Name = "splitContainer3";
            splitContainer3.Orientation = Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            splitContainer3.Panel1.BackColor = SystemColors.ControlLight;
            splitContainer3.Panel1.Controls.Add(label1);
            // 
            // splitContainer3.Panel2
            // 
            splitContainer3.Panel2.Controls.Add(regDisplay);
            splitContainer3.Size = new Size(913, 659);
            splitContainer3.SplitterDistance = 36;
            splitContainer3.TabIndex = 0;
            // 
            // regDisplay
            // 
            regDisplay.BackColor = SystemColors.Window;
            regDisplay.BorderStyle = BorderStyle.FixedSingle;
            regDisplay.Dock = DockStyle.Fill;
            regDisplay.Font = new Font("Consolas", 12F, FontStyle.Regular, GraphicsUnit.Point);
            regDisplay.Location = new Point(0, 0);
            regDisplay.Margin = new Padding(8);
            regDisplay.Multiline = true;
            regDisplay.Name = "regDisplay";
            regDisplay.ReadOnly = true;
            regDisplay.Size = new Size(913, 619);
            regDisplay.TabIndex = 0;
            // 
            // openFileDialog
            // 
            openFileDialog.Title = "Open program";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1898, 1024);
            Controls.Add(splitContainer1);
            Controls.Add(menuStrip1);
            FormBorderStyle = FormBorderStyle.Fixed3D;
            MainMenuStrip = menuStrip1;
            Name = "MainForm";
            Text = "SRA Debugger";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            splitContainer5.Panel1.ResumeLayout(false);
            splitContainer5.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer5).EndInit();
            splitContainer5.ResumeLayout(false);
            flowLayoutPanel1.ResumeLayout(false);
            flowLayoutPanel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)runCnt).EndInit();
            splitContainer6.Panel1.ResumeLayout(false);
            splitContainer6.Panel1.PerformLayout();
            splitContainer6.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer6).EndInit();
            splitContainer6.ResumeLayout(false);
            splitContainer7.Panel1.ResumeLayout(false);
            splitContainer7.Panel2.ResumeLayout(false);
            splitContainer7.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer7).EndInit();
            splitContainer7.ResumeLayout(false);
            splitContainer8.Panel1.ResumeLayout(false);
            splitContainer8.Panel1.PerformLayout();
            splitContainer8.Panel2.ResumeLayout(false);
            splitContainer8.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer8).EndInit();
            splitContainer8.ResumeLayout(false);
            splitContainer2.Panel1.ResumeLayout(false);
            splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer2).EndInit();
            splitContainer2.ResumeLayout(false);
            splitContainer4.Panel1.ResumeLayout(false);
            splitContainer4.Panel1.PerformLayout();
            splitContainer4.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer4).EndInit();
            splitContainer4.ResumeLayout(false);
            splitContainer3.Panel1.ResumeLayout(false);
            splitContainer3.Panel1.PerformLayout();
            splitContainer3.Panel2.ResumeLayout(false);
            splitContainer3.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer3).EndInit();
            splitContainer3.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripMenuItem;
        private SplitContainer splitContainer1;
        private SplitContainer splitContainer2;
        private SplitContainer splitContainer3;
        private Label label1;
        private TextBox regDisplay;
        private SplitContainer splitContainer4;
        private Label label2;
        private SplitContainer splitContainer5;
        private SplitContainer splitContainer6;
        private Label label3;
        private SplitContainer splitContainer7;
        private SplitContainer splitContainer8;
        private Label label4;
        private TextBox memoryAddr;
        private TextBox memory;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button run;
        private Button progress;
        private Button stop;
        private ToolStripMenuItem openToolStripMenuItem;
        private ToolStripMenuItem exitToolStripMenuItem;
        private ToolStripMenuItem settingsToolStripMenuItem;
        private ToolStripMenuItem registerRepToolStripMenuItem;
        private ToolStripMenuItem hexToolStripMenuItem;
        private ToolStripMenuItem decimalToolStripMenuItem;
        private OpenFileDialog openFileDialog;
        private RichTextBox disassemble;
        private Button runFor;
        private NumericUpDown runCnt;
        private ToolStripMenuItem showIntermediateStateToolStripMenuItem;
        private ToolStripMenuItem registerDataTypeToolStripMenuItem;
        private ToolStripMenuItem int32ToolStripMenuItem;
        private ToolStripMenuItem int64ToolStripMenuItem;
        private ToolStripMenuItem uint32ToolStripMenuItem;
        private ToolStripMenuItem uint64ToolStripMenuItem;
        private ToolStripMenuItem floatToolStripMenuItem;
        private ToolStripMenuItem doubleToolStripMenuItem;
        private ToolStripMenuItem windowsToolStripMenuItem;
        private ToolStripMenuItem vectorRegistersToolStripMenuItem;
    }
}