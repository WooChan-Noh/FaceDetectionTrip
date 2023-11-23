using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingAnimation : MonoBehaviour
{
    public Sprite[] images;
    public float secondsForFrame = 0.1f;
    private Image image;

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        StartCoroutine(AnimateSprite());//��ũ�� ����
    }

    IEnumerator AnimateSprite()
    {
        while (true)
        {
            for (int i = 0; i < images.Length; i++)
            {
                image.sprite = images[i];
                yield return new WaitForSeconds(secondsForFrame);
            }
            if(gameObject.activeSelf==false)
                break;
        }
    }

    private void OnDisable()
    {
        //StopCoroutine(AnimateSprite());��ũ�� ���� ���¶� �������� ����. �ڷ�ƾ ������ ����� �� 
        StopAllCoroutines();//����� ��ũ��Ʈ������
    }
}
