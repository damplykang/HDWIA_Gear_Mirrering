namespace WIA_ViewerProgram
{
    partial class PLCSettingForm
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
            StationLabel = new Label();
            StationNumberTextBox = new TextBox();
            MoniteringCycleLabel = new Label();
            MoniteringCycleTextBox = new TextBox();
            MoniterAdrressLabel = new Label();
            MoniterAdrressTextBox = new TextBox();
            SaveButton = new Button();
            PLCCancelButton = new Button();
            ResetButton = new Button();
            SuspendLayout();
            // 
            // IpLabel
            // 
            IpLabel.AutoSize = true;
            IpLabel.Location = new Point(24, 28);
            IpLabel.Name = "IpLabel";
            IpLabel.Size = new Size(17, 15);
            IpLabel.TabIndex = 0;
            IpLabel.Text = "IP";
            // 
            // IpTextBox
            // 
            IpTextBox.Location = new Point(144, 24);
            IpTextBox.Name = "IpTextBox";
            IpTextBox.Size = new Size(260, 23);
            IpTextBox.TabIndex = 1;
            // 
            // StationLabel
            // 
            StationLabel.AutoSize = true;
            StationLabel.Location = new Point(24, 68);
            StationLabel.Name = "StationLabel";
            StationLabel.Size = new Size(73, 15);
            StationLabel.TabIndex = 2;
            StationLabel.Text = "Station 번호";
            // 
            // StationNumberTextBox
            // 
            StationNumberTextBox.Location = new Point(144, 64);
            StationNumberTextBox.Name = "StationNumberTextBox";
            StationNumberTextBox.Size = new Size(260, 23);
            StationNumberTextBox.TabIndex = 3;
            // 
            // MoniteringCycleLabel
            // 
            MoniteringCycleLabel.AutoSize = true;
            MoniteringCycleLabel.Location = new Point(24, 108);
            MoniteringCycleLabel.Name = "MoniteringCycleLabel";
            MoniteringCycleLabel.Size = new Size(119, 15);
            MoniteringCycleLabel.TabIndex = 7;
            MoniteringCycleLabel.Text = "MoniteringCycle(ms)";
            // 
            // MoniteringCycleTextBox
            // 
            MoniteringCycleTextBox.Location = new Point(144, 104);
            MoniteringCycleTextBox.Name = "MoniteringCycleTextBox";
            MoniteringCycleTextBox.Size = new Size(260, 23);
            MoniteringCycleTextBox.TabIndex = 8;
            // 
            // MoniterAdrressLabel
            // 
            MoniterAdrressLabel.AutoSize = true;
            MoniterAdrressLabel.Location = new Point(24, 148);
            MoniterAdrressLabel.Name = "MoniterAdrressLabel";
            MoniterAdrressLabel.Size = new Size(88, 15);
            MoniterAdrressLabel.TabIndex = 9;
            MoniterAdrressLabel.Text = "MoniterAdrress";
            // 
            // MoniterAdrressTextBox
            // 
            MoniterAdrressTextBox.Location = new Point(144, 144);
            MoniterAdrressTextBox.Name = "MoniterAdrressTextBox";
            MoniterAdrressTextBox.Size = new Size(260, 23);
            MoniterAdrressTextBox.TabIndex = 10;
            // 
            // SaveButton
            // 
            SaveButton.Location = new Point(24, 196);
            SaveButton.Name = "SaveButton";
            SaveButton.Size = new Size(100, 32);
            SaveButton.TabIndex = 4;
            SaveButton.Text = "저장";
            SaveButton.UseVisualStyleBackColor = true;
            SaveButton.Click += SaveButton_Click;
            // 
            // PLCCancelButton
            // 
            PLCCancelButton.CausesValidation = false;
            PLCCancelButton.Location = new Point(172, 196);
            PLCCancelButton.Name = "PLCCancelButton";
            PLCCancelButton.Size = new Size(100, 32);
            PLCCancelButton.TabIndex = 5;
            PLCCancelButton.Text = "취소";
            PLCCancelButton.UseVisualStyleBackColor = true;
            PLCCancelButton.Click += PLCCancelButton_Click;
            // 
            // ResetButton
            // 
            ResetButton.CausesValidation = false;
            ResetButton.Location = new Point(304, 196);
            ResetButton.Name = "ResetButton";
            ResetButton.Size = new Size(100, 32);
            ResetButton.TabIndex = 6;
            ResetButton.Text = "초기화";
            ResetButton.UseVisualStyleBackColor = true;
            ResetButton.Click += ResetButton_Click;
            // 
            // PLCSettingForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(434, 251);
            Controls.Add(IpLabel);
            Controls.Add(IpTextBox);
            Controls.Add(StationLabel);
            Controls.Add(StationNumberTextBox);
            Controls.Add(MoniteringCycleLabel);
            Controls.Add(MoniteringCycleTextBox);
            Controls.Add(MoniterAdrressLabel);
            Controls.Add(MoniterAdrressTextBox);
            Controls.Add(SaveButton);
            Controls.Add(PLCCancelButton);
            Controls.Add(ResetButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "PLCSettingForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "PLCSetting";
            Load += PLCSettingForm_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        private Label IpLabel;
        private TextBox IpTextBox;
        private Label StationLabel;
        private TextBox StationNumberTextBox;
        private Label MoniteringCycleLabel;
        private TextBox MoniteringCycleTextBox;
        private Label MoniterAdrressLabel;
        private TextBox MoniterAdrressTextBox;
        private Button SaveButton;
        private Button PLCCancelButton;
        private Button ResetButton;
    }
}
