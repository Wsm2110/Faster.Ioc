using Faster.Ioc.Collections;
using System;

namespace Faster.Ioc.Contracts
{
    internal interface IExpressionGenerator
    {
        Func<Scoped, object> Create(Type type, HashMap delegates);
    }
}