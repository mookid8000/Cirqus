using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Raven.Abstractions.Data;
using Raven.Client;
using Raven.Client.Linq;

namespace d60.Cirqus.RavenDB
{
    public static class ISyncAdvancedSessionOperationExtensions
    {
        public static IEnumerator<StreamResult<T>> NonStaleResultStream<T>(this ISyncAdvancedSessionOperation op, IRavenQueryable<T> q, Expression<Func<T, bool>> where, Expression<Func<T, long>> order)
        {
            q.Customize(c => c.WaitForNonStaleResults()).Where(where).OrderBy(order).Count();
            return op.Stream(q.Where(where).OrderBy(order));
        }
    }
}