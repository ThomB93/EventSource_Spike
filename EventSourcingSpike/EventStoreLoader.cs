using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

namespace EventSourcingSpike
{
    public static class EventStoreLoader
    {
        //NOTE: To start the EventStore process, Visual Studio must be run as Admin

        //Enum containing options for connecting/disconnecting from the EventStore
        public enum StartConflictOption
        {
            Kill,
            Connect,
            Error
        }
        //Path to the local EventStore in solution
        private const string Path = @"..\..\EventStore-OSS-Win-v5.0.8\EventStore.ClusterNode.exe";

        //Contains parameter values to pass into EventStore
        private const string Args = "--config=./EventStore/config.yaml";

        //Enables start/stop of local processes. In this case, the process is the EventStore.ClusterNode.exe
        private static Process _process;

        public static IEventStoreConnection Connection { get; private set; }


        //Method to start setting up the EventStore upon executing the program
        public static void SetupEventStore(StartConflictOption opt = StartConflictOption.Connect) //set default to Connect
        {
            //Save the EventStore process in a variable for later use if running
            var runningEventStores = Process.GetProcessesByName("EventStore.ClusterNode");
            //if a process was found, check the parameter options on what to do with the process
            if (runningEventStores.Length != 0)
            {
                switch (opt)
                {
                    case StartConflictOption.Connect:
                        _process = runningEventStores[0]; //set the process to the EventStore.ClusterNode
                        break;
                    case StartConflictOption.Kill:
                        foreach (var es in runningEventStores) //makes sure that all running processes are killed
                        {
                            es.Kill();
                        }
                        break;
                    case StartConflictOption.Error: 
                        throw new Exception("Conflicting EventStore running."); //Will be thrown if there is already a running EventStore process
                    default:
                        throw new ArgumentOutOfRangeException(nameof(opt), opt, null);
                }
            }
            if (_process == null)
            {
                _process = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false, CreateNoWindow = true, FileName = Path, Arguments = Args, Verb = "runas"
                    }
                };
                _process.Start();
            }
            //set default IP endpoint and port (localhost:1113). HTTP uses port 2113, while TCP uses port 1113
            var tcp = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1113);
            var http = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2113);

            //Connect to the Event Store
            Connection = EventStoreConnection.Create(new Uri("tcp://admin:changeit@localhost:1113"));
            Connection.ConnectAsync().Wait();
        }

        //Method used to close the Event Store connection and kill the running process
        public static void TeardownEventStore(bool leaveRunning = true, bool dropData = false)
        {
            Connection.Close();
            if ( //close the connection if any of the following evaluates to true
                leaveRunning ||
                _process == null ||
                _process.HasExited
                ) return;

            _process.Kill(); //stops the EventStore process
            _process.WaitForExit(); //makes sure that the process was stopped successfully
            if (dropData) //delete data in Event store if set to true
            {
                Directory.Delete(@".\ESData", true);
            }
        }
    }
}
