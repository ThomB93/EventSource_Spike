using System;
using System.Collections.Generic;
using System.Text;
using EventSourcingSpike.Framework;

namespace EventSourcingSpike.Domain
{
    public class Name : Value<Name>
    {
        public string Value { get; private set; }

        internal Name(string value) => Value = value;

        public static Name FromString(string fullName)
        {
            if (fullName.Length < 0 || fullName == null)
                throw new ArgumentNullException(nameof(fullName));

            return new Name(fullName);
        }

        public static implicit operator string(Name fullName)
            => fullName.Value;

        // Satisfy the serialization requirements
        protected Name() { }
    }
}
