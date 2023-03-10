using System.Collections.Generic;

public interface IJSON
{
    string GetDataType();

    string Serialize();

    void Deserialize(Dictionary<string, object> json);
}
