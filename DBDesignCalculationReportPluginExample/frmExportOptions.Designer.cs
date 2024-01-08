using System;

namespace DesignReportPlugin
{
    partial class ExportOptions
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(Boolean disposing)
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
            this.lblDescription = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.gbOutputOptions = new System.Windows.Forms.GroupBox();
            this.rbAll = new System.Windows.Forms.RadioButton();
            this.cbCoincidentTCM = new System.Windows.Forms.ComboBox();
            this.labelCoincidentTCM = new System.Windows.Forms.Label();
            this.cbCoincidentCDM = new System.Windows.Forms.ComboBox();
            this.labelCoincidentCDM = new System.Windows.Forms.Label();
            this.labelNoncoincidentTCM = new System.Windows.Forms.Label();
            this.cbNoncoincidentTCM = new System.Windows.Forms.ComboBox();
            this.rbCoincident = new System.Windows.Forms.RadioButton();
            this.rbtnNoncoincident = new System.Windows.Forms.RadioButton();
            this.gbOutputOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblDescription
            // 
            this.lblDescription.Location = new System.Drawing.Point(11, 11);
            this.lblDescription.Name = "lblDescription";
            this.lblDescription.Size = new System.Drawing.Size(275, 23);
            this.lblDescription.TabIndex = 0;
            this.lblDescription.Text = "Choose cooling design tables to export";
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(105, 321);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(187, 321);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // gbOutputOptions
            // 
            this.gbOutputOptions.Controls.Add(this.rbAll);
            this.gbOutputOptions.Controls.Add(this.cbCoincidentTCM);
            this.gbOutputOptions.Controls.Add(this.labelCoincidentTCM);
            this.gbOutputOptions.Controls.Add(this.cbCoincidentCDM);
            this.gbOutputOptions.Controls.Add(this.labelCoincidentCDM);
            this.gbOutputOptions.Controls.Add(this.labelNoncoincidentTCM);
            this.gbOutputOptions.Controls.Add(this.cbNoncoincidentTCM);
            this.gbOutputOptions.Controls.Add(this.rbCoincident);
            this.gbOutputOptions.Controls.Add(this.rbtnNoncoincident);
            this.gbOutputOptions.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbOutputOptions.Location = new System.Drawing.Point(11, 41);
            this.gbOutputOptions.Name = "gbOutputOptions";
            this.gbOutputOptions.Size = new System.Drawing.Size(274, 270);
            this.gbOutputOptions.TabIndex = 13;
            this.gbOutputOptions.TabStop = false;
            this.gbOutputOptions.Text = "Output Options";
            // 
            // rbAll
            // 
            this.rbAll.AutoSize = true;
            this.rbAll.Location = new System.Drawing.Point(32, 238);
            this.rbAll.Name = "rbAll";
            this.rbAll.Size = new System.Drawing.Size(36, 17);
            this.rbAll.TabIndex = 10;
            this.rbAll.TabStop = true;
            this.rbAll.Text = "All";
            this.rbAll.UseVisualStyleBackColor = true;
            // 
            // cbCoincidentTCM
            // 
            this.cbCoincidentTCM.FormattingEnabled = true;
            this.cbCoincidentTCM.Items.AddRange(new object[] {
            "Building",
            "Cooling System",
            "Building and Cooling System"});
            this.cbCoincidentTCM.Location = new System.Drawing.Point(51, 198);
            this.cbCoincidentTCM.Name = "cbCoincidentTCM";
            this.cbCoincidentTCM.Size = new System.Drawing.Size(190, 21);
            this.cbCoincidentTCM.TabIndex = 9;
            this.cbCoincidentTCM.Text = "Building";
            // 
            // labelCoincidentTCM
            // 
            this.labelCoincidentTCM.AutoSize = true;
            this.labelCoincidentTCM.Location = new System.Drawing.Point(48, 182);
            this.labelCoincidentTCM.Name = "labelCoincidentTCM";
            this.labelCoincidentTCM.Size = new System.Drawing.Size(130, 13);
            this.labelCoincidentTCM.TabIndex = 8;
            this.labelCoincidentTCM.Text = "Totals Calculation Method";
            // 
            // cbCoincidentCDM
            // 
            this.cbCoincidentCDM.FormattingEnabled = true;
            this.cbCoincidentCDM.Items.AddRange(new object[] {
            "Building",
            "Cooling System",
            "Building and Cooling System"});
            this.cbCoincidentCDM.Location = new System.Drawing.Point(51, 143);
            this.cbCoincidentCDM.Name = "cbCoincidentCDM";
            this.cbCoincidentCDM.Size = new System.Drawing.Size(190, 21);
            this.cbCoincidentCDM.TabIndex = 7;
            this.cbCoincidentCDM.Text = "Building";
            // 
            // labelCoincidentCDM
            // 
            this.labelCoincidentCDM.AutoSize = true;
            this.labelCoincidentCDM.Location = new System.Drawing.Point(48, 127);
            this.labelCoincidentCDM.Name = "labelCoincidentCDM";
            this.labelCoincidentCDM.Size = new System.Drawing.Size(143, 13);
            this.labelCoincidentCDM.TabIndex = 6;
            this.labelCoincidentCDM.Text = "Coincident Definition Method";
            // 
            // labelNoncoincidentTCM
            // 
            this.labelNoncoincidentTCM.AutoSize = true;
            this.labelNoncoincidentTCM.Location = new System.Drawing.Point(48, 44);
            this.labelNoncoincidentTCM.Name = "labelNoncoincidentTCM";
            this.labelNoncoincidentTCM.Size = new System.Drawing.Size(130, 13);
            this.labelNoncoincidentTCM.TabIndex = 5;
            this.labelNoncoincidentTCM.Text = "Totals Calculation Method";
            // 
            // cbNoncoincidentTCM
            // 
            this.cbNoncoincidentTCM.FormattingEnabled = true;
            this.cbNoncoincidentTCM.Items.AddRange(new object[] {
            "Building",
            "Cooling System",
            "Building and Cooling System"});
            this.cbNoncoincidentTCM.Location = new System.Drawing.Point(51, 60);
            this.cbNoncoincidentTCM.Name = "cbNoncoincidentTCM";
            this.cbNoncoincidentTCM.Size = new System.Drawing.Size(190, 21);
            this.cbNoncoincidentTCM.TabIndex = 4;
            this.cbNoncoincidentTCM.Text = "Building";
            // 
            // rbCoincident
            // 
            this.rbCoincident.AutoSize = true;
            this.rbCoincident.Location = new System.Drawing.Point(32, 103);
            this.rbCoincident.Name = "rbCoincident";
            this.rbCoincident.Size = new System.Drawing.Size(75, 17);
            this.rbCoincident.TabIndex = 2;
            this.rbCoincident.TabStop = true;
            this.rbCoincident.Text = "Coincident";
            this.rbCoincident.UseVisualStyleBackColor = true;
            // 
            // rbtnNoncoincident
            // 
            this.rbtnNoncoincident.AutoSize = true;
            this.rbtnNoncoincident.Checked = true;
            this.rbtnNoncoincident.Location = new System.Drawing.Point(32, 20);
            this.rbtnNoncoincident.Name = "rbtnNoncoincident";
            this.rbtnNoncoincident.Size = new System.Drawing.Size(97, 17);
            this.rbtnNoncoincident.TabIndex = 0;
            this.rbtnNoncoincident.TabStop = true;
            this.rbtnNoncoincident.Text = "Non-coincident";
            this.rbtnNoncoincident.UseVisualStyleBackColor = true;
            // 
            // ExportOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(298, 356);
            this.ControlBox = false;
            this.Controls.Add(this.gbOutputOptions);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lblDescription);
            this.Name = "ExportOptions";
            this.Text = "Export Options";
            this.gbOutputOptions.ResumeLayout(false);
            this.gbOutputOptions.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox gbOutputOptions;
        private System.Windows.Forms.RadioButton rbtnNoncoincident;
        private System.Windows.Forms.RadioButton rbAll;
        private System.Windows.Forms.ComboBox cbCoincidentTCM;
        private System.Windows.Forms.Label labelCoincidentTCM;
        private System.Windows.Forms.ComboBox cbCoincidentCDM;
        private System.Windows.Forms.Label labelCoincidentCDM;
        private System.Windows.Forms.Label labelNoncoincidentTCM;
        private System.Windows.Forms.ComboBox cbNoncoincidentTCM;
        private System.Windows.Forms.RadioButton rbCoincident;
    }
}

