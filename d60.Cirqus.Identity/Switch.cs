using System;

namespace d60.Cirqus.Identity
{
    public class Switch<TReturn> where TReturn:class
    {
        readonly object subject;
        TReturn result;

        public Switch(object subject)
        {
            this.subject = subject;
        }

        public Switch<TReturn> Match<T>(Func<TReturn> func)
        {
            var asType = subject as Type;
            if (asType != null && asType == typeof(T))
            {
                result = func();
            }

            return Match<T>(x => func());
        }

        public Switch<TReturn> Match<T>(Func<T, TReturn> func)
        {
            if (subject is T)
            {
                result = func((T) subject);
            }

            return this;
        }

        public TReturn Else(TReturn value)
        {
            return result ?? value;
        }

        public TReturn OrThrow(Exception exception)
        {
            if (result != null)
                return result;
            
            throw exception;
        }
    }

    public class Switch
    {
        readonly object subject;
        bool match;

        public Switch(object subject)
        {
            this.subject = subject;
        }

        public Switch Match<T>(Action<T> action)
        {
            if (subject is T)
            {
                match = true;
                action((T)subject);
            }

            return this;
        }

        public void OrThrow(Exception exception)
        {
            if (!match) throw exception;
        }
    }
}