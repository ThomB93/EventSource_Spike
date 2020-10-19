using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventSourcingSpike.Framework;
using EventStore.ClientAPI;
using Newtonsoft.Json;

namespace EventSourcingSpike
{
    public class EsAggregateStore : IAggregateStore
    {
        private readonly IEventStoreConnection _connection;

        //Constructor receives an instance of an EventStore connection
        public EsAggregateStore(IEventStoreConnection connection) => _connection = connection;

        //Saves an aggregate state to a stream, taking in the aggregate as a parameter, if stream doesn't exist, create new stream
        public async Task Save<T, TId>(T aggregate) where T : AggregateRoot<TId>
        {
            if (aggregate == null)
                throw new ArgumentNullException(nameof(aggregate));

            //We need to build a list of events from the events listed on the aggregate.
            //Creates EventData objects from the events in the Aggregate and stores them in a variable.
            var changes = aggregate.GetChanges()
                .Select(@event =>
                    new EventData( //create new events from the changes to the aggregate state
                        eventId: Guid.NewGuid(),
                        type: @event.GetType().Name, //CLR type name is used as the event name
                        isJson: true,
                        data: Serialize(@event), //Serialize to JSON for UI presentation in Event Store
                        metadata: Serialize(new EventMetadata //Metadata is also serialized to JSON. Newtonsoft.Json must know the FQCN to deserialize, which we get here.
                        { ClrType = @event.GetType().AssemblyQualifiedName })
                    ))
                .ToArray();

            if (!changes.Any()) return;

            var streamName = GetStreamName<T, TId>(aggregate); 

            await _connection.AppendToStreamAsync(
                streamName,
                aggregate.Version,
                changes);

            aggregate.ClearChanges();
        }

        //All reads and writes between our application and Event Store need to be executed on the openTCP connection to the Event Store cluster
        //Only the "Create" command for an aggregate is exempt from reading from the event store before it executes

        //What are the steps to retrieve an aggregate from the event store?
        // 1. Find out the stream name for an aggregate
        // 2. Read all of the events from the aggregate stream
        // 3. Loop through all of the events, and call the 'When' handler for each of them

        //All these steps are done in the Load method below
        public async Task<T> Load<T, TId>(TId aggregateId)
            where T : AggregateRoot<TId>
        {
            if (aggregateId == null)
                throw new ArgumentNullException(nameof(aggregateId));

            var stream = GetStreamName<T, TId>(aggregateId);
            var aggregate = (T)Activator.CreateInstance(typeof(T), true); //uses reflections

            var page = await _connection.ReadStreamEventsForwardAsync( //returns ResolvedEvent objects, reads oldest to newest events
                stream, 0, 1024, false); //reads a stream slice (1024 events), starts from beginning of stream

            //calls the Load method on the aggregate instance, after deserializing the raw events to domain events
            aggregate.Load(page.Events.Select(
                resolvedEvent => resolvedEvent.Deserialize()).ToArray());

            return aggregate;
        }

        //Checks whether a stream exists for a certain aggregate
        public async Task<bool> Exists<T, TId>(TId aggregateId)
        {
            var stream = GetStreamName<T, TId>(aggregateId);
            var result = await _connection.ReadEventAsync(stream, 1, false);
            return result.Status != EventReadStatus.NoStream;
        }

        //Serialize data to JSON
        private static byte[] Serialize(object data)
            => Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));

        //Gets the stream name from an aggregateId
        private static string GetStreamName<T, TId>(TId aggregateId)
            => $"{typeof(T).Name}-{aggregateId.ToString()}";

        //We make the stream name derive from the aggregate name
        private static string GetStreamName<T, TId>(T aggregate)
            where T : AggregateRoot<TId>
            => $"{typeof(T).Name}-{aggregate.Id.ToString()}";

        //We need a place to store event metadata. A new class is created for this purpose.
    }
}
