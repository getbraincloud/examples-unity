using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private float MoveSpeed;

    //Networkvariable only works when NetworkBehaviour is involved.
    //Default clients cant write to this variable, only the server but everyone can read it.
    private NetworkVariable<int> randomNumber = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    
    private NetworkVariable<MyCustomData> randomData = new NetworkVariable<MyCustomData>(new MyCustomData(), NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    public struct MyCustomData : INetworkSerializable
    {
        public int _someInt;
        public bool _thisIsABool;
        //Cant use normal string to use in network serializable because its a type rather than a value type variable.
        //FixedStringXXXBytes will preallocate the memory for the specific string, but it has to be the right size cause the data size cannot grow or shrink.
        //1 character = 1 byte so 50 characters should fine with 128 bytes.
        public FixedString128Bytes _messageString;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _someInt);
            serializer.SerializeValue(ref _thisIsABool);
            serializer.SerializeValue(ref _messageString);
        }
    }

    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += ( previousValue,  newValue) =>
        {
            Debug.Log($"ClientID: {OwnerClientId}, randomNumber: {randomNumber.Value}");    
        };

        randomData.OnValueChanged += ( previousValue,  newValue) =>
        {
            Debug.Log($"ClientID: {OwnerClientId}, randomData int: {randomData.Value._someInt}, randomData bool: {randomData.Value._thisIsABool}, message string:{randomData.Value._messageString}");
        };
    }

    // Update is called once per frame
    void Update()
    {

        if (!IsOwner) return;
        
        if(Input.GetKey(KeyCode.R))
        {
            randomData.Value = new MyCustomData {_someInt = Random.Range(0, 100), _thisIsABool = !randomData.Value._thisIsABool, _messageString = "Woow look its a message !"};            
        }
        
        if(Input.GetKey(KeyCode.T))
        {
            randomNumber.Value = Random.Range(0, 100);
        }
        
        if(Input.GetKey(KeyCode.Y))
        {

            TestServerRpc(new ServerRpcParams());
        }
        
        if(Input.GetKey(KeyCode.U))
        {
            TestClientRpc(new ClientRpcParams{Send = new ClientRpcSendParams{TargetClientIds = new List<ulong>{ 1 } } });
        }
    
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) moveDirection.z = +1f;
        if (Input.GetKey(KeyCode.S)) moveDirection.z = -1f;
        if (Input.GetKey(KeyCode.A)) moveDirection.x = -1f;
        if (Input.GetKey(KeyCode.D)) moveDirection.x = +1f;
        transform.position += moveDirection * MoveSpeed * Time.deltaTime;
    }
    
    //ServerRpcParams include the sender client ID so the server can determine who executed this function for the server.
    [ServerRpc]
    private void TestServerRpc(ServerRpcParams serverRpcParams) 
    {
        Debug.Log("TestServerRpc " + OwnerClientId + "; " + serverRpcParams.Receive.SenderClientId);
    }
    
    //This can only be executed from the server for clients to receive, this applies to all clients.
    //ClientRpcParams holds a list of all client ids, so we can execute this function for a specific client.
    [ClientRpc]
    private void TestClientRpc(ClientRpcParams clientRpcParams)
    {
        Debug.Log("TestClientRpc");
    }
}
