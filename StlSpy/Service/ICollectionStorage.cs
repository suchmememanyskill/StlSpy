using System.Collections.Generic;
using System.Threading.Tasks;
using StlSpy.Model;
using StlSpy.Model.PostsEndpoint;

namespace StlSpy.Service;

public interface ICollectionStorage
{
    Task<GenericCollection?> GetPosts(CollectionId id);
    Task<List<CollectionId>> GetCollections();
    Task RemovePost(CollectionId id, string uid);
    Task AddPost(CollectionId id, Post post);
    Task<CollectionId> AddCollection(string name);
    Task RemoveCollection(CollectionId id);
    Task<bool> IsPostPartOfCollection(string uid);
    Task<bool> IsPostPartOfCollection(string uid, CollectionId id);
    string Name();
}