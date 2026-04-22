namespace TimerOverlay
{
    partial class Form1
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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            lblTimer = new Label();
            timerTick = new System.Windows.Forms.Timer(components);
            SuspendLayout();
            // 
            // lblTimer
            // 
            lblTimer.Dock = DockStyle.Fill;
            lblTimer.Font = new Font("Calibri", 40F, FontStyle.Bold);
            lblTimer.ForeColor = SystemColors.MenuHighlight;
            lblTimer.Location = new Point(0, 0);
            lblTimer.Margin = new Padding(0);
            lblTimer.Name = "lblTimer";
            lblTimer.Size = new Size(220, 65);
            lblTimer.TabIndex = 0;
            lblTimer.Text = "0:00";
            lblTimer.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // timerTick
            // 
            timerTick.Enabled = true;
            timerTick.Interval = 8;
            timerTick.Tick += timerTick_Tick_1;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Black;
            ClientSize = new Size(220, 65);
            Controls.Add(lblTimer);
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Location = new Point(60, 60);
            Name = "Form1";
            Opacity = 0.92D;
            StartPosition = FormStartPosition.Manual;
            Text = "Forsaken Timer made by Lusk";
            TopMost = true;
            Load += Form1_Load;
            ResumeLayout(false);
        }

        #endregion

        private Label lblTimer;
        private System.Windows.Forms.Timer timerTick;
    }
}
