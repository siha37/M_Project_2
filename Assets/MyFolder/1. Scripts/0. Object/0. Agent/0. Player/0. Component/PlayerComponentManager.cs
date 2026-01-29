using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component
{
    public class PlayerComponentManager : MonoBehaviour
    {
        private PlayerContext context;
        
        private Dictionary<Type,IPlayerComponent> components;
        private Dictionary<Type,IPlayerUpdateComponent> update_components;
        private void Start()
        {
            Init();
            ComponentInit();
        }

        private void Init()
        {
            TryGetComponent(out context);
        }
        private void ComponentInit()
        {
            components = new Dictionary<Type,IPlayerComponent>();
            update_components = new Dictionary<Type,IPlayerUpdateComponent>();
            components.Clear();
            update_components.Clear();

            AddComponent<PlayerHealComponent>();
            AddComponent<PlayerCamouflageComponent>();
            AddComponent<PlayerDefenceComponent>();
            if(context.Skeleton)
                AddComponent<PlayerSkeletonAnimationComponent>();
        }

        private void AddComponent<T>() where T : IPlayerComponent,new()
        {
            //인스턴스 생성
            IPlayerComponent component = new T();
            components.Add(typeof(T),component);
            //컴포넌트 초기화
            component.Start(context);
            //키 이벤트 등록
            component.SetKeyEvent(context.Input);
            
            //업데이트 컴포넌트 등록
            if(component is IPlayerUpdateComponent up_com)
            {
                update_components.Add(typeof(T),up_com);
            }
        }

        public IPlayerComponent GetPComponent<T>() where T : IPlayerComponent
        {
            if(components != null && components.ContainsKey(typeof(T)))
            {
                components.TryGetValue(typeof(T), out IPlayerComponent component);
                return component;
            }
            return null;                        
        }
        
        
        private void Update()
        {
            foreach (var component in update_components)
            {
                component.Value.Update();
            }
        }
        private void FixedUpdate()
        {
            foreach (var component in update_components)
            {
                component.Value.FixedUpdate();
            }
        }
        private void LateUpdate()
        {
            foreach (var component in update_components)
            {
                component.Value.LateUpdate();
            }
        }

        private void OnDestroy()
        {
            foreach (var component in components)
            {
                component.Value.Stop();
            }
        }
    }
}