using d60.EventSorcerer.Views.Basic;

namespace d60.EventSorcerer.MongoDb.Views
{
    public interface IMongoDbView : IView
    {
        string Id { get; set; } 
    }
}