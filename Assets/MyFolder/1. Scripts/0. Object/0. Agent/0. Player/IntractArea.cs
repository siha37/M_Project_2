using System;
using UnityEngine;
using System.Collections.Generic;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._13._Card;

public class IntractArea : MonoBehaviour
{
    // 상호작용 가능한 오브젝트들을 저장할 리스트
    private List<GameObject> interactableList = new();
    private PlayerContext context;

    private void Start()
    {
        context = transform.parent.GetComponent<PlayerContext>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (transform.parent == other.transform || !context.Sync.IsOwner)
            return;
        // Player나 Object 태그를 가진 오브젝트를 리스트에 추가
        if (other.CompareTag("Player"))
        {
            if (!interactableList.Contains(other.gameObject))
            {
                if (other.gameObject.TryGetComponent(out PlayerContext context) && context.Sync.IsDead())
                {
                    context.AgentUI.Interaction_OnOff(true);
                }
                interactableList.Add(other.gameObject);   
            }
        }
        else if(other.CompareTag("InteractableObj"))
        {
            if (!interactableList.Contains(other.gameObject))
            {
                if (other.gameObject.TryGetComponent(out RewardObject obj))
                {
                    obj.interaction_OnOff(true);
                    interactableList.Add(other.gameObject);   
                }
            }
        }
        else if (other.CompareTag("Object"))
        {
            if (!interactableList.Contains(other.gameObject))
            {
                interactableList.Add(other.gameObject);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (transform.parent == other.transform || !context.Sync.IsOwner)
            return;
        if ( other.CompareTag("Player"))
        {
            if (interactableList.Contains(other.gameObject))
            {
                if (other.gameObject.TryGetComponent(out PlayerContext context))
                {
                    if (context.Sync.IsDead())
                    {
                        context.AgentUI.Interaction_OnOff(true);   
                    }
                    else
                    {
                        context.AgentUI.Interaction_OnOff(false);
                    }
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (transform.parent == other.transform || !context.Sync.IsOwner)
            return;
        // Player나 Object 태그를 가진 오브젝트를 리스트에서 제거
        if ( other.CompareTag("Player"))
        {
            if (interactableList.Contains(other.gameObject))
            {
                if (other.gameObject.TryGetComponent(out PlayerContext context) && context.Sync.IsDead())
                {
                    context.AgentUI.Interaction_OnOff(false);
                }
                interactableList.Remove(other.gameObject);
            }
        }
        else if (other.CompareTag("InteractableObj"))
        {
            if (interactableList.Contains(other.gameObject))
            {
                if (other.gameObject.TryGetComponent(out RewardObject obj))
                {
                    obj.interaction_OnOff(false);
                    interactableList.Remove(other.gameObject);
                }
            }
        }
        else if (other.CompareTag("Object"))
        {
            if (interactableList.Contains(other.gameObject))
            {
                interactableList.Remove(other.gameObject);
            }
        }
    }

    // 상호작용 가능한 오브젝트 리스트 반환
    public List<GameObject> GetInteractableList()
    {
        return interactableList;
    }

    // 가장 가까운 오브젝트 반환
    public GameObject GetNearestObject()
    {
        if (interactableList.Count == 0) return null;

        GameObject nearest = interactableList[0];
        float nearestDistance = Vector2.Distance(transform.position, nearest.transform.position);

        for (int i = 1; i < interactableList.Count; i++)
        {
            float distance = Vector2.Distance(transform.position, interactableList[i].transform.position);
            if (distance < nearestDistance)
            {
                nearest = interactableList[i];
                nearestDistance = distance;
            }
        }

        return nearest;
    }
}
