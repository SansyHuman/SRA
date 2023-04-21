using SRA_Simulator;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SRA_Debugger
{
    public partial class VectorRegisterForm : Form
    {
        private CPU cpu;

        internal CPU CPU
        {
            set
            {
                cpu = value;
            }
        }

        public VectorRegisterForm(CPU cpu)
        {
            InitializeComponent();

            MaximizeBox = false;
            MinimizeBox = false;

            this.cpu = cpu;
        }

        internal void ShowRegisters()
        {
            string VRegValToString(Vector256<ulong> vreg)
            {
                if (int32ToolStripMenuItem.Checked)
                {
                    return vreg.AsInt32().ToString();
                }
                else if (int64ToolStripMenuItem.Checked)
                {
                    return vreg.AsInt64().ToString();
                }
                else if (uint32ToolStripMenuItem.Checked)
                {
                    return vreg.AsUInt32().ToString();
                }
                else if (uint64ToolStripMenuItem.Checked)
                {
                    return vreg.AsUInt64().ToString();
                }
                else if (floatToolStripMenuItem.Checked)
                {
                    return vreg.AsSingle().ToString();
                }
                else if (doubleToolStripMenuItem.Checked)
                {
                    return vreg.AsDouble().ToString();
                }

                return vreg.AsUInt64().ToString();
            }

            if (cpu == null)
            {
                return;
            }

            StringBuilder str = new StringBuilder();
            for (int i = 0; i < 32; i++)
            {
                str.Append($"%v{i}:\t");
                str.Append(VRegValToString(cpu.VectorRegisters[i]));

                str.AppendLine();
            }

            vectorRegDisplay.Text = str.ToString();
        }

        private void int32ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int32ToolStripMenuItem.Checked = true;
            int64ToolStripMenuItem.Checked = false;
            uint32ToolStripMenuItem.Checked = false;
            uint64ToolStripMenuItem.Checked = false;
            floatToolStripMenuItem.Checked = false;
            doubleToolStripMenuItem.Checked = false;
            ShowRegisters();
        }

        private void int64ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int32ToolStripMenuItem.Checked = false;
            int64ToolStripMenuItem.Checked = true;
            uint32ToolStripMenuItem.Checked = false;
            uint64ToolStripMenuItem.Checked = false;
            floatToolStripMenuItem.Checked = false;
            doubleToolStripMenuItem.Checked = false;
            ShowRegisters();
        }

        private void uint32ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int32ToolStripMenuItem.Checked = false;
            int64ToolStripMenuItem.Checked = false;
            uint32ToolStripMenuItem.Checked = true;
            uint64ToolStripMenuItem.Checked = false;
            floatToolStripMenuItem.Checked = false;
            doubleToolStripMenuItem.Checked = false;
            ShowRegisters();
        }

        private void uint64ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int32ToolStripMenuItem.Checked = false;
            int64ToolStripMenuItem.Checked = false;
            uint32ToolStripMenuItem.Checked = false;
            uint64ToolStripMenuItem.Checked = true;
            floatToolStripMenuItem.Checked = false;
            doubleToolStripMenuItem.Checked = false;
            ShowRegisters();
        }

        private void floatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int32ToolStripMenuItem.Checked = false;
            int64ToolStripMenuItem.Checked = false;
            uint32ToolStripMenuItem.Checked = false;
            uint64ToolStripMenuItem.Checked = false;
            floatToolStripMenuItem.Checked = true;
            doubleToolStripMenuItem.Checked = false;
            ShowRegisters();
        }

        private void doubleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int32ToolStripMenuItem.Checked = false;
            int64ToolStripMenuItem.Checked = false;
            uint32ToolStripMenuItem.Checked = false;
            uint64ToolStripMenuItem.Checked = false;
            floatToolStripMenuItem.Checked = false;
            doubleToolStripMenuItem.Checked = true;
            ShowRegisters();
        }

        private void VectorRegisterForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
    }
}
