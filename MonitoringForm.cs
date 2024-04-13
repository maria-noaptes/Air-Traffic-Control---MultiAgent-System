
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;


namespace Reactive
{
    public partial class MonitoringForm : Form
    {
        private PlanetAgent _ownerAgent;
        DataTable dt = new DataTable();
        private DataGridView dataGridView1;
        public static Dictionary<string, Brush> colors = new Dictionary<string, Brush>();
        private Chart chart1;
        private Label label1;
        private Button button1;
        private Label conflicts;
        private Label totalConflicts;
        private Label round;
        private Label config;
        private int iteration = 0;

        public MonitoringForm()
        {
            InitializeComponent();

            dt.Columns.Add("Airplane", typeof(string));
            dt.Columns.Add("Last Speed Recorded", typeof(string));
            dt.Columns.Add("State", typeof(string));

            dataGridView1.DataSource = dt;
            this.chart1.Series.Clear();
        }

        public void SetOwner(PlanetAgent a)
        {
            _ownerAgent = a;
        }

        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawMonitoringView();
        }

        public void UpdatePlanetGUI()
        {
            DrawMonitoringView();
        }

        private void pictureBox_Resize(object sender, EventArgs e)
        {
            DrawMonitoringView();
        }

        private bool checkAirplaneInGrid(string targetValue)
        {

            bool containsValue = false;

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                // Assuming the first column contains your desired value
                if (row.Cells.Count > 0 && row.Cells[0].Value != null)
                {
                    string cellValue = row.Cells[0].Value.ToString();
                    if (cellValue.Equals(targetValue))
                    {
                        containsValue = true;
                        break; // Stop searching once the value is found
                    }
                }
            }
            return containsValue;
        }

        private void DrawMonitoringView()
        {
            int c1 = dataGridView1.Rows.Count - 1;
            this.Invoke((MethodInvoker)delegate
            {
                for (int i = 0; i < c1; i++)
                {
                    string airplaneName = "airplane" + i.ToString();
                    if (dataGridView1.Rows[i] != null && _ownerAgent.AirplanesSpeed.ContainsKey(airplaneName) && !chart1.Series.IsUniqueName(airplaneName))
                    {
                        changeSpeedInGridView(i, Math.Round(_ownerAgent.AirplanesSpeed[airplaneName], 2)); 
                        changeSpeedInChart(airplaneName, Math.Round(_ownerAgent.AirplanesSpeed[airplaneName], 2));
                    }
                }
            });
            if (c1 < _ownerAgent.airplanesTillNowOnRadar)
            {
                List<string> airplanesToAdd = new List<string>();

                for (int i = 0; i < _ownerAgent.airplanesTillNowOnRadar - c1; i++)
                {
                    string airplane = "airplane" + (c1 + i).ToString();
                    if (_ownerAgent.AirplanesSpeed.ContainsKey(airplane) && !checkAirplaneInGrid(airplane) && chart1.Series.IsUniqueName(airplane))
                    {
                        airplanesToAdd.Add(airplane);
                    }
                }
                this.Invoke((MethodInvoker)delegate
                {
                    foreach (string airplane in airplanesToAdd)
                    {
                        addAirplaneToGridView(airplane);
                        addAirplaneToChart(airplane, Math.Round(_ownerAgent.AirplanesSpeed[airplane], 2));
                    }
                });
            }

            // refresh conflict rate on view 
            this.conflicts.Text = "Conflicts now: " + _ownerAgent.conflictsNow.Count; // monitorization purposes
            this.totalConflicts.Text = "Total conflicts: " + _ownerAgent.totalConflicts.Count; // simulation purposes
            this.round.Text = "Round: " + _ownerAgent.round;
            this.config.Text = "Config File: "+Utils.configFilePath;
            iteration++;
        }

        private void InitializeComponent()
        {
            ChartArea chartArea1 = new ChartArea();
            Legend legend1 = new Legend();
            this.dataGridView1 = new DataGridView();
            this.chart1 = new Chart();
            this.label1 = new Label();
            this.button1 = new Button();
            this.conflicts = new Label();
            this.totalConflicts = new Label();
            this.round = new Label();
            this.config = new Label();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new Point(35, 400);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new Size(800, 450);
            this.dataGridView1.TabIndex = 0;
            // 
            // chart1
            // 
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new Point(35, 51);
            this.chart1.Name = "chart1";
            this.chart1.Size = new Size(800, 300);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "chart1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new Point(32, 21);
            this.label1.Name = "label1";
            this.label1.Size = new Size(81, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Monitoring view";
            // 
            // button1
            // 
            this.button1.Location = new Point(759, 21);
            this.button1.Name = "button1";
            this.button1.Size = new Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Save ";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new EventHandler(this.button1_Click);
            // 
            // conflicts
            // 
            this.conflicts.AutoSize = true;
            this.conflicts.Location = new Point(430, 26);
            this.conflicts.Name = "conflicts";
            this.conflicts.Size = new Size(70, 13);
            this.conflicts.TabIndex = 3;
            this.conflicts.Text = "Conflicts now";
            this.conflicts.Click += new EventHandler(this.conflicts_Click);
            // 
            // totalConflicts
            // 
            this.totalConflicts.AutoSize = true;
            this.totalConflicts.Location = new Point(562, 26);
            this.totalConflicts.Name = "totalConflicts";
            this.totalConflicts.Size = new Size(73, 13);
            this.totalConflicts.TabIndex = 4;
            this.totalConflicts.Text = "Total conflicts";
            this.totalConflicts.Click += new EventHandler(this.label2_Click);
            // 
            // round
            // 
            this.round.AutoSize = true;
            this.round.Location = new Point(130, 21);
            this.round.Name = "round";
            this.round.Size = new Size(42, 13);
            this.round.TabIndex = 5;
            this.round.Text = "Round ";
            this.round.Click += new EventHandler(this.label2_Click_1);
            // 
            // config
            // 
            this.config.AutoSize = true;
            this.config.Location = new Point(32, 369);
            this.config.Name = "config";
            this.config.Size = new Size(56, 13);
            this.config.TabIndex = 6;
            this.config.Text = "Config File";
            // 
            // MonitoringForm
            // 
            this.ClientSize = new Size(870, 900);
            this.Controls.Add(this.config);
            this.Controls.Add(this.round);
            this.Controls.Add(this.totalConflicts);
            this.Controls.Add(this.conflicts);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "MonitoringForm";
            this.Load += new EventHandler(this.MonitoringForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }
        private void MonitoringForm_Load(object sender, EventArgs e)
        {
            SplineChart();
        }

        private void SplineChart()
        {
            this.chart1.Series.Clear();
            this.chart1.Titles.Add("Speed variation");
        }
        private void addAirplaneToGridView(string airplane)
        {
            DataRow newRow = dt.NewRow();
            newRow["Airplane"] = airplane;
            newRow["Last Speed Recorded"] = Math.Round(_ownerAgent.AirplanesSpeed[airplane], 2).ToString();
            newRow["State"] = "Flying" + (_ownerAgent.conflictsNow.Contains(airplane)? " (conflict)":"");
            dt.Rows.Add(newRow);
        }
        private void changeSpeedInGridView(int i, double speed)
        {
            dataGridView1.Rows[i].Cells[1].Value = speed;
            if (!(_ownerAgent.ExplorerPositions.ContainsKey("airplane" + i.ToString())))
                dataGridView1.Rows[i].Cells[2].Value = "Landed";
        }
        private void addAirplaneToChart(string airplane, double speed)
        {
            Series series = this.chart1.Series.Add(airplane);
            series.ChartType = SeriesChartType.Spline;
            series.Points.AddXY(iteration, speed);
        }
        private void changeSpeedInChart(string airplane, double speed)
        {
            this.chart1.Series[airplane].Points.AddXY(iteration, speed);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SaveFormView();
        }

        private void SaveFormView()
        {
            using (Bitmap bmp = new Bitmap(this.Width, this.Height))
            {
                this.DrawToBitmap(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));
                bmp.Save("FormScreenshot.png", ImageFormat.Png);
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void conflicts_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click_1(object sender, EventArgs e)
        {

        }
    }
}