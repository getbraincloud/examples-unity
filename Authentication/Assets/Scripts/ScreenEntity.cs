using UnityEngine;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.Entity;

public class ScreenEntity : BCScreen
{
    private static string ENTITY_TYPE_PLAYER = "player";
    private BCUserEntity m_player;

    public override void Activate()
    {
        BrainCloudWrapper.GetBC().PlayerStateService.ReadUserState(ReadPlayerStateSuccess, Failure_Callback);
        m_mainScene.AddLogNoLn("[ReadPlayerState]... ");

    }

    private void ReadPlayerStateSuccess(string json, object cb)
    {
        m_mainScene.AddLog("SUCCESS");
        m_mainScene.AddLogJson(json);
        m_mainScene.AddLog("");
        
        // look for the player entity
        IList<BCUserEntity> entities = BrainCloudWrapper.GetBC().EntityFactory.NewUserEntitiesFromReadPlayerState(json);
        foreach (BCUserEntity entity in entities)
        {
            if (entity.EntityType == ENTITY_TYPE_PLAYER)
            {
                m_player = entity;
            }
        }
    }

    public override void OnScreenGUI()
    {
        GUILayout.BeginVertical();
        //GUILayout.Box("Player Entity");
        
        int minLabelWidth = 60;
        
        // entity id
        GUILayout.BeginHorizontal();
        GUILayout.Label("Id", GUILayout.Width(minLabelWidth));
        GUILayout.Box(m_player != null ? m_player.EntityId : "---");
        GUILayout.EndHorizontal();
        
        // entity type
        GUILayout.BeginHorizontal();
        GUILayout.Label("Type", GUILayout.Width(minLabelWidth));
        GUILayout.Box(m_player != null ? m_player.EntityType : "---");
        GUILayout.EndHorizontal();
        
        // entity property 'name'
        GUILayout.BeginHorizontal();
        GUILayout.Label("Name", GUILayout.Width(minLabelWidth));
        if (m_player != null)
        {
            m_player ["name"] = GUILayout.TextField((string)m_player ["name"]);
        } else
        {
            GUILayout.Box("---");
        }
        GUILayout.EndHorizontal();
        
        // entity property 'age'
        GUILayout.BeginHorizontal();
        GUILayout.Label("Age", GUILayout.Width(minLabelWidth));
        if (m_player != null)
        {
            string ageStr = GUILayout.TextField(((int)m_player ["age"]).ToString());
            int ageInt = 0;
            if (int.TryParse(ageStr, out ageInt))
            {
                m_player ["age"] = ageInt;
            }
        } else
        {
            GUILayout.Box("---");
        }
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        if (m_player == null)
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create Entity"))
            {
                m_player = BrainCloudWrapper.GetBC().EntityFactory.NewUserEntity(ENTITY_TYPE_PLAYER);
                m_player ["name"] = "Johnny Philharmonica";
                m_player ["age"] = 49;
            }
        }
        if (m_player != null)
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save Entity"))
            {
                m_mainScene.AddLogNoLn("[Entity.StoreAsync()]... ");
                m_player.StoreAsync(Success_Callback, Failure_Callback);
            }
            if (GUILayout.Button("Delete Entity"))
            {
                m_player.DeleteAsync(Success_Callback, Failure_Callback);
                m_player = null;
                m_mainScene.AddLogNoLn("[Entity.DeleteEntity]... ");
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
    }
}
