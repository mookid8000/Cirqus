namespace d60.EventSorcerer.Numbers
{
    class CachingSequenceNumberGenerator : ISequenceNumberGenerator
    {
        long _current;
        public CachingSequenceNumberGenerator(long first)
        {
            _current = first;
        }

        public long Next()
        {
            return _current++;
        }
    }
}