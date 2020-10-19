using System;
using System.Collections.Generic;
using System.Text;

namespace EventSourcingSpike.Domain
{
    public static class Events
    {
        public class UserRegistered
        {
            public Guid UserId { get; set; }
            public string Name { get; set; }
        }
        public class UserNameUpdated
        {
            public Guid UserId { get; set; }
            public string Name { get; set; }
        }

    }
}
