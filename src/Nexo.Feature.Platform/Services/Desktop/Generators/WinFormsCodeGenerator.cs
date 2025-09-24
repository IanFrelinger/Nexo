using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nexo.Feature.Platform.Models;

namespace Nexo.Feature.Platform.Services.Desktop.Generators
{
    public class WinFormsCodeGenerator
    {
        private readonly ILogger<WinFormsCodeGenerator> _logger;

        public WinFormsCodeGenerator(ILogger<WinFormsCodeGenerator> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<DesktopCodeGenerationResult> GenerateWinFormsCodeAsync(DesktopCodeGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("Generating WinForms code for {ApplicationType}", request.ApplicationType);

                var result = new DesktopCodeGenerationResult
                {
                    Platform = request.Platform,
                    ApplicationType = request.ApplicationType,
                    UIFramework = "WinForms",
                    Success = true
                };

                // Generate main form
                var mainForm = GenerateMainFormCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "MainForm.cs",
                    Content = mainForm,
                    FileType = "C#"
                });

                // Generate main form designer
                var mainFormDesigner = GenerateMainFormDesignerCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "MainForm.Designer.cs",
                    Content = mainFormDesigner,
                    FileType = "C#"
                });

                // Generate program.cs
                var program = GenerateProgramCode(request);
                result.GeneratedFiles.Add(new GeneratedFile
                {
                    FileName = "Program.cs",
                    Content = program,
                    FileType = "C#"
                });

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating WinForms code");
                return new DesktopCodeGenerationResult
                {
                    Success = false,
                    Errors = { $"WinForms code generation failed: {ex.Message}" }
                };
            }
        }

        private string GenerateMainFormCode(DesktopCodeGenerationRequest request)
        {
            return $@"using System;
using System.Windows.Forms;

namespace {request.ApplicationType}
{{
    public partial class MainForm : Form
    {{
        public MainForm()
        {{
            InitializeComponent();
        }}

        private void MainForm_Load(object sender, EventArgs e)
        {{
            // Form load logic
        }}

        private void button1_Click(object sender, EventArgs e)
        {{
            MessageBox.Show(""Hello from {request.ApplicationType}!"", ""Information"", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }}
    }}
}}";
        }

        private string GenerateMainFormDesignerCode(DesktopCodeGenerationRequest request)
        {
            return $@"namespace {request.ApplicationType}
{{
    partial class MainForm
    {{
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {{
            if (disposing && (components != null))
            {{
                components.Dispose();
            }}
            base.Dispose(disposing);
        }}

        private void InitializeComponent()
        {{
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 12);
            this.button1.Name = ""button1"";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = ""Click Me"";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.button1);
            this.Name = ""MainForm"";
            this.Text = ""{request.ApplicationType}"";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
        }}

        private System.Windows.Forms.Button button1;
    }}
}}";
        }

        private string GenerateProgramCode(DesktopCodeGenerationRequest request)
        {
            return $@"using System;
using System.Windows.Forms;

namespace {request.ApplicationType}
{{
    static class Program
    {{
        [STAThread]
        static void Main()
        {{
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }}
    }}
}}";
        }
    }
}
