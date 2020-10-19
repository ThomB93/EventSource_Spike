using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventSourcingSpike.Domain;
using EventSourcingSpike.User;
using EventStore.ClientAPI;
using Serilog;
using Serilog.Events;

namespace EventSourcingSpike
{
    public class ProjectionManager
    {
        private readonly IEventStoreConnection _connection;
        //Contains a list of the user read models
        private readonly IList<UserReadModel> _items;
        private EventStoreAllCatchUpSubscription _subscription;

        public ProjectionManager(IEventStoreConnection connection, IList<UserReadModel> items)
        {
            _connection = connection;
            _items = items;
        }

        //creates and connects to a new CatchUp subscription
        public void Start()
        {
            var settings = new CatchUpSubscriptionSettings(2000, 500,
                Log.IsEnabled(LogEventLevel.Verbose),
                false, "try-out-subscription");
            _subscription = _connection.SubscribeToAllFrom(Position.Start, //will get all events from the beginning of stream (catch up), subscribes to the $all stream
                settings, EventAppeared); //delegate that is called for each event
        }

        //stops the subscription
        public void Stop() => _subscription.Stop();

        private Task EventAppeared(EventStoreCatchUpSubscription _, ResolvedEvent resolvedEvent)
        {
            //filter out technical and test events from the $all stream
            if (resolvedEvent.Event.EventType.StartsWith("$") ||
                resolvedEvent.Event.EventType.StartsWith("eventType") ||
                resolvedEvent.Event.EventType.StartsWith("PersistentConfig1") ||
                resolvedEvent.Event.EventType.StartsWith("Subscription")) return Task.CompletedTask;
            //Deserialize event to C# object
            var @event = resolvedEvent.Deserialize();
            //tell user which events are being projected
            Console.WriteLine("Projecting event {0}", @event.GetType().Name);

            //check which event appeared in the projection and update read model
            switch (@event)
            {
                case Events.UserRegistered e:
                    _items.Add(new UserReadModel()
                    {
                        UserId = e.UserId,
                        Name = e.Name
                    });
                    break;
                case Events.UserNameUpdated e:
                    UpdateItem(e.UserId, user => user.Name = e.Name);
                    break;
            }

            return Task.CompletedTask;
        }

        //method for updating the read model based on the aggregate id
        private void UpdateItem(Guid id,
            Action<UserReadModel> update)
        {
            var item = _items.FirstOrDefault(
                x => x.UserId == id);
            if (item == null) return;
            update(item);
        }
    }
}
