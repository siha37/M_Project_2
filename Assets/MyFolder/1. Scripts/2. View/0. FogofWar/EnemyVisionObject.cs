using Spine.Unity;
using UnityEngine;
using UnityEngine.Serialization;

namespace MyFolder._1._Scripts._2._View._0._FogofWar
{
    public class EnemyVisionObject : VisionObject
    {
        [SerializeField] private MeshRenderer skeletonMesh;
        [SerializeField] private SpriteRenderer sprite;
        [SerializeField] private Canvas uiCanvas;
        
        public override void VisionOn()
        {
            if(sprite)sprite.enabled = true;
            if(skeletonMesh)skeletonMesh.enabled = true;
            if(uiCanvas) uiCanvas.enabled = true;
        }

        public override void VisionOff()
        {
            if(sprite)sprite.enabled = false;
            if(skeletonMesh)skeletonMesh.enabled = false;
            if(uiCanvas) uiCanvas.enabled = false;
        }
    }
}