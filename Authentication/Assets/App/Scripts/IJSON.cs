using System.Collections.Generic;

public interface IJSON
{
    string Serialize();

    void Deserialize(Dictionary<string, object> json);
}
