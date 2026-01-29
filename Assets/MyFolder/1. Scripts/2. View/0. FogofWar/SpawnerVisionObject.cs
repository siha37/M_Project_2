using UnityEngine;

namespace MyFolder._1._Scripts._2._View._0._FogofWar
{
    public class SpawnerVisionObject : VisionObject
    {
        [SerializeField] SpriteRenderer sprite;
        [SerializeField] Canvas uiCanvas;
        
        public override void VisionOn()
        {
            if(sprite)sprite.enabled = true;
            if(uiCanvas) uiCanvas.enabled = true;
        }

        public override void VisionOff()
        {
            if(sprite)sprite.enabled = true;
            if(uiCanvas) uiCanvas.enabled = true;
        }
    }
}