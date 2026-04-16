using Fusion;
using UnityEngine;

public class KartItemController : KartComponent {
    public float equipItemTimeout = 3f;
    public float useItemTimeout = 2.5f;

    [Networked]
    public TickTimer EquipCooldown { get; set; }
    
    public bool CanUseItem => Kart.HeldItemIndex != -1 && EquipCooldown.ExpiredOrNotRunning(Runner);

    public override void OnEquipItem(Powerup powerup, float timeUntilCanUse) {
        base.OnEquipItem(powerup, timeUntilCanUse);

        EquipCooldown = TickTimer.CreateFromSeconds(Runner, equipItemTimeout);
    }

    public void UseItem() {
        if ( !CanUseItem ) {
            // We dont want to play the horn on re-simulations.
            if ( !Runner.IsForward ) return;
            
            Kart.Audio.PlayHorn();
        } else {
            Kart.HeldItem.Use(Runner, Kart);
            Kart.HeldItemIndex = -1;
        }
    }
}