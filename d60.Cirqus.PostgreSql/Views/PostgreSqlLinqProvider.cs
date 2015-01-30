using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace d60.Cirqus.PostgreSql.Views
{
    class PostgreSqlLinqProvider<TViewInstance> : IQueryable<TViewInstance>
    {
        public PostgreSqlLinqProvider()
        {
            ElementType = typeof (TViewInstance);
            Expression = Expression.Constant(this);
            Provider = new PostgreSqlQueryProvider();
        }

        public IEnumerator<TViewInstance> GetEnumerator()
        {
            return ((IEnumerable<TViewInstance>) Provider.Execute(Expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public Expression Expression { get; private set; }
        public Type ElementType { get; private set; }
        public IQueryProvider Provider { get; private set; }

        public override string ToString()
        {
            return ((PostgreSqlQueryProvider)Provider).GetQueryText(Expression);
        }
    }

    class PostgreSqlQueryProvider : IQueryProvider
    {
        public IQueryable CreateQuery(Expression expression)
        {
            Console.WriteLine(GetQueryText(expression));
            throw new NotImplementedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            Console.WriteLine(GetQueryText(expression));
            throw new NotImplementedException();
        }

        public object Execute(Expression expression)
        {
            Console.WriteLine(GetQueryText(expression));
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(Expression expression)
        {
            Console.WriteLine(GetQueryText(expression));

            throw new NotImplementedException();
        }

        public string GetQueryText(Expression expression)
        {
            var translator = new QueryTranslator();
            
            return translator.Translate(expression);
        }
    }

    class QueryTranslator : ExpressionVisitor
    {
        readonly StringBuilder _sb = new StringBuilder();

        public string Translate(Expression expression)
        {
            Visit(expression);
            var result = _sb.ToString();
            _sb.Clear();
            return result;
        }

        static Expression StripQuotes(Expression e)
        {
            while (e.NodeType == ExpressionType.Quote)
            {
                e = ((UnaryExpression)e).Operand;
            }
            return e;
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "Where")
            {
                _sb.Append("SELECT * FROM (");
                this.Visit(m.Arguments[0]);
                _sb.Append(") AS T WHERE ");
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                this.Visit(lambda.Body);
                return m;
            }
            if (m.Method.DeclaringType == typeof(Queryable) && m.Method.Name == "First")
            {
                _sb.Append("SELECT top 1 * FROM (");
                this.Visit(m.Arguments[0]);
                _sb.Append(") AS T WHERE ");
                LambdaExpression lambda = (LambdaExpression)StripQuotes(m.Arguments[1]);
                this.Visit(lambda.Body);
                return m;
            }
            throw new NotSupportedException(string.Format("The method '{0}' is not supported", m.Method.Name));
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    _sb.Append(" NOT ");
                    this.Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", u.NodeType));
            }
            return u;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            _sb.Append("(");
            this.Visit(b.Left);
            switch (b.NodeType)
            {
                case ExpressionType.And:
                    _sb.Append(" AND ");
                    break;
                case ExpressionType.Or:
                    _sb.Append(" OR");
                    break;
                case ExpressionType.Equal:
                    _sb.Append(" = ");
                    break;
                case ExpressionType.NotEqual:
                    _sb.Append(" <> ");
                    break;
                case ExpressionType.LessThan:
                    _sb.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    _sb.Append(" <= ");
                    break;
                case ExpressionType.GreaterThan:
                    _sb.Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    _sb.Append(" >= ");
                    break;
                default:
                    throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", b.NodeType));
            }
            this.Visit(b.Right);
            _sb.Append(")");
            return b;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            IQueryable q = c.Value as IQueryable;
            if (q != null)
            {
                // assume constant nodes w/ IQueryables are table references
                _sb.Append("SELECT * FROM ");
                _sb.Append(q.ElementType.Name);
            }
            else if (c.Value == null)
            {
                _sb.Append("NULL");
            }
            else
            {
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        _sb.Append(((bool)c.Value) ? 1 : 0);
                        break;
                    case TypeCode.String:
                        _sb.Append("'");
                        _sb.Append(c.Value);
                        _sb.Append("'");
                        break;
                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));
                    default:
                        _sb.Append(c.Value);
                        break;
                }
            }
            return c;
        }

        /// <summary>
        /// Copied from somewhere else
        /// </summary>
        protected override Expression VisitMember(MemberExpression m)
        {
            if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
            {
                _sb.Append(m.Member.Name);
                return m;
            }
            throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
        }

        //protected override Expression VisitMemberAccess(MemberExpression m)
        //{
        //    if (m.Expression != null && m.Expression.NodeType == ExpressionType.Parameter)
        //    {
        //        _sb.Append(m.Member.Name);
        //        return m;
        //    }
        //    throw new NotSupportedException(string.Format("The member '{0}' is not supported", m.Member.Name));
        //}
    }
 

    static class TypeSystem
    {
        public static Type GetElementType(Type seqType)
        {
            Type ienum = FindIEnumerable(seqType);
            if (ienum == null) return seqType;
            return ienum.GetGenericArguments()[0];
        }
        
        static Type FindIEnumerable(Type seqType)
        {
            if (seqType == null || seqType == typeof(string))
                return null;
            if (seqType.IsArray)
                return typeof(IEnumerable<>).MakeGenericType(seqType.GetElementType());
            if (seqType.IsGenericType)
            {
                foreach (Type arg in seqType.GetGenericArguments())
                {
                    Type ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                    if (ienum.IsAssignableFrom(seqType))
                    {
                        return ienum;
                    }
                }
            }
            Type[] ifaces = seqType.GetInterfaces();
            if (ifaces != null && ifaces.Length > 0)
            {
                foreach (Type iface in ifaces)
                {
                    Type ienum = FindIEnumerable(iface);
                    if (ienum != null) return ienum;
                }
            }
            if (seqType.BaseType != null && seqType.BaseType != typeof(object))
            {
                return FindIEnumerable(seqType.BaseType);
            }
            return null;
        }
    }
}