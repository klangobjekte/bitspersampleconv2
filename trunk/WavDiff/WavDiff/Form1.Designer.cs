﻿namespace WavDiff
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナ変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナで生成されたコード

        /// <summary>
        /// デザイナ サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディタで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.buttonStart = new System.Windows.Forms.Button();
            this.textBoxConsole = new System.Windows.Forms.TextBox();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttonRead1 = new System.Windows.Forms.Button();
            this.textBoxRead1 = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.buttonRead2 = new System.Windows.Forms.Button();
            this.textBoxRead2 = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.labelMagnitude = new System.Windows.Forms.Label();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.buttonWrite = new System.Windows.Forms.Button();
            this.textBoxWrite = new System.Windows.Forms.TextBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.checkBoxAutoAdjustVolumeDifference = new System.Windows.Forms.CheckBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonStart
            // 
            resources.ApplyResources(this.buttonStart, "buttonStart");
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            // 
            // textBoxConsole
            // 
            resources.ApplyResources(this.textBoxConsole, "textBoxConsole");
            this.textBoxConsole.Name = "textBoxConsole";
            this.textBoxConsole.ReadOnly = true;
            // 
            // backgroundWorker1
            // 
            this.backgroundWorker1.WorkerReportsProgress = true;
            this.backgroundWorker1.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker1_DoWork);
            this.backgroundWorker1.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker1_RunWorkerCompleted);
            this.backgroundWorker1.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker1_ProgressChanged);
            // 
            // progressBar1
            // 
            resources.ApplyResources(this.progressBar1, "progressBar1");
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttonRead1);
            this.groupBox1.Controls.Add(this.textBoxRead1);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // buttonRead1
            // 
            resources.ApplyResources(this.buttonRead1, "buttonRead1");
            this.buttonRead1.Name = "buttonRead1";
            this.buttonRead1.UseVisualStyleBackColor = true;
            this.buttonRead1.Click += new System.EventHandler(this.buttonRead1_Click);
            // 
            // textBoxRead1
            // 
            resources.ApplyResources(this.textBoxRead1, "textBoxRead1");
            this.textBoxRead1.Name = "textBoxRead1";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.buttonRead2);
            this.groupBox2.Controls.Add(this.textBoxRead2);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            this.groupBox2.Enter += new System.EventHandler(this.groupBox2_Enter);
            // 
            // buttonRead2
            // 
            resources.ApplyResources(this.buttonRead2, "buttonRead2");
            this.buttonRead2.Name = "buttonRead2";
            this.buttonRead2.UseVisualStyleBackColor = true;
            this.buttonRead2.Click += new System.EventHandler(this.buttonRead2_Click);
            // 
            // textBoxRead2
            // 
            resources.ApplyResources(this.textBoxRead2, "textBoxRead2");
            this.textBoxRead2.Name = "textBoxRead2";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.labelMagnitude);
            this.groupBox3.Controls.Add(this.numericUpDown1);
            this.groupBox3.Controls.Add(this.label3);
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // labelMagnitude
            // 
            resources.ApplyResources(this.labelMagnitude, "labelMagnitude");
            this.labelMagnitude.Name = "labelMagnitude";
            // 
            // numericUpDown1
            // 
            resources.ApplyResources(this.numericUpDown1, "numericUpDown1");
            this.numericUpDown1.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            this.numericUpDown1.KeyUp += new System.Windows.Forms.KeyEventHandler(this.numericUpDown1_KeyUp);
            this.numericUpDown1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.numericUpDown1_KeyPress);
            this.numericUpDown1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.numericUpDown1_KeyDown);
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.Name = "label3";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.buttonWrite);
            this.groupBox4.Controls.Add(this.textBoxWrite);
            resources.ApplyResources(this.groupBox4, "groupBox4");
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.TabStop = false;
            // 
            // buttonWrite
            // 
            resources.ApplyResources(this.buttonWrite, "buttonWrite");
            this.buttonWrite.Name = "buttonWrite";
            this.buttonWrite.UseVisualStyleBackColor = true;
            this.buttonWrite.Click += new System.EventHandler(this.buttonWrite_Click);
            // 
            // textBoxWrite
            // 
            resources.ApplyResources(this.textBoxWrite, "textBoxWrite");
            this.textBoxWrite.Name = "textBoxWrite";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.checkBoxAutoAdjustVolumeDifference);
            resources.ApplyResources(this.groupBox5, "groupBox5");
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.TabStop = false;
            // 
            // checkBoxAutoAdjustVolumeDifference
            // 
            resources.ApplyResources(this.checkBoxAutoAdjustVolumeDifference, "checkBoxAutoAdjustVolumeDifference");
            this.checkBoxAutoAdjustVolumeDifference.Name = "checkBoxAutoAdjustVolumeDifference";
            this.checkBoxAutoAdjustVolumeDifference.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.textBoxConsole);
            this.Controls.Add(this.buttonStart);
            this.Name = "Form1";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.TextBox textBoxConsole;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttonRead1;
        private System.Windows.Forms.TextBox textBoxRead1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button buttonRead2;
        private System.Windows.Forms.TextBox textBoxRead2;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label labelMagnitude;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Button buttonWrite;
        private System.Windows.Forms.TextBox textBoxWrite;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.CheckBox checkBoxAutoAdjustVolumeDifference;
    }
}

