using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data
{
    /// <summary>
    /// 적 AI 설정 ScriptableObject
    /// 하드코딩된 매직 넘버들을 외부 설정 파일로 분리하여 
    /// 디자이너가 쉽게 조정할 수 있도록 함
    /// </summary>
    [CreateAssetMenu(fileName = "New Enemy Config", menuName = "Enemy/Enemy Config", order = 1)]
    public class EnemyConfig : ScriptableObject
    {
        
        [Header("=== 인지 설정 ===")]
        [Tooltip("시야 차단 장애물 레이어")]
        public LayerMask obstacleLayer = -1;
    
    [Header("=== 성능 설정 ===")]
    [Tooltip("AI 업데이트 간격 (초) - 성능 최적화용")]
    [Range(0.02f, 0.2f)]
    public float aiUpdateInterval = 0.1f;
    
    [Header("=== LOD 시스템 설정 ===")]
    [Tooltip("근거리 업데이트 간격 (초)")]
    [Range(0.02f, 0.1f)]
    public float closeRangeUpdateInterval = 0.03f;
    
    [Tooltip("중거리 업데이트 간격 (초)")]
    [Range(0.05f, 0.2f)]
    public float midRangeUpdateInterval = 0.2f;
    
    [Tooltip("원거리 업데이트 간격 (초)")]
    [Range(0.1f, 0.5f)]
    public float farRangeUpdateInterval = 0.5f;
    
    [Tooltip("근거리 기준 거리")]
    public float closeRangeThreshold = 15f;
    
    [Tooltip("중거리 기준 거리")]
    public float midRangeThreshold = 30f;
    
    [Tooltip("원거리 기준 거리")]
    public float farRangeThreshold = 50f;
    
    }
} 