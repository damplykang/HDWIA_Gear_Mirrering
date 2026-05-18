namespace WIA_ViewerProgram
{
    partial class KeyenceSettingForm
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
            IpLabel = new Label();
            IpTextBox = new TextBox();
            PortLabel = new Label();
            PortNumberTextBox = new TextBox();
            SaveButton = new Button();
            KeyenceCancelButton = new Button();
            ResetButton = new Button();
            SuspendLayout();
            //
            // IpLabel
            //
            IpLabel.AutoSize = true;
            IpLabel.Location = new Point(24, 28);
            IpLabel.Name = "IpLabel";
            IpLabel.Size = new Size(29, 15);
            IpLabel.TabIndex = 0;
            IpLabel.Text = "IP";
            //
            // IpTextBox
            //
            IpTextBox.Location = new Point(120, 24);
            IpTextBox.Name = "IpTextBox";
            IpTextBox.Size = new Size(260, 23);
            IpTextBox.TabIndex = 1;
            //
            // PortLabel
            //
            PortLabel.AutoSize = true;
            PortLabel.Location = new Point(24, 68);
            PortLabel.Name = "PortLabel";
            PortLabel.Size = new Size(72, 15);
            PortLabel.TabIndex = 2;
            PortLabel.Text = "Port 번호";
            //
            // PortNumberTextBox
            //
            PortNumberTextBox.Location = new Point(120, 64);
            PortNumberTextBox.Name = "PortNumberTextBox";
            PortNumberTextBox.Size = new Size(260, 23);
            PortNumberTextBox.TabIndex = 3;
            //
            // SaveButton
            //
            SaveButton.Location = new Point(24, 116);
            SaveButton.Name = "SaveButton";
            SaveButton.Size = new Size(100, 32);
            SaveButton.TabIndex = 4;
            SaveButton.Text = "저장";
            SaveButton.UseVisualStyleBackColor = true;
            SaveButton.Click += SaveButton_Click;
            //
            // KeyenceCancelButton
            //
            KeyenceCancelButton.Location = new Point(152, 116);
            KeyenceCancelButton.Name = "KeyenceCancelButton";
            KeyenceCancelButton.Size = new Size(100, 32);
            KeyenceCancelButton.TabIndex = 5;
            KeyenceCancelButton.Text = "취소";
            KeyenceCancelButton.UseVisualStyleBackColor = true;
            KeyenceCancelButton.CausesValidation = false;
            KeyenceCancelButton.Click += KeyenceCancelButton_Click;
            //
            // ResetButton
            //
            ResetButton.Location = new Point(280, 116);
            ResetButton.Name = "ResetButton";
            ResetButton.Size = new Size(100, 32);
            ResetButton.TabIndex = 6;
            ResetButton.Text = "초기화";
            ResetButton.UseVisualStyleBackColor = true;
            ResetButton.CausesValidation = false;
            ResetButton.Click += ResetButton_Click;
            //
            // KeyenceSettingForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(414, 171);
            Controls.Add(IpLabel);
            Controls.Add(IpTextBox);
            Controls.Add(PortLabel);
            Controls.Add(PortNumberTextBox);
            Controls.Add(SaveButton);
            Controls.Add(KeyenceCancelButton);
            Controls.Add(ResetButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "KeyenceSetting";
            StartPosition = FormStartPosition.CenterParent;
            Text = "KeyenceSetting";
            Load += KeyenceSettingForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private Label IpLabel;
        private TextBox IpTextBox;
        private Label PortLabel;
        private TextBox PortNumberTextBox;
        private Button SaveButton;
        private Button KeyenceCancelButton;
        private Button ResetButton;
    }
}
