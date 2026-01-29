using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace MyFolder._1._Scripts._7._PlayerRole
{
    [Serializable]
    public class PlayerRoleDefinition
    {
        public string Role;
        
        // 능력 게이트
        public bool CanUseSkill1 = false;
        
        public PlayerRoleType GetRole => (PlayerRoleType)Enum.Parse(typeof(PlayerRoleType), Role);
    }
}