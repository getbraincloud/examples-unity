using BrainCloudUNETExample;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Runtime.InteropServices;
using System.Text;
using BrainCloud.JsonFx.Json;

namespace Gameframework
{
    public class BaseNetworkBehavior : BaseBehaviour
    {
        private const float DEFAULT_INTERVAL = 0.15f;

        public static float SEND_INTERVAL = DEFAULT_INTERVAL; // This should be dynamic based on packet load, but we just want to keep it simple here.
        public static float MSG_ENCODED = 2; // 0 = regular, 1 = json, 2, data stream , mixed

        #region public static
        public static void UpdateTransform(short in_netId, float hisPing, Vector3 pos, Vector3 eulerAngles, Vector3 velocity)
        {
            var myPing = (float)(GCore.Wrapper.Client.RelayService.LastPing * 0.0001);

            BaseNetworkBehavior[] allNetworkBehaviors = FindObjectsOfType<BaseNetworkBehavior>();
            foreach (BaseNetworkBehavior behavior in allNetworkBehaviors)
            {
                if (behavior._netId == in_netId)
                {
                    var combinedPings = (myPing + hisPing) * 0.001f;
                    combinedPings *= 0.5f; // Ping are round trip, here we only care about the one way time.

                    behavior.m_networkPosition = pos + velocity * combinedPings; // Predict where he currently is on his device

                    // Check if we loop around the angle so we don't glitch
                    if (behavior.m_networkRotation.z >= 270 && eulerAngles.z <= 90)
                    {
                        behavior.m_networkRotation.z -= 360;
                        behavior.m_rotation.z -= 360;
                    }
                    if (behavior.m_networkRotation.z <= 90 && eulerAngles.z >= 270)
                    {
                        behavior.m_networkRotation.z += 360;
                        behavior.m_rotation.z += 360;
                    }

                    behavior.m_rotationVelocity = eulerAngles - behavior.m_networkRotation;
                    behavior.m_networkRotation = eulerAngles + behavior.m_rotationVelocity * combinedPings; // Predict where he currently is on his device
                    behavior.m_networkVelocity = velocity;

                    // last synced
                    behavior.LastSyncedPing = hisPing;
                    //break;
                }
            }
        }

        public static void SendStart(string in_classtype, string in_netId, string in_data, Transform in_transform)
        {
            SendStart(in_classtype, in_netId, in_data, in_transform, Vector3.zero);
        }

        public static void SendStart(string in_classtype, short in_netId, string in_data, Transform in_transform)
        {
            SendStart(in_classtype, in_netId, in_data, in_transform, Vector3.zero);
        }

        public static void SendProjectileStart(string in_classtype, Dictionary<string, object> in_dict)
        {
            in_dict[OPERATION] = ENTITY_START;
            in_dict[TYPE] = in_classtype;
            SendStringOrByte(in_dict, '=', ';', true, true);// reliable channel
        }

        public static void SendStart(string in_classtype, short in_netId, string in_data, Transform in_transform, Vector3 velocity)
        {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json[OPERATION] = ENTITY_START;
            json[TYPE] = in_classtype;
            json[NET_ID] = in_netId;
            json[DATA] = in_data;
            ConvertToShort(velocity.x);
            ConvertToShort(velocity.y);
            ConvertToShort(velocity.z);
            if (GCore.Wrapper.Client.RelayService.LastPing != 0.0f) json[LAST_PING] = ConvertToShort(GCore.Wrapper.Client.RelayService.LastPing * 0.0001f);
            UpdateWithTransformData(ref json, in_transform);

            SendStringData(SerializeDict(json, '=', ';'), BrainCloud.BrainCloudRelay.TO_ALL_PLAYERS, true, true);
        }

        static public void SendStringData(string in_data, ulong to_netId, bool in_reliable = true, bool in_ordered = true, int in_channel = 0)
        {
            byte[] data = Encoding.ASCII.GetBytes(in_data);
            GCore.Wrapper.Client.RelayService.Send(data, to_netId, in_reliable, in_ordered, in_channel);
        }

        public static void SendStart(string in_classtype, string in_netId, string in_data, Transform in_transform, Vector3 velocity)
        {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json[OPERATION] = ENTITY_START;
            json[TYPE] = in_classtype;
            json[NET_ID] = in_netId;
            json[DATA] = in_data;
            ConvertToShort(velocity.x);
            ConvertToShort(velocity.y);
            ConvertToShort(velocity.z);
            if (GCore.Wrapper.Client.RelayService.LastPing != 0.0f) json[LAST_PING] = ConvertToShort(GCore.Wrapper.Client.RelayService.LastPing * 0.0001f);
            UpdateWithTransformData(ref json, in_transform);

            SendStringData(SerializeDict(json, '=', ';'), BrainCloud.BrainCloudRelay.TO_ALL_PLAYERS, true, true);
        }

        public static void SendStart(string in_classtype, int in_netId, Transform in_transform)
        {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json[OPERATION] = ENTITY_START;
            json[TYPE] = in_classtype;
            json[NET_ID] = in_netId;
            if (GCore.Wrapper.Client.RelayService.LastPing != 0.0f) json[LAST_PING] = ConvertToShort(GCore.Wrapper.Client.RelayService.LastPing * 0.0001f);
            UpdateWithTransformData(ref json, in_transform);

            SendStringData(SerializeDict(json, '=', ';'), BrainCloud.BrainCloudRelay.TO_ALL_PLAYERS, true, true);
        }

        // slowly moving to this 
        public static void SendProjectileDestroy(string in_classtype, Dictionary<string, object> in_dict)
        {
            in_dict[OPERATION] = ENTITY_DESTROY;
            in_dict[TYPE] = in_classtype;
            SendStringOrByte(in_dict, '=', ';', true, true);
        }

        public static void SendDestroy(string in_classtype, short in_netId, short in_data)
        {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json[OPERATION] = ENTITY_DESTROY;
            json[TYPE] = in_classtype;
            json[NET_ID] = in_netId;
            json[DATA] = in_data;

            SendStringData(SerializeDict(json, '=', ';'), BrainCloud.BrainCloudRelay.TO_ALL_PLAYERS, true, true);
        }

        public static void SendDestroy(string in_classtype, short in_netId, int in_data)
        {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json[OPERATION] = ENTITY_DESTROY;
            json[TYPE] = in_classtype;
            json[NET_ID] = in_netId;
            json[DATA] = in_data;

            SendStringData(SerializeDict(json, '=', ';'), BrainCloud.BrainCloudRelay.TO_ALL_PLAYERS, true, true);
        }

        public static void SendDestroy(string in_classtype, string in_netId, string in_data)
        {
            Dictionary<string, object> json = new Dictionary<string, object>();
            json[OPERATION] = ENTITY_DESTROY;
            json[TYPE] = in_classtype;
            json[NET_ID] = in_netId;
            json[DATA] = in_data;

            SendStringData(SerializeDict(json, '=', ';'), BrainCloud.BrainCloudRelay.TO_ALL_PLAYERS, true, true);
        }

        public static void UpdateWithTransformData(ref Dictionary<string, object> ref_dict, Transform in_trans)
        {
            if (in_trans == null) return;

            ref_dict[POSITION_X] = ConvertToShort(in_trans.position.x);
            ref_dict[POSITION_Y] = ConvertToShort(in_trans.position.y);
            ref_dict[POSITION_Z] = ConvertToShort(in_trans.position.z);

            // send euler angles 
            ref_dict[ROTATION_X] = ConvertToShort(in_trans.localEulerAngles.x);
            ref_dict[ROTATION_Y] = ConvertToShort(in_trans.localEulerAngles.y);
            ref_dict[ROTATION_Z] = ConvertToShort(in_trans.localEulerAngles.z);

            Rigidbody rigidBody = in_trans.GetComponent<Rigidbody>();

            if (rigidBody != null && !rigidBody.isKinematic)
            {
                ref_dict[VELOCITY_X] = ConvertToShort(rigidBody.linearVelocity.x);
                ref_dict[VELOCITY_Y] = ConvertToShort(rigidBody.linearVelocity.y);
                ref_dict[VELOCITY_Z] = ConvertToShort(rigidBody.linearVelocity.z);
            }
        }
        #endregion

        protected Vector3 m_networkPosition = Vector3.zero;
        protected Vector3 m_networkRotation = Vector3.zero;
        protected Vector3 m_networkVelocity = Vector3.zero;

        protected Vector3 m_rotation = Vector3.zero;
        protected Vector3 m_rotationVelocity = Vector3.zero;

        public float LastSyncedPing { get; private set; }
        public bool IsLocalPlayer { get { return _netId == GCore.Wrapper.RelayService.GetNetIdForProfileId(GCore.Wrapper.Client.AuthenticationService.ProfileId); } }

        public bool IsServerBot
        {
            get { return m_bServerBot; }
            protected set { m_bServerBot = value; }
        }
        private bool m_bServerBot = false;

        #region protected
        protected virtual void Start()
        {
            SEND_INTERVAL = SEND_INTERVAL < DEFAULT_INTERVAL ? DEFAULT_INTERVAL : SEND_INTERVAL; // Small bandaid; this occassionally gets set to -1
            if (_syncTransformInformation) InvokeRepeating("sendTransformInformation", SEND_INTERVAL, SEND_INTERVAL);
        }

        protected void syncTransformsWithNetworkData()
        {
            // Continue to guess predicted network position/rotation based on the velocity of each
            {
                m_networkPosition += m_networkVelocity * Time.deltaTime;
                m_networkRotation += m_rotationVelocity * Time.deltaTime;
            }

            // Velocity
            {
                Rigidbody rigidBody = GetComponent<Rigidbody>();
                rigidBody.linearVelocity = m_networkVelocity; // Don't lerp velocity, we don't have to go that deep
            }

            // Position
            {
                transform.position = Vector3.Lerp(transform.position, m_networkPosition, (1.0f / SEND_INTERVAL) * Time.deltaTime);
            }

            // Rotation
            {
                // Here we have to cache the rotation animation locally, and asign it every frame to the transform.
                // Otherwise it seems to jump back every time (Possibly bound to a rigid body?)
                m_rotation.z = Mathf.Lerp(m_rotation.z, m_networkRotation.z, (1.0f / SEND_INTERVAL) * Time.deltaTime);
                transform.rotation = Quaternion.Euler(m_rotation);
            }
        }

        public bool IsServer { get { return (GCore.Wrapper.Client.ProfileId == BombersNetworkManager.LobbyInfo.OwnerProfileId); } }

        protected bool _hasAuthority = false;
        protected bool _syncTransformInformation = false;
        public short _netId { get; protected set; }
        protected string _classType = "";
        protected string _fileName = "";
        #endregion

        #region private
        private void sendTransformInformation()
        {
            if (_hasAuthority)
            {
                Dictionary<string, object> json = new Dictionary<string, object>();
                json[OPERATION] = TRANSFORM_UPDATE;
                json[NET_ID] = _netId;
                json[LAST_PING] = ConvertToShort(GCore.Wrapper.Client.RelayService.LastPing * 0.0001f);
                UpdateWithTransformData(ref json, transform);

                cachePosInfo();

                SendStringOrByte(json, '=', ';', false, true);
            }
        }

        private static void SendStringOrByte(Dictionary<string, object> in_dict, char in_joinChar = '=', char in_splitChar = ';', bool in_reliable = true, bool in_ordered = true, int in_channel = 0)
        {
            if (MSG_ENCODED == 2)
            {
                GCore.Wrapper.Client.RelayService.Send(SerializeDict(in_dict), BrainCloud.BrainCloudRelay.TO_ALL_PLAYERS, in_reliable, in_ordered, in_channel);
            }
            else
            {
                SendStringData(SerializeDict(in_dict, in_joinChar, in_splitChar), BrainCloud.BrainCloudRelay.TO_ALL_PLAYERS, in_reliable, in_ordered, in_channel);
            }
        }

        public static string SerializeDict(Dictionary<string, object> in_dict, char in_joinChar = '=', char in_splitChar = ';')
        {
            string toString = "";

            if (MSG_ENCODED == 0)
            {
                toString = JsonWriter.Serialize(in_dict);
            }
            else
            {
                foreach (string key in in_dict.Keys)
                {
                    if (in_dict[key] != null)
                        toString += key + in_joinChar + in_dict[key] + in_splitChar;
                }
            }

            return toString;
        }

        public static byte[] EMPTY_ARRAY = new byte[0];
        public static byte[] SerializeDict(Dictionary<string, object> in_dict)
        {
            if (MSG_ENCODED == 2)
            {
                try
                {
                    // transform updates dont have data
                    sTransformUpdate entity = new sTransformUpdate(in_dict);
                    byte[] toReturn = HudHelper.StructureToByteArray<sTransformUpdate>(entity);
                    return toReturn;
                }
                catch (Exception)
                {
                    try
                    {
                        // transform updates dont have data
                        sProjectileData entity = new sProjectileData(in_dict);
                        byte[] toReturn = HudHelper.StructureToByteArray<sProjectileData>(entity);

                        return toReturn;
                    }
                    catch (Exception)
                    {
                        return EMPTY_ARRAY;
                    }
                }
            }
            return EMPTY_ARRAY;
        }

        public static Dictionary<string, object> DeserializeString(string in_string, char in_joinChar = '=', char in_splitChar = ';')
        {
            Dictionary<string, object> toDict = new Dictionary<string, object>();
            if (in_string == "" || in_string == null) return toDict;

            // regular json 
            if (MSG_ENCODED == 0)
            {
                try
                {
                    toDict = (Dictionary<string, object>)JsonReader.Deserialize(in_string);
                }
                catch (Exception)
                {
                    Debug.LogWarning("COULD NOT SERIALIZE " + in_string);
                }
            }
            // key value pairs
            else if (MSG_ENCODED == 1 || MSG_ENCODED == 2)
            {
                string[] splitItems = in_string.Split(in_splitChar);
                int indexOf = -1;
                foreach (string item in splitItems)
                {
                    indexOf = item.IndexOf(in_joinChar);
                    if (indexOf >= 0)
                    {
                        toDict[item.Substring(0, indexOf)] = item.Substring(indexOf + 1);
                    }
                }
            }
            return toDict;
        }

        public static Dictionary<string, object> DeserializeData(byte[] in_data)
        {
            if (MSG_ENCODED == 2)
            {
                Dictionary<string, object> newDict = new Dictionary<string, object>();

                try
                {
                    sTransformUpdate newObject = HudHelper.ByteArrayToStructure<sTransformUpdate>(in_data);
                    newDict = newObject.ToDict();
                }
                catch (Exception)
                {
                    try
                    {
                        sProjectileData newObject = HudHelper.ByteArrayToStructure<sProjectileData>(in_data);
                        newDict = newObject.ToDict();
                    }
                    catch (Exception)
                    {
                        string result = Encoding.ASCII.GetString(in_data);
                        if (result.Length > 0)
                        {
                            if (result[0] == '{')
                            {
                                newDict = (Dictionary<string, object>)JsonReader.Deserialize(result);
                            }
                            else
                            {
                                newDict = DeserializeString(result);
                            }
                        }
                    }
                }

                return newDict;
            }
            return null;
        }

        // offsets float by 10, for better short precision
        public static short ConvertToShort(float in_float)
        {
            try
            {
                short sToReturn = Convert.ToInt16(in_float * 10.0f);
                return sToReturn;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        // offsets float by 10, for better short precision
        public static float ConvertToFloat(Dictionary<string, object> in_dict, string in_key)
        {
            float sToReturn = GConfigManager.ReadFloatSafely(in_dict, in_key) / 10.0f;
            return sToReturn;
        }

        private void cachePosInfo()
        {
            m_posX = transform.position.x;
            m_posY = transform.position.y;
            m_posZ = transform.position.z;
            m_rotX = transform.rotation.x;
            m_rotY = transform.rotation.y;
            m_rotZ = transform.rotation.z;
        }

        private float m_posX = 0.0f;
        private float m_posY = 0.0f;
        private float m_posZ = 0.0f;

        private float m_rotX = 0.0f;
        private float m_rotY = 0.0f;
        private float m_rotZ = 0.0f;
        #endregion

        #region public consts

        public const string OPERATION = "op";
        public const string TYPE = "t";
        public const string NET_ID = "id";
        public const string DATA = "do";

        public const string TRANSFORM_UPDATE = "tx";
        public const string LAST_PING = "lp";
        public const string ENTITY_START = "es";
        public const string ENTITY_DESTROY = "ed";

        public const string POSITION_X = "pX";
        public const string POSITION_Y = "pY";
        public const string POSITION_Z = "pZ";

        public const string ROTATION_X = "rX";
        public const string ROTATION_Y = "rY";
        public const string ROTATION_Z = "rZ";

        public const string VELOCITY_X = "vX";
        public const string VELOCITY_Y = "vY";
        public const string VELOCITY_Z = "vZ";

        public const string PARENT_VELOCITY_X = "pvX";
        public const string PARENT_VELOCITY_Y = "pvY";
        public const string PARENT_VELOCITY_Z = "pvZ";

        // THESE MUST BE UNIQUE
        public static string DIRECTION_X = "dX";
        public static string DIRECTION_Y = "dY";
        public static string DIRECTION_Z = "dZ";

        public static string SPEED_X = "sX";
        public static string SPEED_Y = "sY";
        public static string SPEED_Z = "sZ";

        public static string SHOOTER_ID = "sd";
        public static string HIT_ID = "hd";
        public static string ID = "id";
        #endregion

    }
    [StructLayout(LayoutKind.Sequential)]
    struct sTransformUpdate
    {
        public void Reset()
        {
            LastPing = 0;
            PositionX = 0;
            PositionY = 0;
            RotationZ = 0;

            VelocityX = 0;
            VelocityY = 0;
            Id = -1;

            Operation = "na";
        }

        public sTransformUpdate(Dictionary<string, object> in_dict)
        {
            // this is a unique string from other entities
            if (in_dict.ContainsKey(BaseNetworkBehavior.SHOOTER_ID))
                throw new InvalidOperationException("INCORRECT PARSING");

            LastPing = 0;
            PositionX = 0;
            PositionY = 0;
            if (in_dict.ContainsKey(BaseNetworkBehavior.LAST_PING)) LastPing = Convert.ToInt16(in_dict[BaseNetworkBehavior.LAST_PING]);
            if (in_dict.ContainsKey(BaseNetworkBehavior.POSITION_X)) PositionX = Convert.ToInt16(in_dict[BaseNetworkBehavior.POSITION_X]);
            if (in_dict.ContainsKey(BaseNetworkBehavior.POSITION_Y)) PositionY = Convert.ToInt16(in_dict[BaseNetworkBehavior.POSITION_Y]);

            RotationZ = 0;
            if (in_dict.ContainsKey(BaseNetworkBehavior.ROTATION_Z)) RotationZ = Convert.ToInt16(in_dict[BaseNetworkBehavior.ROTATION_Z]);

            VelocityX = 0;
            VelocityY = 0;
            if (in_dict.ContainsKey(BaseNetworkBehavior.VELOCITY_X)) VelocityX = Convert.ToInt16(in_dict[BaseNetworkBehavior.VELOCITY_X]);
            if (in_dict.ContainsKey(BaseNetworkBehavior.VELOCITY_Y)) VelocityY = Convert.ToInt16(in_dict[BaseNetworkBehavior.VELOCITY_Y]);

            // required!
            Id = Convert.ToInt16(in_dict[BaseNetworkBehavior.NET_ID]);
            Operation = (string)in_dict[BaseNetworkBehavior.OPERATION];
        }

        public short LastPing;
        public short PositionX;
        public short PositionY;
        public short RotationZ;

        public short VelocityX;
        public short VelocityY;
        public short Id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
        public string Operation;

        public Dictionary<string, object> ToDict()
        {
            Dictionary<string, object> newDict = new Dictionary<string, object>();

            newDict[BaseNetworkBehavior.LAST_PING] = LastPing;
            newDict[BaseNetworkBehavior.POSITION_X] = PositionX;
            newDict[BaseNetworkBehavior.POSITION_Y] = PositionY;
            newDict[BaseNetworkBehavior.ROTATION_Z] = RotationZ;

            newDict[BaseNetworkBehavior.VELOCITY_X] = VelocityX;
            newDict[BaseNetworkBehavior.VELOCITY_Y] = VelocityY;

            newDict[BaseNetworkBehavior.NET_ID] = Id;
            newDict[BaseNetworkBehavior.OPERATION] = Operation;

            return newDict;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct sProjectileData
    {
        public short ShooterId;
        public short PositionX;
        public short PositionY;
        public short PositionZ;

        public short RotationX;
        public short RotationY;
        public short VelocityX;
        public short VelocityY;

        public short LastPing;
        public short HitId;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
        public string Type;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 4)]
        public string Operation;

        public int Id; // large random numbers of int.max

        public sProjectileData(Dictionary<string, object> in_dict)
        {
            if (!in_dict.ContainsKey(BaseNetworkBehavior.SHOOTER_ID))
                throw new InvalidOperationException("INCORRECT PARSING");

            LastPing = 0;
            PositionX = 0;
            PositionY = 0;
            PositionZ = 0;
            if (in_dict.ContainsKey(BaseNetworkBehavior.LAST_PING)) LastPing = Convert.ToInt16(in_dict[BaseNetworkBehavior.LAST_PING]);
            if (in_dict.ContainsKey(BaseNetworkBehavior.POSITION_X)) PositionX = Convert.ToInt16(in_dict[BaseNetworkBehavior.POSITION_X]);
            if (in_dict.ContainsKey(BaseNetworkBehavior.POSITION_Y)) PositionY = Convert.ToInt16(in_dict[BaseNetworkBehavior.POSITION_Y]);
            if (in_dict.ContainsKey(BaseNetworkBehavior.POSITION_Z)) PositionZ = Convert.ToInt16(in_dict[BaseNetworkBehavior.POSITION_Z]);

            RotationX = 0;
            RotationY = 0;
            if (in_dict.ContainsKey(BaseNetworkBehavior.DIRECTION_X)) RotationX = Convert.ToInt16(in_dict[BaseNetworkBehavior.DIRECTION_X]);
            if (in_dict.ContainsKey(BaseNetworkBehavior.DIRECTION_Y)) RotationY = Convert.ToInt16(in_dict[BaseNetworkBehavior.DIRECTION_Y]);

            VelocityX = 0;
            VelocityY = 0;
            if (in_dict.ContainsKey(BaseNetworkBehavior.SPEED_X)) VelocityX = Convert.ToInt16(in_dict[BaseNetworkBehavior.SPEED_X]);
            if (in_dict.ContainsKey(BaseNetworkBehavior.SPEED_Y)) VelocityY = Convert.ToInt16(in_dict[BaseNetworkBehavior.SPEED_Y]);

            HitId = -1;
            if (in_dict.ContainsKey(BaseNetworkBehavior.HIT_ID)) HitId = Convert.ToInt16(in_dict[BaseNetworkBehavior.HIT_ID]);

            // required!
            Id = (int)(in_dict[BaseNetworkBehavior.ID]);
            ShooterId = Convert.ToInt16(in_dict[BaseNetworkBehavior.SHOOTER_ID]);
            Type = (string)in_dict[BaseNetworkBehavior.TYPE];
            Operation = (string)in_dict[BaseNetworkBehavior.OPERATION];
        }

        public Dictionary<string, object> ToDict()
        {
            Dictionary<string, object> newDict = new Dictionary<string, object>();
            newDict[BaseNetworkBehavior.SHOOTER_ID] = ShooterId;
            newDict[BaseNetworkBehavior.HIT_ID] = HitId;
            newDict[BaseNetworkBehavior.ID] = Id;

            newDict[BaseNetworkBehavior.POSITION_X] = PositionX;
            newDict[BaseNetworkBehavior.POSITION_Y] = PositionY;
            newDict[BaseNetworkBehavior.POSITION_Z] = PositionZ;

            newDict[BaseNetworkBehavior.DIRECTION_X] = RotationX;
            newDict[BaseNetworkBehavior.DIRECTION_Y] = RotationY;

            newDict[BaseNetworkBehavior.SPEED_X] = VelocityX;
            newDict[BaseNetworkBehavior.SPEED_Y] = VelocityY;

            newDict[BaseNetworkBehavior.LAST_PING] = LastPing;
            newDict[BaseNetworkBehavior.TYPE] = Type;
            newDict[BaseNetworkBehavior.OPERATION] = Operation;

            return newDict;
        }
    }
}
