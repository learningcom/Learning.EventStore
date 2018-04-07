using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Learning.Cqrs
{
    public interface ICommand
    {
    }

    public interface ICommand<TReturn>
    {
    }
}
