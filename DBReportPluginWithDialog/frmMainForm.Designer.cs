namespace DBReportPluginWithDialog
{
    partial class MainForm
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
            this.lblExplain = new System.Windows.Forms.Label();
            this.cbGains = new System.Windows.Forms.CheckBox();
            this.cbAirflow = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cbPolygons = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lblExplain
            // 
            this.lblExplain.Location = new System.Drawing.Point(10, 10);
            this.lblExplain.Name = "lblExplain";
            this.lblExplain.Size = new System.Drawing.Size(360, 39);
            this.lblExplain.TabIndex = 0;
            this.lblExplain.Text = "This is an example plugin that scans the model and compiles a report of model dat" +
    "a. Please use these options to define which data you want included.";
            // 
            // cbGains
            // 
            this.cbGains.AutoSize = true;
            this.cbGains.Location = new System.Drawing.Point(33, 87);
            this.cbGains.Name = "cbGains";
            this.cbGains.Size = new System.Drawing.Size(91, 17);
            this.cbGains.TabIndex = 1;
            this.cbGains.Text = "Include Gains";
            this.cbGains.UseVisualStyleBackColor = true;
            // 
            // cbAirflow
            // 
            this.cbAirflow.AutoSize = true;
            this.cbAirflow.Location = new System.Drawing.Point(33, 110);
            this.cbAirflow.Name = "cbAirflow";
            this.cbAirflow.Size = new System.Drawing.Size(95, 17);
            this.cbAirflow.TabIndex = 2;
            this.cbAirflow.Text = "Include Airflow";
            this.cbAirflow.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(60, 150);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 5;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(235, 150);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // cbPolygons
            // 
            this.cbPolygons.AutoSize = true;
            this.cbPolygons.Location = new System.Drawing.Point(33, 64);
            this.cbPolygons.Name = "cbPolygons";
            this.cbPolygons.Size = new System.Drawing.Size(107, 17);
            this.cbPolygons.TabIndex = 7;
            this.cbPolygons.Text = "Include Polygons";
            this.cbPolygons.UseVisualStyleBackColor = true;
            this.cbPolygons.CheckedChanged += new System.EventHandler(this.cbPolygons_CheckedChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(384, 192);
            this.ControlBox = false;
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.cbAirflow);
            this.Controls.Add(this.cbGains);
            this.Controls.Add(this.lblExplain);
            this.Controls.Add(this.cbPolygons);
            this.Name = "MainForm";
            this.Text = "Example Plugin";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblExplain;
        private System.Windows.Forms.CheckBox cbGains;
        private System.Windows.Forms.CheckBox cbAirflow;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox cbPolygons;
    }
}

