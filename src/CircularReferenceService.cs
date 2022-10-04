using System;
using System.Collections.Generic;
using System.Linq;

namespace Faster.Ioc
{
    public class CircularReferenceService
    {
        private readonly Stack<Type> _circularReferences = new();

        public void Add(Registration reg)
        {
            if (_circularReferences.Any(x => x == reg.RegisteredType))
            {
                _circularReferences.Push(reg.RegisteredType);
                throw new InvalidOperationException("Found circular reference: " + string.Join(" -> ", _circularReferences.Select(x => x.Name)));
            }

            _circularReferences.Push(reg.RegisteredType);

        }

        public void Exit()
        {
            _circularReferences.Pop();
        }
    }
}
