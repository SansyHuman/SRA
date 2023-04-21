using SRA_Simulator;

using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Text;

namespace SRA_Debugger
{
    public partial class MainForm : Form
    {
        private CPU cpu;
        private ulong currMemAddr;
        private string[] asm;
        private int[] instPos;
        private ulong currInstIndex;

        VectorRegisterForm vectorRegShowWindow;

        private Task programRunTask;
        private CancellationTokenSource cancelTokenSource;

        [DllImport("kernel32.dll")]
        public static extern int AllocConsole();

        [DllImport("kernel32.dll")]
        public static extern int FreeConsole();

        public MainForm()
        {
            AllocConsole();

            InitializeComponent();

            MaximizeBox = false;
            MinimizeBox = false;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void ShowMemory(ulong vaddr)
        {
            if (cpu == null)
            {
                return;
            }

            string dataMem = cpu.GetMemoryString(vaddr, 6, 32);

            memory.Text = dataMem;
            memoryAddr.Text = $"0x{vaddr:x16}";
            currMemAddr = vaddr;
        }

        private void ShowDisassemble(bool initialize)
        {
            if (cpu == null)
            {
                return;
            }

            ulong currInstIndex = (cpu.Registers.PC - cpu.Entrypoint) / 4;

            if (initialize)
            {
                disassemble.Clear();

                instPos = new int[asm.Length];

                for (ulong i = 0; i < (ulong)asm.Length; i++)
                {
                    disassemble.SelectionStart = disassemble.TextLength;
                    disassemble.SelectionLength = 0;
                    disassemble.SelectionColor = Color.Gray;
                    disassemble.AppendText($"{cpu.TextSegmentStart + i * 4:x16}:\t\t");

                    instPos[i] = disassemble.TextLength;
                    if (i == currInstIndex)
                    {
                        disassemble.SelectionStart = disassemble.TextLength;
                        disassemble.SelectionLength = 0;
                        disassemble.SelectionColor = Color.DarkRed;
                        disassemble.AppendText(asm[i]);
                    }
                    else
                    {
                        disassemble.SelectionStart = disassemble.TextLength;
                        disassemble.SelectionLength = 0;
                        disassemble.SelectionColor = Color.Black;
                        disassemble.AppendText(asm[i]);
                    }

                    disassemble.AppendText(Environment.NewLine);
                }

                disassemble.SelectionStart = instPos[currInstIndex];
                disassemble.ScrollToCaret();
            }
            else
            {
                disassemble.SelectionStart = instPos[this.currInstIndex];
                disassemble.SelectionLength = asm[this.currInstIndex].Length;
                disassemble.SelectionColor = Color.Black;

                disassemble.SelectionStart = instPos[currInstIndex];
                disassemble.SelectionLength = asm[currInstIndex].Length;
                disassemble.SelectionColor = Color.DarkRed;

                disassemble.SelectionStart = instPos[currInstIndex];
                disassemble.ScrollToCaret();
            }

            this.currInstIndex = currInstIndex;
        }

        private void ShowRegisters()
        {
            string RegValToString(ulong regVal)
            {
                if (int32ToolStripMenuItem.Checked)
                {
                    return $"{(int)(uint)regVal}";
                }
                else if (int64ToolStripMenuItem.Checked)
                {
                    return $"{(long)regVal}";
                }
                else if (uint32ToolStripMenuItem.Checked)
                {
                    return hexToolStripMenuItem.Checked ? $"{(uint)regVal:x8}" : $"{(uint)regVal}";
                }
                else if (uint64ToolStripMenuItem.Checked)
                {
                    return hexToolStripMenuItem.Checked ? $"{regVal:x16}" : $"{regVal}";
                }
                else if (floatToolStripMenuItem.Checked)
                {
                    return $"{BitConverter.ToSingle(BitConverter.GetBytes((uint)regVal))}";
                }
                else if (doubleToolStripMenuItem.Checked)
                {
                    return $"{BitConverter.ToDouble(BitConverter.GetBytes(regVal))}";
                }

                return hexToolStripMenuItem.Checked ? $"{regVal:x16}" : $"{regVal}";
            }

            if (cpu == null)
            {
                return;
            }

            StringBuilder str = new StringBuilder();
            for (int i = 0; i < 16; i++)
            {
                str.Append($"{Disassembler.regnumToName[i]}:\t");
                str.Append(RegValToString(cpu.Registers[i]));

                str.Append("\t");

                str.Append($"{Disassembler.regnumToName[i + 16]}:\t");
                str.Append(RegValToString(cpu.Registers[i + 16]));

                str.AppendLine();
            }

            str.Append("%hi:\t");
            str.Append(RegValToString(cpu.Registers.Hi));

            str.Append("\t");

            str.Append("%lo:\t");
            str.Append(RegValToString(cpu.Registers.Lo));

            str.AppendLine();

            str.Append("%pc:\t");
            str.Append(hexToolStripMenuItem.Checked ? $"{cpu.Registers.PC:x16}" : $"{(long)cpu.Registers.PC}");

            regDisplay.Text = str.ToString();

            if (vectorRegShowWindow != null && !vectorRegShowWindow.IsDisposed)
            {
                vectorRegShowWindow.ShowRegisters();
            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filename = openFileDialog.FileName;

                try
                {
                    StackSizeForm stackSizeForm = new StackSizeForm();
                    if (stackSizeForm.ShowDialog(this) == DialogResult.OK)
                    {
                        ulong stackSize = decimal.ToUInt64(stackSizeForm.stackSize.Value);
                        if (stackSizeForm.stackSizeUnit.Text == "KB")
                        {
                            stackSize *= 1024;
                        }
                        else if (stackSizeForm.stackSizeUnit.Text == "MB")
                        {
                            stackSize *= (1024 * 1024);
                        }

                        cpu = new CPU(filename, stackSize);
                        cpu.TerminateOnExit = false;
                        asm = Disassembler.Disassemble(cpu.TextBinary, cpu.TextSegmentStart);

                        if (vectorRegShowWindow != null && !vectorRegShowWindow.IsDisposed)
                        {
                            vectorRegShowWindow.CPU = cpu;
                        }

                        cpu.Reset();
                        ShowMemory(0x10_0000_0000UL);
                        ShowDisassemble(true);
                        ShowRegisters();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Error in loading program: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void hexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            hexToolStripMenuItem.Checked = true;
            decimalToolStripMenuItem.Checked = false;
            ShowRegisters();
        }

        private void decimalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            hexToolStripMenuItem.Checked = false;
            decimalToolStripMenuItem.Checked = true;
            ShowRegisters();
        }

        private void memoryAddr_KeyDown(object sender, KeyEventArgs e)
        {
            if (programRunTask != null && !programRunTask.IsCompleted)
            {
                return;
            }

            if (e.KeyCode == Keys.Enter)
            {
                try
                {
                    ulong vaddr = Convert.ToUInt64(memoryAddr.Text, 16);
                    ShowMemory(vaddr);
                }
                catch (Exception)
                {
                    MessageBox.Show(this, "Wrong address format", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void progress_Click(object sender, EventArgs e)
        {
            if (cpu == null)
            {
                return;
            }

            if (programRunTask != null && !programRunTask.IsCompleted)
            {
                return;
            }

            try
            {
                cpu.Clock();
                if (cpu.ExitCode.HasValue)
                {
                    MessageBox.Show(this, $"Program is terminated with exit code {cpu.ExitCode.Value}. Reset the program", "Program terminated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    cpu.Reset();
                }
                ShowMemory(currMemAddr);
                ShowDisassemble(false);
                ShowRegisters();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Error in running program: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void stop_Click(object sender, EventArgs e)
        {
            if (cpu == null)
            {
                return;
            }

            if (programRunTask != null && !programRunTask.IsCompleted)
            {
                cancelTokenSource.Cancel();
            }

            cpu.Reset();

            ShowMemory(currMemAddr);
            ShowDisassemble(false);
            ShowRegisters();
        }

        private void run_Click(object sender, EventArgs e)
        {
            if (cpu == null)
            {
                return;
            }

            if (programRunTask != null && !programRunTask.IsCompleted)
            {
                return;
            }

            if (cancelTokenSource != null)
            {
                cancelTokenSource.Dispose();
            }

            cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;

            programRunTask = new Task(() => RunProgramTillEnd(token), token);
            programRunTask.Start();
        }

        private void RunProgramTillEnd(CancellationToken token)
        {
            try
            {
                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    cpu.Clock();
                    if (cpu.ExitCode.HasValue)
                    {
                        Invoke(() =>
                        {
                            MessageBox.Show(this, $"Program is terminated with exit code {cpu.ExitCode.Value}. Reset the program", "Program terminated", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            cpu.Reset();
                            ShowMemory(currMemAddr);
                            ShowDisassemble(false);
                            ShowRegisters();
                        });
                        break;
                    }

                    if (showIntermediateStateToolStripMenuItem.Checked)
                    {
                        Invoke(() =>
                        {
                            ShowMemory(currMemAddr);
                            ShowDisassemble(false);
                            ShowRegisters();
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Invoke(() =>
                {
                    MessageBox.Show(this, $"Error in running program: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        }

        private void runFor_Click(object sender, EventArgs e)
        {
            if (cpu == null)
            {
                return;
            }

            if (programRunTask != null && !programRunTask.IsCompleted)
            {
                return;
            }

            if (cancelTokenSource != null)
            {
                cancelTokenSource.Dispose();
            }

            int cnt = decimal.ToInt32(runCnt.Value);

            cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;

            programRunTask = new Task(() => RunProgramFor(cnt, token), token);
            programRunTask.Start();
        }

        private void RunProgramFor(int cnt, CancellationToken token)
        {
            try
            {
                for (int i = 0; i < cnt; i++)
                {
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    cpu.Clock();
                    if (cpu.ExitCode.HasValue)
                    {
                        Invoke(() =>
                        {
                            MessageBox.Show(this, $"Program is terminated with exit code {cpu.ExitCode.Value}. Reset the program", "Program terminated", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            cpu.Reset();
                            ShowMemory(currMemAddr);
                            ShowDisassemble(false);
                            ShowRegisters();
                        });
                        break;
                    }

                    if (showIntermediateStateToolStripMenuItem.Checked)
                    {
                        Invoke(() =>
                        {
                            ShowMemory(currMemAddr);
                            ShowDisassemble(false);
                            ShowRegisters();
                        });
                    }
                }

                if (!cpu.ExitCode.HasValue)
                {
                    Invoke(() =>
                    {
                        ShowMemory(currMemAddr);
                        ShowDisassemble(false);
                        ShowRegisters();
                    });
                }
            }
            catch (OperationCanceledException)
            {

            }
            catch (Exception ex)
            {
                Invoke(() =>
                {
                    MessageBox.Show(this, $"Error in running program: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
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

        private void vectorRegistersToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (vectorRegShowWindow == null || vectorRegShowWindow.IsDisposed)
            {
                vectorRegShowWindow = new VectorRegisterForm(cpu);
                vectorRegShowWindow.Show();
                vectorRegShowWindow.ShowRegisters();
            }
        }
    }
}