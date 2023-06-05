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

namespace Reactive
{
    public class PlanetAgent : Agent
    {
        private PlanetForm _formGui;
        private Stopwatch stopwatch = new Stopwatch();
        public Dictionary<string, string> ExplorerPositions { get; set; }

        public PlanetAgent()
        {
            ExplorerPositions = new Dictionary<string, string>();

            Thread t = new Thread(new ThreadStart(GUIThread));
            t.Start();
        }

        private void GUIThread()
        {
            _formGui = new PlanetForm();
            _formGui.SetOwner(this);
            _formGui.ShowDialog();
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
                    Console.WriteLine("landing " + message.Sender + " " + ExplorerPositions.Values.Count);
                    ExplorerPositions.Remove(message.Sender);
                    Console.WriteLine("landing " + message.Sender + " " +  ExplorerPositions.Values.Count);
                    break;
                default:
                    break;
            }
            _formGui.UpdatePlanetGUI();
        }

        private void HandlePosition(string sender, string position)
        {
            ExplorerPositions.Add(sender, position);
            Send(sender, "move");
        }
        private void HandleChange(string sender, string position)
        {
            ExplorerPositions[sender] = position;
            Send(sender, "move");
        }
    }
}