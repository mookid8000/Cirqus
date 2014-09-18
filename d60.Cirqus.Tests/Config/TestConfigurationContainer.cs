using System;
using System.Linq;
using d60.Cirqus.Config.Configurers;
using NUnit.Framework;

namespace d60.Cirqus.Tests.Config
{
    [TestFixture]
    public class TestConfigurationContainer : FixtureBase
    {
        ConfigurationContainer _container;

        protected override void DoSetUp()
        {
            _container = new ConfigurationContainer();
        }

        [Test]
        public void ThrowsWhenNoResolverIsPresent()
        {
            Assert.Throws<ResolutionException>(() => _container.CreateContext().Get<string>());
        }

        [Test]
        public void CanGetPrimaryInstance()
        {
            _container.Register(c => "hej");

            var resolvedString = _container.CreateContext().Get<string>();

            Assert.That(resolvedString, Is.EqualTo("hej"));
        }

        [Test]
        public void CanGetDecoratedInstance()
        {
            _container.Register(c => "hej");
            _container.Register(c => c.Get<string>() + " med dig", decorator: true);

            var resolvedString = _container.CreateContext().Get<string>();

            Assert.That(resolvedString, Is.EqualTo("hej med dig"));
        }

        [Test]
        public void CanGetDecoratedInstanceWithAnArbitraryNumberOfDecorators()
        {
            _container.Register(c => "hej");

            Enumerable.Range(1, 7)
                .ToList()
                .ForEach(tal => _container.Register(c => c.Get<string>() + " " + tal, decorator: true));

            var resolvedString = _container.CreateContext().Get<string>();

            Assert.That(resolvedString, Is.EqualTo("hej 1 2 3 4 5 6 7"));
        }

        [Test]
        public void CanGetDecoratedInstanceWithAnArbitraryNumberOfInterleavedDecorators()
        {
            _container.Register(c => "1");
            _container.Register(c => int.Parse(c.Get<string>()) + 1);

            _container.Register(c => c.Get<int>().ToString() + "2", decorator: true);
            _container.Register(c => int.Parse(c.Get<string>()) + 2, decorator: true);

            var resolvedString = _container.CreateContext().Get<int>();

            Assert.That(resolvedString, Is.EqualTo(24)); //..... ok, this is what happens:

            //  we resolve an int =>
            //      we resolve a string =>
            //          we resolve an int =>
            //              we resolve a string => 
            //              return "1"
            //          return int.parse("1") + 1 = 2
            //      return "2".toString() + "2" = "22"
            //  return int.parse("22") + 2 = 24
            //  = 24!
        }

        [Test]
        public void CanRegisterAndGetInstance()
        {
            const string friendlyInstance = "hej med dig min ven";
            _container.RegisterInstance(friendlyInstance);

            var instance = _container.CreateContext().Get<string>();

            Assert.That(instance, Is.EqualTo(friendlyInstance));
        }

        [Test]
        public void CannotRegisterInstanceMultipleTimesByDefault()
        {
            const string friendlyInstance = "hej med dig min ven";
            _container.RegisterInstance(friendlyInstance);

            Assert.Throws<InvalidOperationException>(() => _container.RegisterInstance("hej igen"));
        }

        [Test]
        public void CanRegisterMultipleInstancesIfMultipleIsSpecified()
        {
            _container.RegisterInstance("hej", multi:true);
            _container.RegisterInstance("med", multi:true);
            _container.RegisterInstance("dig", multi:true);

            var all = _container.CreateContext().GetAll<string>();

            Assert.That(string.Join(" ", all), Is.EqualTo("hej med dig"));
        }
    }
}