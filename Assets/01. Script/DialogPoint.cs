using UnityEngine;
using UnityEngine.UI;
namespace LLMUnity
{
    public class DialogPoint : MonoBehaviour
    {
        [Range(-100f, 100f)]
        public float point = 0f;
        [SerializeField] Text pointText;

        public void UpPoint()
        {
            point+= 20;
            pointText.text = "현재 점수 : " + point.ToString();
        }

        public void DownPoint()
        {
            point-= 20;
            pointText.text = "현재 점수 : " + point.ToString();
        }
    }
}

