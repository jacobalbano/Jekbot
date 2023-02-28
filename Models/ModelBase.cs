using LiteDB;

namespace Jekbot.Models;

public abstract record class ModelBase
{
    [BsonId]
    public Guid Key { get; init; } = Guid.NewGuid();
}

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class IndexedAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
public class BsonConverterAttribute : Attribute
{
    public Type ConverterType { get; }

    public BsonConverterAttribute(Type converterType)
    {
        ConverterType = converterType;
    }

}

public abstract class BsonConverter
{

}