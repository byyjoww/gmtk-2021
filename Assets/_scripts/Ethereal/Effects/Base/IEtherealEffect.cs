using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IEtherealEffect
{
    bool IsAvailable { get; }

    void DeployStart();
    void DeployFinish();
    void RetrieveStart();
    void RetrieveFinish();

    void ForceRetrieve();

    void OnActivate();
    void OnDeactivate();

    void OnCollide(Collider2D _collider);
    void OnLinkCollideTick(Collider2D _collider);
}
