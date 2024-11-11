namespace WinFormAutoDISample;

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
        button1 = new Button();
        label1 = new Label();
        statusStrip1 = new StatusStrip();
        tsslCpuTitle = new ToolStripStatusLabel();
        tsslRamTitle = new ToolStripStatusLabel();
        tsslRam = new ToolStripStatusLabel();
        tsslCpu = new ToolStripStatusLabel();
        statusStrip1.SuspendLayout();
        SuspendLayout();
        // 
        // button1
        // 
        button1.Location = new Point(329, 173);
        button1.Name = "button1";
        button1.Size = new Size(75, 23);
        button1.TabIndex = 0;
        button1.Text = "button1";
        button1.UseVisualStyleBackColor = true;
        button1.Click += button1_Click;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(235, 250);
        label1.Name = "label1";
        label1.Size = new Size(43, 17);
        label1.TabIndex = 1;
        label1.Text = "label1";
        // 
        // statusStrip1
        // 
        statusStrip1.Items.AddRange(new ToolStripItem[] { tsslCpuTitle, tsslCpu, tsslRamTitle, tsslRam });
        statusStrip1.Location = new Point(0, 415);
        statusStrip1.Name = "statusStrip1";
        statusStrip1.RenderMode = ToolStripRenderMode.Professional;
        statusStrip1.Size = new Size(682, 22);
        statusStrip1.TabIndex = 2;
        statusStrip1.Text = "statusStrip1";
        // 
        // tsslCpuTitle
        // 
        tsslCpuTitle.Name = "tsslCpuTitle";
        tsslCpuTitle.Size = new Size(35, 17);
        tsslCpuTitle.Text = "CPU:";
        // 
        // tsslRamTitle
        // 
        tsslRamTitle.Name = "tsslRamTitle";
        tsslRamTitle.Size = new Size(39, 17);
        tsslRamTitle.Text = "RAM:";
        // 
        // tsslRam
        // 
        tsslRam.Name = "tsslRam";
        tsslRam.Size = new Size(12, 17);
        tsslRam.Text = " ";
        // 
        // tsslCpu
        // 
        tsslCpu.Name = "tsslCpu";
        tsslCpu.Size = new Size(12, 17);
        tsslCpu.Text = " ";
        // 
        // MainForm
        // 
        AutoScaleDimensions = new SizeF(7F, 17F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(682, 437);
        Controls.Add(statusStrip1);
        Controls.Add(label1);
        Controls.Add(button1);
        Name = "MainForm";
        Text = "Form1";
        Load += MainForm_Load;
        statusStrip1.ResumeLayout(false);
        statusStrip1.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Button button1;
    private Label label1;
    private StatusStrip statusStrip1;
    private ToolStripStatusLabel tsslCpuTitle;

/* 项目“NugetPackManager (net8.0-windows)”的未合并的更改
在此之前:
    private ToolStripStatusLabel tsslRam;
    private ToolStripProgressBar tspbRam;
}
在此之后:
    private ToolStripStatusLabel tsslRamTitle;
    private ToolStripStatusLabel tsslRamValue;
}
*/
    private ToolStripStatusLabel tsslRamTitle;
    private ToolStripStatusLabel tsslRam;
    private ToolStripStatusLabel tsslCpu;
}
