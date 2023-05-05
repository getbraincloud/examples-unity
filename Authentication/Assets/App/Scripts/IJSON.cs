using System.Collections.Generic;

public interface IJSON
{
    string GetDataType();

    Dictionary<string, object> GetDictionary();

    string Serialize();

    void Deserialize(Dictionary<string, object> json);
}
