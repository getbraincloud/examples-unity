using Fusion;

public class KartComponent : NetworkBehaviour {
    public KartEntity Kart { get; private set; }

    public virtual void Init(KartEntity kart) {
        Kart = kart;
    }
    
    /// <summary>
    /// Called on the tick that the race has started. This method is tick-aligned.
    /// </summary>
    public virtual void OnRaceStart() { }
    /// <summary>
    /// Called when this kart has crossed the finish line. This method is tick-aligned.
    /// </summary>
    public virtual void OnLapCompleted(int lap, bool isFinish) { }
    /// <summary>
    /// Called when an item has been picked up. This method is tick-aligned.
    /// </summary>
    public virtual void OnEquipItem(Powerup powerup, float timeUntilCanUse) { }
}