using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using BrainCloudPhotonExample.Game;

namespace BrainCloudPhotonExample.Connection
{
    public class MapPresets : MonoBehaviour
    {
        public class Preset
        {
            public string m_name = "";
            public int m_numShips = 0;
            public List<ShipInfo> m_ships;

            public Preset(string aName, int aNumShips)
            {
                m_name = aName;
                m_numShips = aNumShips;
                m_ships = new List<ShipInfo>();
            }
        }

        public class ShipInfo
        {
            public int m_team = 0;
            public ShipController.eShipType m_shipType = ShipController.eShipType.SHIP_TYPE_NONE;
            public float m_xPositionPercent = 0;
            public float m_yPositionPercent = 0;
            public float m_angle = 0;
            public float m_respawnTime = -1;
            public Vector3[] m_path = new Vector3[4];
            public float m_pathSpeed = 0;

            public ShipInfo(int aTeam, int aShipType, float aXPos, float aYPos, float aAngle, float aRespawnTime, Vector3[] aPath, float aPathSpeed)
            {
                m_team = aTeam;
                m_shipType = (ShipController.eShipType)aShipType;
                m_xPositionPercent = aXPos;
                m_yPositionPercent = aYPos;
                m_angle = aAngle;
                m_respawnTime = aRespawnTime;
                m_path = aPath;
                m_pathSpeed = aPathSpeed;
            }
        }

        public class MapSize
        {
            public string m_name = "";
            public float m_horizontalSize = 0;
            public float m_verticalSize = 0;

            public MapSize(string aName, float aHSize, float aVSize)
            {
                m_name = aName;
                m_horizontalSize = aHSize;
                m_verticalSize = aVSize;
            }
        }

        public List<Preset> m_presets;
        public List<MapSize> m_mapSizes;

        void Start()
        {
            DontDestroyOnLoad(gameObject);
            m_presets = new List<Preset>();
            m_mapSizes = new List<MapSize>();
            string text = ((TextAsset)Resources.Load("MapLayouts")).text;
            JsonData jsonData = JsonMapper.ToObject(text);
            int numPresets = jsonData.Count;

            for (int i = 0; i < numPresets; i++)
            {
                JsonData preset = jsonData[i.ToString()];
                string name = preset["name"].ToString();
                int numShips = int.Parse(preset["numShips"].ToString());
                Preset newPreset = new Preset(name, numShips);

                for (int j = 0; j < numShips; j++)
                {
                    JsonData ship = preset["ship" + (j + 1)];
                    int team = int.Parse(ship["team"].ToString());
                    int shipType = 0;
                    string shipTypeString = ship["shipType"].ToString();
                    switch (shipTypeString)
                    {
                        case "Carrier":
                            shipType = (int)ShipController.eShipType.SHIP_TYPE_CARRIER;
                            break;
                        case "Battleship":
                            shipType = (int)ShipController.eShipType.SHIP_TYPE_BATTLESHIP;
                            break;
                        case "Submarine":
                            shipType = (int)ShipController.eShipType.SHIP_TYPE_SUBMARINE;
                            break;
                        case "Cruiser":
                            shipType = (int)ShipController.eShipType.SHIP_TYPE_CRUISER;
                            break;
                        case "Destroyer":
                            shipType = (int)ShipController.eShipType.SHIP_TYPE_DESTROYER;
                            break;
                        case "Patrol Boat":
                            shipType = (int)ShipController.eShipType.SHIP_TYPE_PATROL_BOAT;
                            break;
                    }
                    float xPos = float.Parse(ship["xPos"].ToString());
                    float yPos = float.Parse(ship["yPos"].ToString());
                    float angle = float.Parse(ship["angle"].ToString());
                    float respawnTime = float.Parse(ship["respawn"].ToString());
                    Vector3[] path = new Vector3[4];
                    for (int k = 0; k < 4; k++)
                    {
                        JsonData point = ship["path"]["p" + (k + 1)];
                        float x = float.Parse(point["x"].ToString());
                        float y = float.Parse(point["y"].ToString());
                        path[k] = new Vector3(x, y, 0);
                    }
                    float pathSpeed = float.Parse(ship["pathSpeed"].ToString());

                    ShipInfo shipInfo = new ShipInfo(team, shipType, xPos, yPos, angle, respawnTime, path, pathSpeed);
                    newPreset.m_ships.Add(shipInfo);
                }

                m_presets.Add(newPreset);
            }

            text = ((TextAsset)Resources.Load("MapSizes")).text;
            jsonData = JsonMapper.ToObject(text);
            numPresets = jsonData.Count;

            for (int i = 0; i < numPresets; i++)
            {
                JsonData preset = jsonData[i.ToString()];
                string name = preset["name"].ToString();
                float horz = float.Parse(preset["horizontalSize"].ToString());
                float vert = float.Parse(preset["verticalSize"].ToString());
                MapSize mapSize = new MapSize(name, horz, vert);
                m_mapSizes.Add(mapSize);
            }
        }
    }
}