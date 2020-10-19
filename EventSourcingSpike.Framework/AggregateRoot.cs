using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventSourcingSpike.Framework
{
    public abstract class AggregateRoot<TId> : IInternalEventHandler
    {
        public TId Id { get; protected set; }
        public int Version { get; private set; } = -1;

        //Method is overridden by aggregates to change state
        protected abstract void When(object @event);

        //contains a list of events 
        private readonly List<object> _changes;

        protected AggregateRoot() => _changes = new List<object>();

        //Method is called for each event to invoke the 'When' method and ensure the state is valid before adding changes
        protected void Apply(object @event)
        {
            When(@event);
            EnsureValidState();
            _changes.Add(@event);
        }

        public IEnumerable<object> GetChanges() => _changes.AsEnumerable();
        //receives a list of previous events (history) and calls the 'When' method for each thus changing the state
        public void Load(IEnumerable<object> history)
        {
            foreach (var e in history)
            {
                When(e);
                Version++;
            }
        }

        public void ClearChanges() => _changes.Clear();
        //is overridden in aggregates to ensure all new changes are valid
        protected abstract void EnsureValidState();

        //applies new events to the entities of aggregates
        protected void ApplyToEntity(IInternalEventHandler entity, object @event)
            => entity?.Handle(@event);

        void IInternalEventHandler.Handle(object @event) => When(@event);
    }
}
