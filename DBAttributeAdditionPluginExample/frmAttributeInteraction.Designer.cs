namespace Attribute_Addition_Plugin
{
    partial class frmAttributeInteraction
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
            this.btnGet = new System.Windows.Forms.Button();
            this.btnSet = new System.Windows.Forms.Button();
            this.lblExplain = new System.Windows.Forms.Label();
            this.lblMessage = new System.Windows.Forms.Label();
            this.lblNewValue = new System.Windows.Forms.Label();
            this.txtNewValue = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // btnGet
            // 
            this.btnGet.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
            this.btnGet.Location = new System.Drawing.Point(12, 105);
            this.btnGet.Name = "btnGet";
            this.btnGet.Size = new System.Drawing.Size(75, 23);
            this.btnGet.TabIndex = 0;
            this.btnGet.Text = "Get Value";
            this.btnGet.UseVisualStyleBackColor = true;
            this.btnGet.Click += new System.EventHandler(this.btnGet_Click);
            // 
            // btnSet
            // 
            this.btnSet.Location = new System.Drawing.Point(205, 105);
            this.btnSet.Name = "btnSet";
            this.btnSet.Size = new System.Drawing.Size(75, 23);
            this.btnSet.TabIndex = 1;
            this.btnSet.Text = "Set Value";
            this.btnSet.UseVisualStyleBackColor = true;
            this.btnSet.Click += new System.EventHandler(this.btnSet_Click);
            // 
            // lblExplain
            // 
            this.lblExplain.Location = new System.Drawing.Point(12, 9);
            this.lblExplain.Name = "lblExplain";
            this.lblExplain.Size = new System.Drawing.Size(268, 32);
            this.lblExplain.TabIndex = 2;
            this.lblExplain.Text = "This dialog interacts with DBExampleAttribute, allowing you to retrieve and set t" +
    "he attribute\'s value";
            // 
            // lblMessage
            // 
            this.lblMessage.Location = new System.Drawing.Point(12, 41);
            this.lblMessage.Name = "lblMessage";
            this.lblMessage.Size = new System.Drawing.Size(258, 31);
            this.lblMessage.TabIndex = 3;
            this.lblMessage.Text = "Current Value: ";
            // 
            // lblNewValue
            // 
            this.lblNewValue.AutoSize = true;
            this.lblNewValue.Location = new System.Drawing.Point(13, 76);
            this.lblNewValue.Name = "lblNewValue";
            this.lblNewValue.Size = new System.Drawing.Size(65, 13);
            this.lblNewValue.TabIndex = 4;
            this.lblNewValue.Text = "New Value: ";
            // 
            // txtNewValue
            // 
            this.txtNewValue.Location = new System.Drawing.Point(85, 73);
            this.txtNewValue.Name = "txtNewValue";
            this.txtNewValue.Size = new System.Drawing.Size(195, 20);
            this.txtNewValue.TabIndex = 5;
            // 
            // frmAttributeInteraction
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 140);
            this.Controls.Add(this.txtNewValue);
            this.Controls.Add(this.lblNewValue);
            this.Controls.Add(this.lblMessage);
            this.Controls.Add(this.lblExplain);
            this.Controls.Add(this.btnSet);
            this.Controls.Add(this.btnGet);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "frmAttributeInteraction";
            this.Text = "Example Attribute Interaction";
            this.Load += new System.EventHandler(this.frmAttributeInteraction_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGet;
        private System.Windows.Forms.Button btnSet;
        private System.Windows.Forms.Label lblExplain;
        private System.Windows.Forms.Label lblMessage;
        private System.Windows.Forms.Label lblNewValue;
        private System.Windows.Forms.TextBox txtNewValue;
    }
}

