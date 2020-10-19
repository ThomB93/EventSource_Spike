using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingSpike.User
{
    public static class Contracts
    {
        public class RegisterUser
        {
            public Guid UserId { get; set; }
            public string Name { get; set; }
        }

        public class UpdateUserName
        {
            public Guid UserId { get; set; }
            public string Name { get; set; }
        }

    }
}
