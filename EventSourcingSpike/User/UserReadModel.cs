using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingSpike.User
{
    public class UserReadModel
    {
        public Guid UserId { get; set; }
        public string Name { get; set; }
    }

}
