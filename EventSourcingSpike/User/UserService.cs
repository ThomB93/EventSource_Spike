using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventSourcingSpike.Framework;
using EventSourcingSpike.Domain;
using EventStore.ClientAPI;

namespace EventSourcingSpike.User
{
    public class UserService
    {
        private readonly IAggregateStore _store;

        public UserService(IAggregateStore store)
        {
            _store = store;
        }

        //Handles the creation of a new user
        public async Task HandleCreate(Contracts.RegisterUser cmd)
        {
            //check if user already exists in event store
            if (await _store
                .Exists<Domain.User, UserId>(
                    new UserId(cmd.UserId)
                ))
                throw new InvalidOperationException(
                    $"Entity with id {cmd.UserId} already exists"
                );

            //create new instance of user from the command
            var user = new Domain.User(
                new UserId(cmd.UserId),
                Name.FromString(cmd.Name)
            );

            //save the new user to the event store
            await _store
                .Save<Domain.User, UserId>(
                    user
                );
        }
        //method for handling updates to the user, can accept any kind of update opertion
        public async Task HandleUpdate<T, TId>(TId aggregateId, Action<T> operation)
            where T : AggregateRoot<TId>
        {
            //loads the user from the event store via the user ID
            var aggregate = await _store.Load<T, TId>(aggregateId);
            if (aggregate == null)
                throw new InvalidOperationException($"Entity with id {aggregateId.ToString()} cannot be found");

            //performs the update operation sent as request
            operation(aggregate);
            //saves the changes to the event store (creates a new event)
            await _store.Save<T, TId>(aggregate);
        }

        //method for fetching a user by their ID
        public UserReadModel Query(
            IEnumerable<UserReadModel> items,
            UserQueryModels.GetUser query)
            => items.FirstOrDefault(x => x.UserId == query.UserId);

        //method for fetching all user aggregates
        public List<UserReadModel> QueryAll(
            IEnumerable<UserReadModel> items)
            => items.Where(x => x.Name.Length > 0).ToList();
    }
}
