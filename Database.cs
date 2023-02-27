using Jekbot.Models;
using LiteDB;

namespace Jekbot;

public class Database
{
    public Database(string directory, string dbFilename)
    {
        db = new LiteDatabase(Path.Combine(directory, $"{dbFilename}.db"));
        SetupCollectionNameResolver(ref db.Mapper.ResolveCollectionName);
        db.Checkpoint();
    }

    private static void SetupCollectionNameResolver(ref Func<Type, string> resolveCollectionName)
    {
        var old = resolveCollectionName;
        resolveCollectionName = (t) => {
            var result = old(t);
            for (var decl = t.DeclaringType; decl != null; decl = decl.DeclaringType)
                result = $"{old(decl)}_{result}";

            return result;
        };
    }

    public ILiteQueryable<T> Select<T>()
    {
        return Establish<T>().Query();
    }

    public void Insert<T>(T item)
    {
        Establish<T>().Insert(item);
    }

    public void InsertOrUpdate<T>(T item)
    {
        Establish<T>().Upsert(item);
    }

    public bool Delete<T>(T item) where T : ModelBase
    {
        return Establish<T>().Delete(item.Key);
    }

    public int DeleteAll<T>() where T : ModelBase
    {
        return Establish<T>().DeleteAll();
    }

    public bool Update<T>(T item) where T : ModelBase
    {
        return Establish<T>().Update(item);
    }

    private ILiteCollection<T> Establish<T>()
    {
        var collection = db.GetCollection<T>();
        return collection;
    }

    public SingletonWrapper<T> GetSingleton<T>() where T : ModelBase, new()
    {
        return new SingletonWrapper<T>(this);
    }

    public class SingletonWrapper<T> : IDisposable where T : ModelBase, new()
    {
        public T Value { get; }

        public SingletonWrapper(Database database)
        {
            this.database = database;
            Value = database.Select<T>().SingleOrDefault() ?? new T();
        }

        public void Dispose()
        {
            database.InsertOrUpdate(Value);
        }

        private readonly Database database;
    }

    private readonly LiteDatabase db;
}
