using ActressMas;
using System;
using System.Configuration;
using System.Threading;
using static Reactive.Utils;

namespace Reactive
{
    public class Program
    {
        public static AppSettingsSection settings;

        [STAThread]
        private static void Main(string[] args)
        {
            Console.WriteLine(args);
            if (args.Length == 0) { Utils.LoadConfigBasedOnEnvironment(""); }
            else Utils.LoadConfigBasedOnEnvironment(args[0]);

            EnvironmentMas env = new EnvironmentMas(0, 1);

            var planetAgent = new PlanetAgent();
            env.Add(planetAgent, "planet");

            var coordinatorAgent = new CoordinatorAgent();
            env.Add(coordinatorAgent, "coordinator");

            for (int i = 0; i < Utils.NoExplorers; i++)
            {
                var explorerAgent = new ExplorerAgent();
                env.Add(explorerAgent, "airplane" + i);
            }

            Thread.Sleep(100);

            env.Start();


        }
    }
}