using System;
using System.Collections.Generic;

namespace wyvern.api.abstractions
{
    public interface IReactiveServices : IEnumerable<(Type, Type)>
    {
    }
}