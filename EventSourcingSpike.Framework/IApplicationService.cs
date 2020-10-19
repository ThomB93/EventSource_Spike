using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingSpike.Framework
{
    public interface IApplicationService
    {
        Task Handle(object command);
    }
}
