using ActressMas;
using Message = ActressMas.Message;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using System.Diagnostics;
using System.Configuration;

namespace Reactive
{
    public class PlanetAgent : Agent
    {
        private PlanetForm _formGui;
        private MonitoringForm _formGuiMonitoring;
        private Stopwatch stopwatch = new Stopwatch();
        public int planComputed = 0;
        public int airplanesTillNowOnRadar = 0;
        public List<string> conflictsNow = new List<string>();
        public List<string> totalConflicts = new List<string>();
        private List<string> Landed = new List<string>();
        private string pathToSaveLogs = ConfigurationManager.AppSettings["pathToSaveLogs"];
        public int round = 0;

        public Dictionary<string, string> ExplorerPositions { get; set; } = new Dictionary<string, string> { }; 
        public Dictionary<string, double> AirplanesSpeed { get; set; } = new Dictionary<string, double> { };

        int airplanesAtStart = Int32.Parse(ConfigurationManager.AppSettings["noExplorers"]);

        public PlanetAgent()
        {
            Thread t = new Thread(new ThreadStart(GUIThread));
            t.Start();

            Thread t_m = new Thread(new ThreadStart(GUIThreadMonitoring));
            t_m.Start();
        }

        private void GUIThread()
        {
            _formGui = new PlanetForm();
            _formGui.SetOwner(this);
            _formGui.ShowDialog();


            System.Windows.Forms.Application.Run();
        }

        private void GUIThreadMonitoring()
        {
            _formGuiMonitoring = new MonitoringForm();
            _formGuiMonitoring.SetOwner(this);
            _formGuiMonitoring.ShowDialog();

            System.Windows.Forms.Application.Run();
        }

        public override void Setup()
        {
            Console.WriteLine("Starting " + Name);
            round++;
            DateTime localDate = DateTime.Now;
            Utils.appendToFile(pathToSaveLogs, "Simulation time " + localDate + "\nNoExplorers: " + Utils.NoExplorers + "\nCollaborating: " + ConfigurationManager.AppSettings["activateCollaboration"]);

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
                    Landed.Add(message.Sender);
                    if (airplanesAtStart == Landed.Count()) // all planes landed
                        handleSimulationEnd();
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

        private void handleSimulationEnd()
        {
            conflictsNow.Clear();
            totalConflicts.Clear();
            ExplorerPositions.Clear();
            AirplanesSpeed.Clear();

            Utils.appendToFile(pathToSaveLogs, "Conflicts: " + totalConflicts.Count);
            int noActivatedCollaboration = ConfigurationManager.AppSettings["activateCollaboration"].Split(' ').Count();
            Utils.appendToFile(pathToSaveLogs, "Collaboration activated: " + noActivatedCollaboration + "/" + airplanesAtStart + "\n");

            Setup();

            foreach (string airplane in Landed)
            {
                Send(airplane, "restart");
            }
            Landed.Clear();
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

            checkConflicts(ExplorerPositions);
        }
        private void checkConflicts(Dictionary<string, string> AirplanesPositions)
        {
            // check distance between all combinations of planes currently flying
            double nrOfCombinations = 0;
            conflictsNow.Clear();
            foreach (string airplane1 in AirplanesPositions.Keys)
                foreach (string airplane2 in AirplanesPositions.Keys)
                {
                    if (airplane1 != airplane2 && !conflictsNow.Contains(airplane2 + " " + airplane1))
                    {
                        nrOfCombinations += 1;
                        double separation = Utils.distance(AirplanesPositions[airplane1], AirplanesPositions[airplane2]);
                        if (separation < Utils.separationRequired)
                        {
                            conflictsNow.Add(airplane1+ " " + airplane2);
                        }
                    }
                }
            addConflicts(totalConflicts, conflictsNow);
        }
        private void addConflicts(List<string> totalConflicts, List<string> toAdd)
        {
            foreach(string conflict in toAdd)
            {
                var sameConflict = string.Join(" ", conflict.Split(' ').Reverse());
                if (!totalConflicts.Contains(conflict) && !totalConflicts.Contains(sameConflict))
                {
                    totalConflicts.Add(conflict);
                }
            }
        }
        
    }
}