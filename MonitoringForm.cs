
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
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
                    Console.WriteLine("cellValue " + cellValue);
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
            iteration++;
        }

        private void InitializeComponent()
        {
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.chart1 = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.label1 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chart1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(35, 400);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.Size = new System.Drawing.Size(800, 450);
            this.dataGridView1.TabIndex = 0;
            // 
            // chart1
            // 
            chartArea1.Name = "ChartArea1";
            this.chart1.ChartAreas.Add(chartArea1);
            legend1.Name = "Legend1";
            this.chart1.Legends.Add(legend1);
            this.chart1.Location = new System.Drawing.Point(35, 51);
            this.chart1.Name = "chart1";
            this.chart1.Size = new System.Drawing.Size(800, 300);
            this.chart1.TabIndex = 0;
            this.chart1.Text = "chart1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(32, 21);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(81, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Monitoring view";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(759, 21);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 2;
            this.button1.Text = "Save ";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // MonitoringForm
            // 
            this.ClientSize = new System.Drawing.Size(870, 900);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.chart1);
            this.Controls.Add(this.dataGridView1);
            this.Name = "MonitoringForm";
            this.Load += new System.EventHandler(this.MonitoringForm_Load);
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
            newRow["Last Speed Recorded"] = _ownerAgent.AirplanesSpeed[airplane].ToString();
            newRow["State"] = "Flying";
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

        /*private void btnSaveImage_Click(object sender, EventArgs e)
        {
            Bitmap bitmap = new Bitmap(this.Width, this.Height);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(this.Location, new Point(0, 0), this.Size);
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Bitmap Image (*.bmp)|*.bmp|JPEG Image (*.jpg)|*.jpg|PNG Image (*.png)|*.png";
            saveFileDialog.Title = "Save Image";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                bitmap.Save(saveFileDialog.FileName);
            }

            bitmap.Dispose();
        }*/

        private void button1_Click(object sender, EventArgs e)
        {
            Thread saveImageThread = new Thread(SaveImageThreadMethod);
            saveImageThread.Start();
        }
        private void SaveImageThreadMethod()
        {
            // Create a bitmap to capture the form's content
            Bitmap bitmap = new Bitmap(this.Width, this.Height);
            Console.WriteLine("save 1");
            // Create a Graphics object from the bitmap
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                // Capture the form's content
                graphics.CopyFromScreen(this.Location, new Point(0, 0), this.Size);
            }
            Console.WriteLine("save 2");


            this.BeginInvoke((MethodInvoker)delegate
            {
                // Show a SaveFileDialog to choose the file location and name
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Bitmap Image (*.bmp)|*.bmp|JPEG Image (*.jpg)|*.jpg|PNG Image (*.png)|*.png";
                saveFileDialog.Title = "Save Image";

                Console.WriteLine("save 3");
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Save the bitmap to the chosen file location
                    bitmap.Save(saveFileDialog.FileName);
                }

                Console.WriteLine("save 4");
                // Dispose of the bitmap to release resources
                bitmap.Dispose();
            });
        }
    }
}