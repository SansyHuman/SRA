namespace SRA_Debugger
{
    partial class StackSizeForm
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
            splitContainer1 = new SplitContainer();
            stackSize = new NumericUpDown();
            confirmButton = new Button();
            stackSizeUnit = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)stackSize).BeginInit();
            SuspendLayout();
            // 
            // splitContainer1
            // 
            splitContainer1.Dock = DockStyle.Fill;
            splitContainer1.Location = new Point(0, 0);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(stackSize);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(confirmButton);
            splitContainer1.Panel2.Controls.Add(stackSizeUnit);
            splitContainer1.Size = new Size(378, 194);
            splitContainer1.SplitterDistance = 201;
            splitContainer1.TabIndex = 0;
            // 
            // stackSize
            // 
            stackSize.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            stackSize.Location = new Point(81, 87);
            stackSize.Maximum = new decimal(new int[] { 10000, 0, 0, 0 });
            stackSize.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            stackSize.Name = "stackSize";
            stackSize.Size = new Size(117, 31);
            stackSize.TabIndex = 0;
            stackSize.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // confirmButton
            // 
            confirmButton.DialogResult = DialogResult.OK;
            confirmButton.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point);
            confirmButton.Location = new Point(42, 150);
            confirmButton.Margin = new Padding(10);
            confirmButton.Name = "confirmButton";
            confirmButton.Size = new Size(112, 34);
            confirmButton.TabIndex = 1;
            confirmButton.Text = "OK";
            confirmButton.UseVisualStyleBackColor = true;
            // 
            // stackSizeUnit
            // 
            stackSizeUnit.Anchor = AnchorStyles.Left;
            stackSizeUnit.FormattingEnabled = true;
            stackSizeUnit.Items.AddRange(new object[] { "Byte", "KB", "MB" });
            stackSizeUnit.Location = new Point(3, 85);
            stackSizeUnit.Name = "stackSizeUnit";
            stackSizeUnit.Size = new Size(121, 33);
            stackSizeUnit.TabIndex = 0;
            stackSizeUnit.Text = "Byte";
            // 
            // StackSizeForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(378, 194);
            Controls.Add(splitContainer1);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Name = "StackSizeForm";
            Text = "Set stack size";
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)stackSize).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer splitContainer1;
        private Button confirmButton;
        internal NumericUpDown stackSize;
        internal ComboBox stackSizeUnit;
    }
}