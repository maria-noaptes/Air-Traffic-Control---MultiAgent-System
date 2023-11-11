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
            for (int i = 0; i < Utils.NoExplorers; i++)
            {
                Brush brush = Utils.PickBrush();
                while (colors.Values.Contains(brush)) brush = Utils.PickBrush();
                colors.Add("airplane" + i, brush);
            }

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

        Func<string, string> getAirplaneIndex = (name) =>
        {
            string index = name.Replace("airplane", "");
            index = (index.Length == 1 ? " " : "") + name.Replace("airplane", "");
            return index;
        };
        private void DrawPlanet()
        {
            int w = pictureBox.Width;
            int h = pictureBox.Height;

            if (_doubleBufferImage != null)
            {
                _doubleBufferImage.Dispose();
                GC.Collect(); // prevents memory leaks
            }

            _doubleBufferImage = new Bitmap(w, h);
            Graphics g = Graphics.FromImage(_doubleBufferImage);
            g.Clear(Color.White);

            int minXY = Math.Min(w, h);
            int radarStartX = 10;
            int radarStartY = 10;

            Font font = new Font(FontFamily.GenericSansSerif, 12.0F, FontStyle.Bold);

            if (_ownerAgent != null)
            {
                // radar
                g.FillEllipse(Brushes.Red, Utils.airportCenterX, Utils.airportCenterY, Utils.cellSize, Utils.cellSize);
                Pen blackPen = new Pen(Color.Black, 3);
                Pen redPen = new Pen(Color.Red, 3);
                g.DrawEllipse(blackPen, radarStartX, radarStartY, Utils.radarRay * 2, Utils.radarRay * 2);
                // altitude
                foreach (KeyValuePair<string, string> v in _ownerAgent.ExplorerPositions)
                {
                    List<int> pos = v.Value.Split(' ').Select(e => Convert.ToInt32((double.Parse(e)))).ToList();

                    // airplane on radar
                    g.FillRectangle(colors[v.Key], Utils.airportCenterX + pos[0], Utils.airportCenterY + pos[1], Utils.cellSize/3, Utils.cellSize/3);
                   // g.DrawString(getAirplaneIndex(v.Key), font, Brushes.Black, Utils.airportCenterX + pos[0], Utils.airportCenterY + pos[1]);

                    // airplane altitude
                    g.FillRectangle(colors[v.Key], w - Convert.ToInt32(0.8 * Utils.radarRay), h - pos[2]-100, Utils.cellSize, Utils.cellSize);
                    g.DrawString(getAirplaneIndex(v.Key), font, Brushes.Black, w - Convert.ToInt32(0.8 * Utils.radarRay), h - pos[2]-100);
                }
            }

            g.DrawString("Plan recomputed: " + _ownerAgent.planComputed, font, Brushes.Black, w - 200, h - 30);
            Graphics pbg = pictureBox.CreateGraphics();
            pbg.DrawImage(_doubleBufferImage, 0, 0);
        }

        private void pictureBox_Click(object sender, EventArgs e)
        {

        }
    }
}