using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Ioc.Comparer
{
    internal class RegistrationEqualityComparer : IEqualityComparer<Registration>
    {
        public bool Equals(Registration x, Registration y)
        {
            if (!ReferenceEquals(x.RegisteredType, y.RegisteredType))
            {
                return false;
            }

            return ReferenceEquals(x.ReturnType, y.ReturnType);
        }

        public int GetHashCode(Registration obj)
        {
            throw new NotImplementedException();
        }
    }
}
