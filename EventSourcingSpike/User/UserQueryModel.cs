using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingSpike.User
{
    public static class UserQueryModels
    {
        public class GetUser
        {
            public Guid UserId { get; set; }
        }
        public class GetUsers
        {
            public int Page { get; set; }
            public int PageSize { get; set; }
        }
    }
}
