using System;
using System.Collections.Generic;

namespace wyvern.api
{
    public interface IReactiveServices : IEnumerable<(Type, Type)>
    {
    }
}