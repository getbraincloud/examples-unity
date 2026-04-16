using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NicknameUI : MonoBehaviour
{
    public WorldUINickname nicknamePrefab;

    private readonly Dictionary<KartEntity, WorldUINickname> _kartNicknames =
        new Dictionary<KartEntity, WorldUINickname>();

    private void Awake() {
        EnsureAllTexts();

        KartEntity.OnKartSpawned += SpawnNicknameText;
        KartEntity.OnKartDespawned += DespawnNicknameText;
    }

    private void OnDestroy() {
        KartEntity.OnKartSpawned -= SpawnNicknameText;
        KartEntity.OnKartDespawned -= DespawnNicknameText;
    }

    private void EnsureAllTexts() {
        // we need to make sure that any karts that spawned before the callback was subscribed, are registered
        var karts = KartEntity.Karts;
        foreach ( var kart in karts.Where(kart => !_kartNicknames.ContainsKey(kart)) ) {
            SpawnNicknameText(kart); 
        }
    }

    private void SpawnNicknameText(KartEntity kart) {
        // we dont want to see our own name tag - dont spawn
        if ( kart.Object.IsValid && kart.Object.HasInputAuthority )
            return;

        var obj = Instantiate(nicknamePrefab, this.transform);
        obj.SetKart(kart);

        _kartNicknames.Add(kart, obj);
    }

    private void DespawnNicknameText(KartEntity kart) {
        if ( !_kartNicknames.ContainsKey(kart) )
            return;

        var text = _kartNicknames[kart];
        Destroy(text.gameObject);

        _kartNicknames.Remove(kart);
    }
}
