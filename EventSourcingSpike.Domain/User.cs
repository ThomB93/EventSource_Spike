using EventSourcingSpike.Framework;
using System;
using System.Security.Cryptography;

namespace EventSourcingSpike.Domain
{
    public class User : AggregateRoot<UserId>
    {
        // Aggregate state properties
        public Name Name { get; private set; }

        public User(UserId id, Name name)
            => Apply(new Events.UserRegistered
            {
                UserId = id,
                Name = name
            });

        public void UpdateName(Name name)
            => Apply(new Events.UserNameUpdated
            {
                UserId = Id,
                Name = name
            });

        protected override void When(object @event)
        {
            switch (@event)
            {
                case Events.UserRegistered e:
                    Id = new UserId(e.UserId);
                    Name = new Name(e.Name);
                    
                    break;
                case Events.UserNameUpdated e:
                    Name = new Name(e.Name);
                    break;
            }
        }

        protected override void EnsureValidState() { }

        // Satisfy the serialization requirements
        protected User() { }
    }
}
