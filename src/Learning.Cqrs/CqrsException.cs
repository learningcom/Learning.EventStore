using System;

namespace Learning.Cqrs
{
    public class CqrsException : Exception
    {
        public CqrsException(string message) : base(message)
        {
        }
    }
}

