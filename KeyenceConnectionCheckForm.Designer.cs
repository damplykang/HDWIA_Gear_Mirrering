namespace WIA_ViewerProgram
{
    partial class KeyenceConnectionCheckForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            ResultTextBox = new TextBox();
            SuspendLayout();
            //
            // ResultTextBox
            //
            ResultTextBox.Location = new Point(12, 12);
            ResultTextBox.Multiline = true;
            ResultTextBox.Name = "ResultTextBox";
            ResultTextBox.ReadOnly = true;
            ResultTextBox.ScrollBars = ScrollBars.Vertical;
            ResultTextBox.Size = new Size(560, 320);
            ResultTextBox.TabIndex = 0;
            ResultTextBox.Font = new Font("맑은 고딕", 9F);
            //
            // KeyenceConnectionCheckForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(584, 344);
            Controls.Add(ResultTextBox);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "KeyenceConnectionCheckForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Keyence 연결 확인";
            Shown += KeyenceConnectionCheckForm_Shown;
            ResumeLayout(false);
            PerformLayout();
        }

        private TextBox ResultTextBox;
    }
}
