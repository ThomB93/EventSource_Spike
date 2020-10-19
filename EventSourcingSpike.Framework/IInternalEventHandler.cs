using System;
using System.Collections.Generic;
using System.Text;

namespace EventSourcingSpike.Framework
{
    public interface IInternalEventHandler
    {
        void Handle(object @event);
    }
}
