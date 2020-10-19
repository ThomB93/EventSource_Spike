using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EventSourcingSpike.User;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace EventSourcingSpike
{
    class Program
    {
        //constant values only used for testing purposes
        const string STREAM = "test_stream";
        const string GROUP = "test_group";

        public static UserCommandHandler userCommands;
        public static UserQueryHandler userQueries;
        public static UserService userService;
        public static ProjectionManager projectionManager;

        static void Main(string[] args)
        {
            //Setup code...
            Console.WriteLine("Connecting to EventStore...");

            EventStoreLoader.SetupEventStore(EventStoreLoader.StartConflictOption.Connect);

            //create instance of the user service
            userService = new UserService(new EsAggregateStore(EventStoreLoader.Connection));

            //read models and projections
            var items = new List<UserReadModel>();
            projectionManager = new ProjectionManager(EventStoreLoader.Connection, items);

            //command and query handlers
            userCommands = new UserCommandHandler(userService);
            userQueries = new UserQueryHandler(items, userService); //need list of read model items

            Console.WriteLine("EventStore was setup successfully!");

            DisplayUserCommands();
            StartCommandLoop();
        }
        public static void StartCommandLoop()
        {
            do //Command loop, runs until user shuts down program or EventStore
            {
                var cmd = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(cmd))
                {
                    DisplayUserCommands();
                    continue;
                }

                string streamName = string.Empty;

                switch (cmd.ToLower())
                {
                    case "exit":
                        Console.WriteLine("Disconnecting from EventStore");
                        EventStoreLoader.TeardownEventStore();
                        break;
                    case "1t":
                        Console.Write("Please enter stream name (New or existing): ");
                        streamName = Console.ReadLine();
                        Console.WriteLine("Adding 3 events to stream " + streamName + "...\n");
                        CreateAndRunTestStream(streamName);
                        continue;
                    case "2t":
                        Console.Write("Please enter existing stream name: ");
                        streamName = Console.ReadLine();
                        CreatePersistentSubscription(streamName);
                        Console.WriteLine("Persistent Subscription for stream \"" + streamName + "\" has been created!");
                        continue;
                    case "3t":
                        Console.Write("Please enter existing stream name: ");
                        streamName = Console.ReadLine();
                        ConnectPersistentSubscription(streamName);
                        Console.WriteLine("Connection to Persistent Subscription for stream \"" + streamName + "\" has been created!");
                        Console.WriteLine("Waiting for new events...");
                        continue;
                    case "4t":
                        Console.Write("Please enter stream name: ");
                        streamName = Console.ReadLine();
                        Console.Write("Read events reversed? (y/n): ");
                        string answer = Console.ReadLine();
                        bool reversed = answer == "y" ? true : false;
                        Console.WriteLine("Listing all events from stream...\n");
                        ListOperations(streamName, reversed);
                        continue;
                    case "1d":
                        Console.Write("Please input username: ");
                        string userName = Console.ReadLine();
                        userCommands.RegisterUser(new Contracts.RegisterUser() { UserId = Guid.NewGuid(), Name = userName });
                        Console.WriteLine("User with username \"" + userName + "\" has been saved to the Event Store!\n");
                        continue;
                    case "2d":
                        Console.Write("Please input ID of user to update: ");
                        string userID = Console.ReadLine();
                        Console.Write("Please input new user name: ");
                        string userNameUpdated = Console.ReadLine();
                        userCommands.UpdateUserName(new Contracts.UpdateUserName() { UserId = Guid.Parse(userID), Name = userNameUpdated });
                        Console.WriteLine("User " + userID + " has been updated with new username \"" + userNameUpdated + "\"");
                        continue;
                    case "3d":
                        Console.WriteLine("Creating and connecting to Catch Up subscription for the $all stream...");
                        projectionManager.Start();
                        break;
                    case "4d":
                        Console.Write("Please input ID of user to display: ");
                        string userId = Console.ReadLine();
                        UserReadModel queriedUser = userQueries.GetSingleUser(new UserQueryModels.GetUser() { UserId = Guid.Parse(userId) });
                        Console.WriteLine("Success, user was found!\n--------------------------------------------\nUserID: " + queriedUser.UserId + "\nUsername: " + queriedUser.Name);
                        break;
                    case "5d":
                        Console.WriteLine("Fetching all users aggregates from Event Store...\n");
                        List<UserReadModel> userReadModels = userQueries.GetAllUsers();
                        foreach (var urm in userReadModels)
                        {
                            Console.WriteLine("UserID: " + urm.UserId + "\nUsername: " + urm.Name + "\n--------------------------------------------");
                        }
                        break;
                }

            } while (true);
        }
        public static void DisplayUserCommands()
        {
            Console.WriteLine();
            Console.WriteLine("Available Commands (Testing):\n-----------------------------------");
            Console.WriteLine("\t 1T: createTestEvents");
            Console.WriteLine("\t 2T: createPersistentSubTest");
            Console.WriteLine("\t 3T: connectPersistentSubTest");
            Console.WriteLine("\t 4T: listTestEventOperations");
            Console.WriteLine();
            Console.WriteLine("Available Commands (DDD):\n-----------------------------------");
            //Console.WriteLine("\t 1D: createEsAggregateStore");
            Console.WriteLine("\t 1D: createUserAggregate");
            Console.WriteLine("\t 2D: updateUserAggregate");
            Console.WriteLine("\t 3D: create&ConnectCatchUpSub");
            Console.WriteLine("\t 4D: getUserAggregateFromID");
            Console.WriteLine("\t 5D: getAllUserAggregates");
            Console.WriteLine("");
            Console.WriteLine("\t exit: shutdownEventStore");
            Console.WriteLine("");
            Console.Write("Command: ");
        }

        //Method for creating a test stream and adding 3 events to it
        public static void CreateAndRunTestStream(string streamName)
        {
            //const string STREAM = "test_stream";
            for (var x = 0; x < 3; x++)
            {
                EventStoreLoader.Connection.AppendToStreamAsync(streamName,
                    ExpectedVersion.Any, //ensures that write will always succeed
                    GetEventData(x)).Wait(); //creates a new event and appends to stream
                Console.WriteLine("Event " + x + " has been written to " + streamName);
            }
        }

        //Method used to return test event data
        private static EventData GetEventData(int i)
        {
            return new EventData(
                Guid.NewGuid(), //each new event has a Guid
                "eventType", //default Event Type
                true,
                Encoding.ASCII.GetBytes("{\"test-data\" : " + i + "}"), //test data
                Encoding.ASCII.GetBytes("{\"meta-data\" : " + i + "}")
            );
        }

        //Creates a persistent subscription on the test_stream from code
        private static void CreatePersistentSubscription(string streamName)
        {
            //Configure settings for the subscription. Not all settings are shown. 
            PersistentSubscriptionSettings settings = PersistentSubscriptionSettings.Create()
                .DoNotResolveLinkTos()
                .StartFromCurrent(); //start subscribing to new events from the point on where the stream is when creating the subscription
            try
            {
                //creates a new persistent subscription on test_stream using default login credentials
                EventStoreLoader.Connection.CreatePersistentSubscriptionAsync(streamName, GROUP, settings, new UserCredentials("admin", "changeit")).Wait();
            }
            catch (AggregateException ex)
            {
                //check if subscription already exists, if so throw an error
                if (ex.InnerException.GetType() != typeof(InvalidOperationException)
                    && ex.InnerException?.Message != $"Subscription group {GROUP} on stream {STREAM} already exists")
                {
                    throw;
                }
            }
        }

        //Method for connecting to a persistent subscription from code
        private static void ConnectPersistentSubscription(string streamName)
        {
            //when a new event appears, write it to the console
            EventStoreLoader.Connection.ConnectToPersistentSubscription(streamName, GROUP, (_, x) =>
            {
                var data = Encoding.ASCII.GetString(x.Event.Data);
                Console.WriteLine("Received new event: EventStreamId = " + x.Event.EventStreamId + " : EventNumber = " + x.Event.EventNumber + " : Created at = " + x.Event.Created +"\n");
                Console.WriteLine(data);
            });
            Console.WriteLine("Waiting for new events...");
        }

        //Method for listing the latest events. Parameter can be set to reverse the list (get from start or end of stream)
        private static void ListOperations(string streamName, bool reversed = false)
        {
            var streamEvents = new List<ResolvedEvent>();

            StreamEventsSlice currentSlice;
            //checks to see from what position of the stream the slice should be drawn from
            long nextSliceStart = reversed ? StreamPosition.End : StreamPosition.Start;
            do
            {
                currentSlice =
                    reversed ?
                        EventStoreLoader.Connection.ReadStreamEventsBackwardAsync( //newest to oldest events
                            streamName, //what stream to read from
                            nextSliceStart, //determines where to read from
                            20, //how many events to read
                            false).Result //don't resolve the events

                        :
                        EventStoreLoader.Connection.ReadStreamEventsForwardAsync( //oldest to newest events
                            streamName,
                            nextSliceStart,
                            20,
                            false).Result;
                nextSliceStart = currentSlice.NextEventNumber;
                streamEvents.AddRange(currentSlice.Events); //add the events found in the slice to list of events
            } while (!currentSlice.IsEndOfStream);

            //loop continues until no more events are found in the stream (EndOfStream)
            //Display events to the console
            foreach (var view in streamEvents)
            {
                Console.WriteLine("Event No: " + view.Event.EventNumber + " Created at: " + view.Event.Created + "  Type: " + view.Event.EventType);
            }
        }
    }
}
