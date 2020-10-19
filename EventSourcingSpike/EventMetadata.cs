using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventSourcingSpike
{
    public class EventMetadata
        {
            //custom metadata property, used instead of EventStore default EventMetadata
            public string ClrType { get; set; }
        }
}
