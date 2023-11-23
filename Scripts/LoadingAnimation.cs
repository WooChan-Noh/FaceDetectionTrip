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
        StartCoroutine(AnimateSprite());//링크가 끊김
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
        //StopCoroutine(AnimateSprite());링크가 끊긴 상태라 동작하지 않음. 코루틴 변수를 사용할 것 
        StopAllCoroutines();//연결된 스크립트에서만
    }
}
