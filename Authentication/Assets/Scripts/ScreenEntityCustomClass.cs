using UnityEngine;
using System.Collections.Generic;
using BrainCloud;
using BrainCloud.Common;
using BrainCloud.Entity;

public class ScreenEntityCustomClass : BCScreen
{
    public class Hobby
    {
        public string Name
        {
            get { return ""; }
        }
    }

    public class Player : BCUserEntity
    {
        public static string ENTITY_TYPE = "player";

        public Player(BrainCloudEntity in_bcEntityService) : base(in_bcEntityService)
        {
            // set up some defaults
            m_entityType = "player";
            Name = "";
            Age = 0;
            Hobbies = new List<Hobby>();
        }

        public string Name
        {
            get { return (string) this ["name"]; }
            set { this ["name"] = value; }
        }

        public int Age
        {
            get { return (int) this ["age"]; }
            set { this ["age"] = value; }
        }

        public IList<Hobby> Hobbies
        {
            get { return this.Get<IList<Hobby>>("hobbies"); }
            set { this["hobbies"] = value; }
        }
    }


    //private Player m_player;
    
    public ScreenEntityCustomClass(BrainCloudWrapper bc) : base(bc) { }

    public override void Activate()
    {
        //_bc.EntityFactory.RegisterEntityClass<Player>(Player.ENTITY_TYPE);

        //_bc.PlayerStateService.ReadUserState(ReadPlayerStateSuccess, Failure_Callback);
        m_mainScene.CustomEntityInterface.ReadCustomEntity();
        m_mainScene.RealLogging("[ReadPlayerState]... ");
    }

    /*
    private void ReadPlayerStateSuccess(string json, object cb)
    {
        m_mainScene.AddLog("SUCCESS");
        m_mainScene.AddLogJson(json);
        m_mainScene.AddLog("");

        // look for the player entity
        IList<BCUserEntity> entities = _bc.EntityFactory.NewUserEntitiesFromReadPlayerState(json);
        foreach (BCUserEntity e in entities)
        {
            if (e.EntityType == Player.ENTITY_TYPE)
            {
                m_player = (Player)e;
            }
        }
    }
    */

    public override void OnScreenGUI()
    {
        CustomEntityInstance m_player = null;
        if (m_mainScene.CustomEntityInterface.PlayerAssigned)
        {
            m_player = m_mainScene.CustomEntityInterface.CustomPlayer;    
        }
        
        GUILayout.BeginVertical();

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
            m_player.FirstName = GUILayout.TextField(m_player.FirstName);
        } 
        else
        {
            GUILayout.Box("---");
        }
        GUILayout.EndHorizontal();

        // entity property 'age'
        GUILayout.BeginHorizontal();
        GUILayout.Label("Position", GUILayout.Width(minLabelWidth));
        if (m_player != null)
        {
            m_player.Position = GUILayout.TextField(m_player.Position);
        } 
        else
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
                m_mainScene.CustomEntityInterface.CreateCustomEntity();
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
