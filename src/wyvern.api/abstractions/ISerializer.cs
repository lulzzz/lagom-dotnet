namespace wyvern.api.abstractions
{
    /// <summary>
    /// Serializer interface
    /// </summary>
    public interface ISerializer
    {
        byte[] Serialize<T>(T obj);
    }
}