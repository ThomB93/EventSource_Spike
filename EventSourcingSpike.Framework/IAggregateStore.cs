using System;
using System.Threading.Tasks;

namespace EventSourcingSpike.Framework
{
    //Generic repository interface.
    //Persistence aspects are handled in the same way for all aggregates
    public interface IAggregateStore
    {
        //Method must be implemented to check whether an aggregate exists in the event store
        Task<bool> Exists<T, TId>(TId aggregateId);
        //Method used to save changes to or create a new aggregate in the event store
        Task Save<T, TId>(T aggregate) where T : AggregateRoot<TId>;
        //Method used to load changes to an aggregate from the event store based on the aggregate ID
        Task<T> Load<T, TId>(TId aggregateId) where T : AggregateRoot<TId>;
    }
}
