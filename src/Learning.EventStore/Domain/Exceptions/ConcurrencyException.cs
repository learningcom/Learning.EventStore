using System;

namespace Learning.EventStore.Domain.Exceptions
{
    public class ConcurrencyException : System.Exception
    {
        public ConcurrencyException(string id)
            : base($"A different version than expected was found in aggregate {id}")
        { }
    }
}