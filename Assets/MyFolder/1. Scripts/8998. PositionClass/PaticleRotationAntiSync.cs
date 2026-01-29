using System;
using UnityEngine;

namespace MyFolder._1._Scripts._8998._PositionClass
{
    public class PaticleRotationAntiSync : MonoBehaviour
    {
         ParticleSystem particle;
         [SerializeField] Transform target;
         [SerializeField] private float offset;
        ParticleSystem.MainModule main;
         private void Start()
         {
             particle = GetComponent<ParticleSystem>();
             main = particle.main;
         }

         private void LateUpdate()
         {
             // 1) 타깃의 Z(도)
             float zDeg = target.rotation.eulerAngles.z;

             // 2) 원하는 최종 각도(도) = offset(도) - 타깃 Z(도)
             //    DeltaAngle로 래핑(-180~180)하면 보간이나 로그가 더 보기 좋음
             float finalDeg = Mathf.DeltaAngle(0f, offset - zDeg);

             // 3) 라디안 변환
             float finalRad = finalDeg * Mathf.Deg2Rad;

             // 4) 적용
             var rot = main.startRotation;
             rot.mode = ParticleSystemCurveMode.Constant;
             rot.constant = finalRad;          // 라디안!!
             main.startRotation = rot;

         }
    }
}
