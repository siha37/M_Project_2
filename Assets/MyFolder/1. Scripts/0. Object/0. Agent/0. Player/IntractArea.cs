using UnityEngine;
using System.Collections.Generic;

public class IntractArea : MonoBehaviour
{
    // 상호작용 가능한 오브젝트들을 저장할 리스트
    private List<GameObject> interactableList = new List<GameObject>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (transform.parent == other.transform)
            return;
        // Player나 Object 태그를 가진 오브젝트를 리스트에 추가
        if (other.CompareTag("Player") || other.CompareTag("Object"))
        {
            if (!interactableList.Contains(other.gameObject))
            {
                interactableList.Add(other.gameObject);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (transform.parent == other.transform)
            return;
        // Player나 Object 태그를 가진 오브젝트를 리스트에서 제거
        if ( other.CompareTag("Player") || other.CompareTag("Object"))
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
