using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Reactive
{
    public partial class PlanetForm : Form
    {
        private PlanetAgent _ownerAgent;
        private Bitmap _doubleBufferImage;
        public static Dictionary<string, Brush> colors = new Dictionary<string, Brush>();

        public PlanetForm()
        {
            InitializeComponent();
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

        Func<string, string> getAirplaneIndex = name =>
        {
            string index = name.Replace("airplane", "");
            return index.Length == 1 ? " " + index : index;
        };

        private void DrawPlanet()
        {
            int width = pictureBox.Width;
            int height = pictureBox.Height;
            CleanupBufferImage(); 

            _doubleBufferImage = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(_doubleBufferImage))
            {
                g.Clear(Color.White);
                DrawRadar(g, width, height);
                if (_ownerAgent != null) DrawAirplanes(g, width, height);
                DrawPlanStatus(g, width, height);
            }
            RenderToPictureBox();
        }

        private void CleanupBufferImage()
        {
            _doubleBufferImage?.Dispose();
            GC.Collect(); 
        }

        private void DrawRadar(Graphics g, int width, int height)
        {
            int radarDiameter = Utils.radarRay * 2;
            g.FillEllipse(Brushes.Red, Utils.airportCenterX - Utils.cellSize / 4, Utils.airportCenterY - Utils.cellSize / 4, Utils.cellSize, Utils.cellSize);
            g.DrawEllipse(new Pen(Color.Black, 3), 10, 10, radarDiameter, radarDiameter);
        }

        private void DrawAirplanes(Graphics g, int width, int height)
        {
            Font font = new Font(FontFamily.GenericSansSerif, 12.0F, FontStyle.Bold);
            List<string> planesInConflict = getPlanesInConflict();

            foreach (var position in _ownerAgent.ExplorerPositions)
            {
                int index = Convert.ToInt32(position.Key.Replace("airplane", ""));
                var pos = position.Value.Split(' ').Select(e => Convert.ToInt32(double.Parse(e))).ToList();
                Brush brush = Utils.PickBrush(index);

                DrawAirplaneOnRadar(g, brush, font, pos, index, planesInConflict);
                DrawAirplaneAltitude(g, brush, font, pos, index, width, height);
            }
        }

        private void DrawAirplaneOnRadar(Graphics g, Brush brush, Font font, List<int> pos, int index, List<string> planesInConflict)
        {
            g.FillRectangle(brush, Utils.airportCenterX + pos[0], Utils.airportCenterY + pos[1], Utils.cellSize / 3, Utils.cellSize / 3);
            if (planesInConflict.Contains($"airplane{index}"))
            {
                g.DrawString("!", font, Brushes.Red, Utils.airportCenterX + pos[0] - Utils.cellSize / 6, Utils.airportCenterY + pos[1] - Utils.cellSize / 3);
            }
        }

        private void DrawAirplaneAltitude(Graphics g, Brush brush, Font font, List<int> pos, int index, int width, int height)
        {
            g.FillRectangle(brush, width - Convert.ToInt32(0.8 * Utils.radarRay), height - pos[2] - 100, Utils.cellSize, Utils.cellSize);
            g.DrawString(getAirplaneIndex($"airplane{index}"), font, Brushes.Black, width - Convert.ToInt32(0.8 * Utils.radarRay), height - pos[2] - 100);
        }

        private void DrawPlanStatus(Graphics g, int width, int height)
        {
            Font font = new Font(FontFamily.GenericSansSerif, 12.0F, FontStyle.Bold);
            g.DrawString($"Plan recomputed: {_ownerAgent.planComputed}", font, Brushes.Black, width - 200, height - 30);
        }

        private void RenderToPictureBox()
        {
            using (Graphics pbg = pictureBox.CreateGraphics())
            {
                if (pictureBox.InvokeRequired)
                {
                    pictureBox.Invoke((MethodInvoker)(() => pbg.DrawImage(_doubleBufferImage, 0, 0)));
                }
                else
                {
                    pbg.DrawImage(_doubleBufferImage, 0, 0);
                }
            }
        }


        private List<string> getPlanesInConflict()
        {
            List<string> result = new List<string>();
            foreach (string conflict in _ownerAgent.conflictsNow)
            {
                foreach (string airplane in conflict.Split(' ')) { result.Add(airplane); }
            }
            return result.Distinct().ToList();
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {

        }

        private void PlanetForm_Load(object sender, EventArgs e)
        {

        }
    }
}