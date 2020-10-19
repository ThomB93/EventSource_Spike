using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventSourcingSpike.Domain;
using EventStore.ClientAPI;

namespace EventSourcingSpike.User
{
    public class UserCommandHandler
    {
        private readonly UserService _userService;

        public UserCommandHandler(UserService userService)
            => _userService = userService;

        //Registers a new user to the event store
        public async void RegisterUser(Contracts.RegisterUser request)
        {
            await _userService.HandleCreate(request);
        }

        public async void UpdateUserName(Contracts.UpdateUserName request)
        {
            await _userService.HandleUpdate<Domain.User, UserId>(new UserId(request.UserId), user => user.UpdateName(
                Name.FromString(request.Name)
            ));
        }
    }
}
