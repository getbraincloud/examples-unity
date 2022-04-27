using UnityEngine;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.Entity;

public class ScreenEntity : BCScreen
{
    private static string ENTITY_TYPE_PLAYER = "player";
    //private BCUserEntity m_player;
    
    public ScreenEntity(BrainCloudWrapper bc) : base(bc) { }

    public override void Activate()
    {
        //_bc.PlayerStateService.ReadUserState(ReadPlayerStateSuccess, Failure_Callback);
        m_mainScene.EntityInterface.ReadEntity();
        m_mainScene.AddLogNoLn("[ReadPlayerState]... ");

    }

    public override void OnScreenGUI()
    {
        EntityInstance m_player = null;
        if (m_mainScene.EntityInterface.PlayerAssigned)
        {
            m_player = m_mainScene.EntityInterface.Player;    
        }
        
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
            m_player.Name = GUILayout.TextField(m_player.Name);
        } 
        else
        {
            GUILayout.Box("---");
        }
        GUILayout.EndHorizontal();
        
        // entity property 'age'
        GUILayout.BeginHorizontal();
        GUILayout.Label("Age", GUILayout.Width(minLabelWidth));
        if (m_player != null)
        {
            string ageStr = GUILayout.TextField(m_player.Age);
            int ageInt = 0;
            if (int.TryParse(ageStr, out ageInt))
            {
                m_player.Age = ageInt.ToString();
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
                m_mainScene.EntityInterface.CreateEntity();
                m_mainScene.RealLogging("Creating Entity....");
            }
        }
        if (m_player != null)
        {
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save Entity"))
            {
                m_mainScene.EntityInterface.UpdateEntity();
                m_mainScene.RealLogging("Updating Entity...");
            }
            if (GUILayout.Button("Delete Entity"))
            {
                m_mainScene.EntityInterface.DeleteEntity();
                m_player = null;
                m_mainScene.RealLogging("Deleting Entity...");
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.EndVertical();
    }
}
