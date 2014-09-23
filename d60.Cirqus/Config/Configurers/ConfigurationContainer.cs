using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using d60.Cirqus.Aggregates;
using d60.Cirqus.Events;
using d60.Cirqus.Views;

namespace d60.Cirqus.Config.Configurers
{
    public class ConfigurationContainer : IRegistrar
    {
        readonly List<ResolutionContext.Resolver> _resolvers = new List<ResolutionContext.Resolver>();

        readonly List<Action<Options>> _optionActions = new List<Action<Options>>();

        public ResolutionContext CreateContext()
        {
            return new ResolutionContext(_resolvers);
        }

        public void RegisterInstance<TService>(TService instance, bool multi = false)
        {
            Register(c => instance, multi: multi);
        }

        public void Register<TService>(Func<ResolutionContext, TService> serviceFactory, bool decorator = false, bool multi = false)
        {
            var havePrimaryResolverAlready = HasService<TService>(checkForPrimary: true);

            if (!decorator && !multi && havePrimaryResolverAlready)
            {
                var message = string.Format("Attempted to register factory method for {0} as non-decorator," +
                                            " but there's already a primary resolver for that service! There" +
                                            " can be only one primary resolver for each service type (but" +
                                            " any number of decorators)",
                    typeof(TService));

                throw new InvalidOperationException(message);
            }

            if (multi && havePrimaryResolverAlready)
            {
                var message = string.Format("Attempted to register factory method for {0} as multi, but there's" +
                                            " already a primary resolver for that service! When doing multi-registrations," +
                                            " it is important that they're all configured to be multi-registrations!",
                    typeof (TService));

                throw new InvalidOperationException(message);
            }

            var resolver = new ResolutionContext.Resolver<TService>(serviceFactory, decorator, multi);

            if (decorator)
            {
                _resolvers.Insert(0, resolver);
                return;
            }

            _resolvers.Add(resolver);
        }

        public void RegisterOptionConfig(Action<Options> optionAction)
        {
            _optionActions.Add(optionAction);
        }

        public bool HasService<TService>(bool checkForPrimary = false)
        {
            return checkForPrimary

                ? _resolvers
                    .OfType<ResolutionContext.Resolver<TService>>()
                    .Any(s => !s.Decorator && !s.Multi)

                : _resolvers
                    .OfType<ResolutionContext.Resolver<TService>>()
                    .Any();
        }

        internal void InsertResolversInto(ConfigurationContainer otherContainer)
        {
            otherContainer._resolvers.AddRange(_resolvers);
        }

        internal void LogServicesTo(TextWriter writer)
        {
            var es = _resolvers.OfType<ResolutionContext.Resolver<IEventStore>>().ToList();
            var agg = _resolvers.OfType<ResolutionContext.Resolver<IAggregateRootRepository>>().ToList();
            var ed = _resolvers.OfType<ResolutionContext.Resolver<IEventDispatcher>>().ToList();

            writer.WriteLine(@"----------------------------------------------------------------------
Event store:
{0}

Aggregate root repository:
{1}

Event dispatcher:
{2}
----------------------------------------------------------------------",
                                                                       Format(es), Format(agg), Format(ed));
        }

        string Format<TService>(List<ResolutionContext.Resolver<TService>> agg)
        {
            var primary = agg.Where(r => !r.Decorator)
                .ToList();

            var decorators = agg.Where(r => r.Decorator)
                .ToList();

            var builder = new StringBuilder();

            if (primary.Any())
            {
                builder.AppendLine(@"    Primary:");
                builder.AppendLine(string.Join(Environment.NewLine, primary.Select(p => string.Format("        {0}", p.Type))));
            }

            if (decorators.Any())
            {
                builder.AppendLine(@"    Decorators:");
                builder.AppendLine(string.Join(Environment.NewLine, decorators.Select(p => string.Format("        {0}", p.Type))));
            }

            return builder.ToString();
        }
    }
}