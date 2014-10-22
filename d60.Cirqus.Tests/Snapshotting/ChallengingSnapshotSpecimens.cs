using System.Collections.Generic;
using System.Linq;
using d60.Cirqus.Aggregates;

namespace d60.Cirqus.Tests.Snapshotting
{
    public class ChallengingSnapshotSpecimens
    {
        public class SimpleRootWithOrdinaryField : AggregateRoot
        {
            readonly string _thisIsAllIHave;

            public SimpleRootWithOrdinaryField()
            {
                _thisIsAllIHave = "hej";
            }

            public override int GetHashCode()
            {
                return CurrentSequenceNumber.GetHashCode()
                       ^ GlobalSequenceNumberCutoff.GetHashCode()
                       ^ Id.GetHashCode()
                       ^ _thisIsAllIHave.GetHashCode();
            }
        }

        public class SimpleRootWithPublicField : AggregateRoot
        {
            public readonly string ThisIsAllIHave;

            public SimpleRootWithPublicField()
            {
                ThisIsAllIHave = "hej";
            }

            public override int GetHashCode()
            {
                return CurrentSequenceNumber.GetHashCode()
                       ^ GlobalSequenceNumberCutoff.GetHashCode()
                       ^ Id.GetHashCode()
                       ^ ThisIsAllIHave.GetHashCode();
            }
        }

        public class SimpleRootWithProperty : AggregateRoot
        {
            public SimpleRootWithProperty()
            {
                ThisIsAllIHave = "hej";
            }

            public string ThisIsAllIHave { get; set; }

            public override int GetHashCode()
            {
                return CurrentSequenceNumber.GetHashCode()
                       ^ GlobalSequenceNumberCutoff.GetHashCode()
                       ^ Id.GetHashCode()
                       ^ ThisIsAllIHave.GetHashCode();
            }
        }

        public class SomeRootWithVariousDifficultThingsGoingOnForIt : AggregateRoot
        {
            readonly List<ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter> _nestedBadBoys;

            readonly IEnumerable<ThisOneHasPrivateFields> _enumerableOfStuff;

            readonly HashSet<ThisOneIsAbstract> _abstracts;

            public SomeRootWithVariousDifficultThingsGoingOnForIt()
            {
                _nestedBadBoys = new List<ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter>
                {
                    new ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter(1),
                    new ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter(2),
                    new ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter(3),
                };

                _enumerableOfStuff = new List<ThisOneHasPrivateFields>
                {
                    new ThisOneHasPrivateFields("private1"),
                    new ThisOneHasPrivateFields("private2"),
                };

                _abstracts = new HashSet<ThisOneIsAbstract>
                {
                    new ThisIsTheFirstImplementation(),
                    new ThisIsTheSecondImplementation()
                };
            }

            public override int GetHashCode()
            {
                return Id.GetHashCode()

                       ^

                       _nestedBadBoys
                           .Aggregate(_nestedBadBoys.Count.GetHashCode(),
                               (value, badBoy) => value ^ badBoy.GetHashCode())

                       ^

                       _enumerableOfStuff
                           .Aggregate(_enumerableOfStuff.Count().GetHashCode(),
                               (value, stuff) => value ^ stuff.GetHashCode())

                       ^

                       _abstracts
                           .Aggregate(_abstracts.Count.GetHashCode(),
                               (value, abs) => value ^ abs.GetHashCode());
            }
        }

        public class ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter
        {
            public ThisBadBoyIsNestedAndHasPropertyWithPrivateSetter(int number)
            {
                Number = number;
            }

            public int Number { get; private set; }

            public override int GetHashCode()
            {
                return Number.GetHashCode();
            }
        }

        public class ThisOneHasPrivateFields
        {
            readonly string _name;

            public ThisOneHasPrivateFields(string name)
            {
                _name = name;
            }

            public override int GetHashCode()
            {
                return _name.GetHashCode();
            }
        }

        public abstract class ThisOneIsAbstract
        {
            public abstract string WhoAreYou();
        }

        public class ThisIsTheFirstImplementation : ThisOneIsAbstract
        {
            public override string WhoAreYou()
            {
                return "first";
            }

            public override int GetHashCode()
            {
                return "first".GetHashCode();
            }
        }

        public class ThisIsTheSecondImplementation : ThisOneIsAbstract
        {
            public override string WhoAreYou()
            {
                return "second";
            }

            public override int GetHashCode()
            {
                return "second".GetHashCode();
            }
        }
    }
}