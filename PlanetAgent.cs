using ActressMas;
using Message = ActressMas.Message;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Data.SqlTypes;
using System.Runtime.Remoting.Messaging;

namespace Reactive
{
    public class PlanetAgent : Agent
    {
        private PlanetForm _formGui;
        private MonitoringForm _formGuiMonitoring;
        private Stopwatch stopwatch = new Stopwatch();
        private StreamWriter writer;
        public int planComputed = 0;
        public int airplanesTillNowOnRadar = 0;

        public Dictionary<string, string> ExplorerPositions { get; set; }
        public Dictionary<string, double> AirplanesSpeed { get; set; }

        public PlanetAgent()
        {
            ExplorerPositions = new Dictionary<string, string> { }; 
            AirplanesSpeed = new Dictionary<string, double> { };

            Thread t = new Thread(new ThreadStart(GUIThread));
            t.Start();
            //GUIThread();

            Thread t_m = new Thread(new ThreadStart(GUIThreadMonitoring));
            t_m.Start();
            //GUIThreadMonitoring();
        }

        private void GUIThread()
        {
            _formGui = new PlanetForm();
            _formGui.SetOwner(this);
            _formGui.ShowDialog();


            Application.Run();
        }

        private void GUIThreadMonitoring()
        {
            _formGuiMonitoring = new MonitoringForm();
            _formGuiMonitoring.SetOwner(this);
            _formGuiMonitoring.ShowDialog();

            Application.Run();
        }

        public override void Setup()
        {
            Console.WriteLine("Starting " + Name);
            stopwatch.Start();
        }

        public override void Act(Message message)
        {
            Console.WriteLine("\t[{1} -> {0}]: {2}", this.Name, message.Sender, message.Content);

            string action; string parameters;
            Utils.ParseMessage(message.Content, out action, out parameters);

            switch (action)
            {
                case "position":
                    HandlePosition(message.Sender, parameters);
                    break;

                case "change":
                    HandleChange(message.Sender, parameters);
                    break;
                case "landing":
                    ExplorerPositions.Remove(message.Sender);

                    appendToFile("landings.txt", "landing " + message.Sender + " " + stopwatch.Elapsed);
                    break;
                case "plan":
                    planComputed++;
                    break;
                default:
                    break;
            }
            if (_formGui != null) _formGui.UpdatePlanetGUI();
            if (_formGuiMonitoring != null) _formGuiMonitoring.UpdatePlanetGUI();
        }

        private void appendToFile(string file, string text)
        {
            writer = new StreamWriter(file, true);
            writer.WriteLine(text);
            writer.Close();
        }

        private void HandlePosition(string sender, string position)
        {
            double speed = Double.Parse(position.Split(' ')[3]);
            AirplanesSpeed.Add(sender, speed);

            airplanesTillNowOnRadar++;

            ExplorerPositions.Add(sender, Utils.RemoveFromEnd(position, " " + speed));
            
            int indexAirplane = Int32.Parse(sender.Replace("airplane", ""));
            Send("airplane" + (indexAirplane+1), "start");

            Send(sender, "move");

        }
        private void HandleChange(string sender, string position)
        {
            double speed = Double.Parse(position.Split(' ')[3]);
            AirplanesSpeed[sender] = speed;

            ExplorerPositions[sender] = Utils.RemoveFromEnd(position, " " + speed);
            Send(sender, "move");
        }
    }
}