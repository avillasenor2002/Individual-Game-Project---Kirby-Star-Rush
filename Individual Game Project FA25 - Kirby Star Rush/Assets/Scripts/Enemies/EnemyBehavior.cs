using UnityEngine;

public abstract class EnemyBehavior : ScriptableObject
{
    // Called every FixedUpdate from the Enemy script
    public abstract void Execute(Enemy enemy);
}
