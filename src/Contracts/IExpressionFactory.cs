using Faster.Ioc.Collections;
using Faster.Ioc.Models;
using System;

namespace Faster.Ioc.Contracts
{
    internal interface IExpressionFactory
    {
        Func<Scoped, object> Get(Type type);
    }
}