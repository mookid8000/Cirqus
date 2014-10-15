namespace d60.Cirqus.MongoDb.Events
{
    public interface IJsonEventMutator
    {
        string Mutate(string jsonText);
    }
}