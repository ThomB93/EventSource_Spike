using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventSourcingSpike.Domain;

namespace EventSourcingSpike.User
{
    public class UserQueryHandler
    {
        private readonly UserService _userService;
        private readonly IEnumerable<UserReadModel> _items;

        public UserQueryHandler(IEnumerable<UserReadModel> items, UserService userService)
        {
            _items = items;
            _userService = userService;
        } 

        public UserReadModel GetSingleUser(UserQueryModels.GetUser request)
        {
            return _userService.Query(_items, request);
        }
        public List<UserReadModel> GetAllUsers()
        {
            return _userService.QueryAll(_items);
        }

    }
}
