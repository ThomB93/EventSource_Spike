using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingSpike.User
{
    public static class UserQueries
    {
        public static UserReadModel Query(
            this IEnumerable<UserReadModel> items,
            UserQueryModels.GetUser query)
            => items.FirstOrDefault(x => x.UserId == query.UserId);
    }
}
