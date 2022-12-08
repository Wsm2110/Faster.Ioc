using System;
using System.Collections.Generic;
using System.Linq;

namespace Faster.Ioc
{
    public class CircularReference
    {
        private readonly Stack<Type> _circularReferences = new();

        public void Add(Type type)
        {
            if (_circularReferences.Any(x => ReferenceEquals(x, type)))
            {
                _circularReferences.Push(type);
                throw new InvalidOperationException("Found circular reference: " + string.Join(" -> ", _circularReferences.Select(x => x.Name)));
            }

            _circularReferences.Push(type);
        }

        public void Exit()
        {
            _circularReferences.Pop();
        }
    }
}
