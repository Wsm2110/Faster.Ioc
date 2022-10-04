using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Faster.Ioc.Tests.Data
{
    public interface ISingletonOne : IDisposable
    {

        void Test();

    }


    public interface ISingletonCircularOne
    {
    }

    public interface ISingletonCircularTwo
    {
    }

    public interface ISingletonCircularThree
    {
    }


    public interface ISingletonTwo
    {
    }

    public interface ISingletonThree
    {
    }

    public interface ISingletonFour
    {
    }

    public interface ISingletonFive
    {
    }

    public interface ISingletonSpecial
    {
    }

    public interface ISingletonSpecialGeneric<T>
    {
    }

    public interface ISingletonSix
    {

    }

    public class SingletonCircularOne : ISingletonCircularOne
    {
        private readonly ISingletonCircularTwo _singletonCircularTwo;

        public SingletonCircularOne(ISingletonCircularTwo singletonCircularTwo)
        {
            _singletonCircularTwo = singletonCircularTwo;
           
        }
    }

    public class SingletonCircularTwo : ISingletonCircularTwo
    {
        private readonly ISingletonCircularThree _circularThree;

        public SingletonCircularTwo(ISingletonCircularThree circularThree)
        {
            _circularThree = circularThree;
        }
    }

    public class SingletonCircularTree : ISingletonCircularThree
    {
        private readonly ISingletonCircularFour _circularOne;
        private readonly ISingletonCircularFive _singletonCircularFive;

        public SingletonCircularTree(ISingletonCircularFour circularOne, ISingletonCircularFive singletonCircularFive)
        {
            _circularOne = circularOne;
            _singletonCircularFive = singletonCircularFive;
        }
    }


    public class SingletonCircularFour : ISingletonCircularFour
    {
        private readonly ISingletonCircularOne _circularOne;

        public SingletonCircularFour()
        {
            
        }
    }

    public interface ISingletonCircularFour
    {
    }

    public class SingletonCircularFive : ISingletonCircularFive
    {
        private readonly ISingletonCircularFour _circularOne;

        public SingletonCircularFive(ISingletonCircularFour circularOne, ISingletonCircularOne one)
        {
            _circularOne = circularOne;
        }
    }

    public interface ISingletonCircularFive
    {
    }

    public class SingletonTen : ISingletonTen
    {


    }

    public class SingletonNine : ISingletonNine
    {


    }

    public class SingletonEight : ISingletonEight
    {


    }

    public class SingletonSix : ISingletonSix
    {
        private readonly ISingletonOne _singletonOne;
        private readonly ISingletonTwo _singletonTwo;

        public SingletonSix(ISingletonOne singletonOne, ISingletonTwo singletonTwo)
        {
            _singletonOne = singletonOne;
            _singletonTwo = singletonTwo;
        }
    }
    public class SingletonFive : ISingletonFive
    {
        private readonly ISingletonThree _singletonThree;
        private readonly ISingletonFour _singletonFour;

        public SingletonFive(ISingletonThree singletonThree, ISingletonFour singletonFour)
        {
            _singletonThree = singletonThree;
            _singletonFour = singletonFour;
        }
    }

    public class SingletonSeven : ISingletonSeven
    {
        private readonly ISingletonSix _singletonSix;
        private readonly ISingletonFive _singletonFive;

        public SingletonSeven(ISingletonSix singletonSix, ISingletonFive singletonFive)
        {
            _singletonSix = singletonSix;
            _singletonFive = singletonFive;
        }
    }


    public class SingletonFour : ISingletonFour
    {
    }

    public class SingletonThree : ISingletonThree
    {
    }

    public class SingletonTwo : ISingletonTwo
    {
    }

    public class SingletonOne : ISingletonOne
    {
        public SingletonOne()
        {

        }

        public void Dispose()
        {
            //throw new NotImplementedException();
            //throw new NotImplementedException();
        }

        public void Test()
        {
//            throw new NotImplementedException();
        }
    }

    public class SingletonZero : ISingletonOne
    {
        private readonly ISingletonSpecial _special;

        public SingletonZero(ISingletonSpecial special)
        {
            _special = special;
        }

        public void Dispose()
        {
            //  throw new NotImplementedException();
        }

        public void Test()
        {
            //       throw new NotImplementedException();
        }
    }



    public class SingletonOneSpecialTwo : ISingletonSpecial
    {

    }

    public class SingletonOneSpecialTree: ISingletonSpecial
    {
       
    }


    public class SingletonOneSpecialGeneric<T> : ISingletonSpecialGeneric<T>
    {

    }


    public class Special
    {
        public IEnumerable<ISingletonOne> One { get; }

        public Special(IEnumerable<ISingletonOne> one)
        {
            One = one;
        }
    }


    public class SingletonOneSpecial : ISingletonSpecial
    {
        public readonly IEnumerable<ISingletonOne> _specials;

        public SingletonOneSpecial(IEnumerable<ISingletonOne> specials)
        {
            _specials = specials;

            foreach (var item in _specials)
            {
                
            }
        }
    }

    public class SingletonOnePartTwo : ISingletonOne
    {
        public void Dispose()
        {
            // throw new NotImplementedException();
        }

        public void Test()
        {
            
        }
    }

    public class ISingletonEight
    {
    }

    public class ISingletonNine
    {
    }
    public class ISingletonSeven
    {
    }


    public class ISingletonTen
    {
    }
}