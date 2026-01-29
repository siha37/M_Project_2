using System.Net.Mime;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using UnityEngine;

namespace MyFolder._1._Scripts._2._View._0._FogofWar
{
    public class PlayerVisionObject : VisionObject
    {
        [SerializeField] private PlayerContext context;
        /// <summary>
        /// 시각화를 활성화
        /// </summary>
        public override void VisionOn()
        {
            if(context.Sprite)context.Sprite.enabled = true;
            if(context.SkeletonMesh)context.SkeletonMesh.enabled = true;
            if(context.ShieldSprite)context.ShieldSprite.enabled = true;
            if(context.ShieldEffect)context.ShieldEffect.SetActive(true);
            if(context.uiCanvas) context.uiCanvas.enabled = true;
            
        }

        
        /// <summary>
        /// 시각화를 비활성화
        /// </summary>
        public override void VisionOff()
        {
            if(context.Sprite)context.Sprite.enabled = false;
            if(context.SkeletonMesh)context.SkeletonMesh.enabled = false;
            if(context.ShieldSprite)context.ShieldSprite.enabled = false;
            if(context.ShieldEffect)context.ShieldEffect.SetActive(false);
            if(context.uiCanvas) context.uiCanvas.enabled = false;
        }
    }
}