namespace Model.Data;

public interface ISerializer<T>
{
    void Serialize(T data, string filePath);
    T? Deserialize(string filePath);
    bool IsValidFile(string filePath);
}
