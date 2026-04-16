using Fusion;
using UnityEngine;

[CreateAssetMenu(fileName = "New Powerup", menuName = "Scriptable Object/Powerup")]
public class Powerup : ScriptableObject
{
	public string itemName;
	public Sprite itemIcon;
	public SpawnedPowerup prefab;
    
    public void Use(NetworkRunner runner, KartEntity user) {
        if ( prefab != null ) {
            var position = user.itemDropNode.position;
            var rotation = user.itemDropNode.rotation;

            if (runner.CanSpawn)
            {
                var obj = runner.Spawn(prefab, position, rotation, null, null);
                obj.Init(user);
            }
        }
    }
}