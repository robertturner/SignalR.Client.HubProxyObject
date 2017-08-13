namespace SignalR.Client.HubProxyObject.DemoUIServer
{
    partial class HostForm
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
            this.buttonCallSig = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonCallSig
            // 
            this.buttonCallSig.Location = new System.Drawing.Point(12, 12);
            this.buttonCallSig.Name = "buttonCallSig";
            this.buttonCallSig.Size = new System.Drawing.Size(75, 23);
            this.buttonCallSig.TabIndex = 0;
            this.buttonCallSig.Text = "Call Sig";
            this.buttonCallSig.UseVisualStyleBackColor = true;
            this.buttonCallSig.Click += new System.EventHandler(this.buttonCallSig_Click);
            // 
            // HostForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(476, 356);
            this.Controls.Add(this.buttonCallSig);
            this.Name = "HostForm";
            this.Text = "HostForm";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonCallSig;
    }
}

