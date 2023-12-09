
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Drawing;
using System.Linq;
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
        private System.Windows.Forms.DataVisualization.Charting.Chart chart1;
        private int iteration = 0;

        public MonitoringForm()
        {
            InitializeComponent();

            dt.Columns.Add("Airplane", typeof(string));
            dt.Columns.Add("Speed", typeof(string));

            dataGridView1.DataSource = dt;
            this.chart1.Series.Clear();
        }

        public void SetOwner(PlanetAgent a)
        {
            _ownerAgent = a;
        }

        private void pictureBox_Paint(object sender, PaintEventArgs e)
        {
            DrawPlanet();
        }

        public void UpdatePlanetGUI()
        {
            DrawPlanet();
        }

        private void pictureBox_Resize(object sender, EventArgs e)
        {
            DrawPlanet();
        }

        private void DrawPlanet()
        {
            int c1 = dataGridView1.Rows.Count;
            Console.WriteLine("datagrid " + dataGridView1.Rows.Count + " " + dataGridView1.Rows.ToString());
            Console.WriteLine("c1 " + c1 + ", c2 " + _ownerAgent.airplanesTillNowOnRadar);

            /*for(int i = 0;i < this.chart1.Series.Count; i++)
            {
                addNewSpeedInChart("airplane" + i, Math.Round(_ownerAgent.AirplanesSpeed["airplane" + i], 2));
            }*/

            for (int i = 0; i < c1; i++)
            {
                Console.WriteLine("index " + i);
                DataGridViewRow newDataRow = dataGridView1.Rows[i];
                if (_ownerAgent.AirplanesSpeed.ContainsKey("airplane" + i))
                {
                    newDataRow.Cells["Speed"].Value = Math.Round(_ownerAgent.AirplanesSpeed["airplane" + i], 2).ToString();
                }
                // dataGridView1.Rows[i].Cells[1].Value = Math.Round(_ownerAgent.AirplanesSpeed["airplane" + i], 2).ToString();
                //if (this.chart1.Series.Count > i)
                // addNewSpeedInChart("airplane" + i, Math.Round(_ownerAgent.AirplanesSpeed["airplane" + i], 2));
            }

            if (c1 < _ownerAgent.airplanesTillNowOnRadar)
                for(int i = 0;i < _ownerAgent.airplanesTillNowOnRadar - c1; i++) 
                {
                    if (_ownerAgent.AirplanesSpeed.ContainsKey("airplane" + (c1 + i).ToString()))
                    {
                        dt.Rows.Add(new object[] { "airplane" + (c1 + i).ToString(), _ownerAgent.AirplanesSpeed["airplane" + (c1 + i).ToString()].ToString() });
                        // addAirplaneToChart("airplane" + (c1 + i), Math.Round(_ownerAgent.AirplanesSpeed["airplane" + (c1 + i)], 2));
                    }
                }

            iteration++;
            this.chart1.Series.ToList().ForEach(p => Console.WriteLine(p + " "));
            Console.WriteLine("this.chart1.Series[0] ");
        }

        private void InitializeComponent()
        {
            ChartArea chartArea1 = new ChartArea();
            Legend legend1 = new Legend();
            Series series1 = new Series();
            this.dataGridView1 = new DataGridView();
            this.chart1 = new Chart();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(26, 51);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(818, 447);
            this.dataGridView1.TabIndex = 0;
            // 
            // chart1
            // 
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(341, 51);
            this.chart1.Name = "chart1";
            series1.ChartArea = "ChartArea1";
            series1.Legend = "Legend1";
            series1.Name = "Series1";
            this.chart1.Series.Add(series1);
            this.chart1.Size = new System.Drawing.Size(503, 284);
            this.chart1.TabIndex = 1;
            this.chart1.Text = "chart1";
            // 
            // MonitoringForm
            // 
            this.ClientSize = new System.Drawing.Size(869, 526);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "MonitoringForm";
            this.Load += new System.EventHandler(this.MonitoringForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).EndInit();
            this.ResumeLayout(false);

        }
        private void MonitoringForm_Load(object sender, EventArgs e)
        {
            SplineChart();
        }
      
        private void elementHost1_ChildChanged(object sender, System.Windows.Forms.Integration.ChildChangedEventArgs e)
        {

        }
        private void SplineChart()
        {
            this.chart1.Series.Clear();
            this.chart1.Titles.Add("Speed variation");
        }

        private void addAirplaneToChart(string airplane, double speed)
        {
            Series series = this.chart1.Series.Add(airplane);
            series.ChartType = SeriesChartType.Spline;
            series.Points.AddXY(iteration, speed);
        }
        private void addNewSpeedInChart(string airplane, double speed)
        {
            this.chart1.Series[airplane].Points.AddXY(iteration, speed);
        }

    }
}