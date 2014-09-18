using System;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Playground
{
    [TestFixture]
    public class TestTypeHashCodes
    {
        /// <summary>
        /// 11353497    !=    15401971    ?
        /// 49504964    !=    29664548    ?
        /// 
        /// SomeType hash code: 11353497
        /// After adding a property: SomeType hash code: 11353497
        /// Conclusion: Cannot use GetHashCode on Type as a signature
        /// </summary>
        [Test]
        public void HowDoesItWork()
        {
            CompareTypes(typeof (SomeType), typeof (AnotherType));
            CompareTypes(typeof (N1.SomeClass), typeof (N2.SomeClass));
        }

        [Test]
        public void WhatIsDefaultOfNullableLong()
        {
            var value = false ? 23L : default(long?);

            Console.WriteLine("It is {0}", value);
        }


        static void CompareTypes(Type firstType, Type secondType)
        {
            var firstHashCode = firstType.GetHashCode();
            var secondHashCode = secondType.GetHashCode();
            Console.WriteLine("{0}    !=    {1}    ?", firstHashCode, secondHashCode);
            Assert.That(firstHashCode, Is.Not.EqualTo(secondHashCode));
        }

        class SomeType
        {
            public string Hej { get; set; }
        }
        class AnotherType { }
    }

    namespace N1
    {
        class SomeClass { }
    }

    namespace N2
    {
        class SomeClass { }
    }
}