using Configs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Gameplay
{
    public interface ITarget
    {
        Transform Transform { get; }
        bool IsAlive { get; }
        int Team { get; set; }
    }

    public interface IWeapon : ITarget
    {
        WeaponConfig Weapon { get; }
        Transform ShootPoint { get; }
        float Cooldown { get; set; }
        ITarget Target { get; set; }
    }

    public interface IMovable
    {
        Transform Transform { get; }
        CharacterConfig Config { get; }
        float RotationSpeed { get; }
        void MovePosition(Vector3 delta);
        void SetRotation(Quaternion rotation);
    }
}
