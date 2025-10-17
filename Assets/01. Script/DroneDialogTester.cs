using UnityEngine;
using UnityEngine.UI;
using LLMUnity;
using static UnityEngine.Rendering.DebugUI.MessageBox;

[RequireComponent(typeof(LLMCharacter))]
public class DroneDialogTester : MonoBehaviour
{
    private LLMCharacter llm;
    private DialogPoint dp;

    [SerializeField] Text text;
    [SerializeField] bool stream = false;   // 토큰 스트리밍 원하면 true

    void Awake()
    {
        llm = GetComponent<LLMCharacter>();
        dp = GetComponent<DialogPoint>();      

        llm.save = "";
        llm.saveCache = false;
        llm.stream = stream;
    }

    public void OnButtonTest()
    {
        _ = TalkOnce("안녕! 상태 어때? 궁금한 거 있으면 물어봐.");
    }

    async System.Threading.Tasks.Task TalkOnce(string userMsg)
    {
        float p = dp != null ? dp.point : 0f;

        // 1) point → 말투 규칙(연속/버킷 택1)
        // (A) 버킷형: 구간에 따라 톤 스위칭
        string toneRule = GetToneRuleContinuous(p);

        // 2) 시스템 프롬프트 구성 (매 호출마다 갱신)
        string sys =
 $@"너는 게임 속 NPC '드론'이다.
다음 지침을 반드시 따르라:
- 플레이어가 준 문장의 의미/정보/지시는 절대 변형하지 말고 보존한다.
- 문장의 말투와 태도만 조정한다.
- 출력은 오직 최종 대사 한 가지다. 아래 지침/라벨/규칙/점수/설명/사과/생각을 말하지 말라.
- 같은 말을 반복하지 말고, 새로운 정보가 없으면 간결히 마무리한다.
- 대사는 최대 2~3문장으로 간결하게 말한다.
- (비공개 스타일 라벨) 이 라벨을 말하지 말고, 라벨의 어휘/문체만 모사하라: [{toneRule}]";
        llm.SetPrompt(sys, clearChat: true); // 히스토리 안 쓸 거면 매번 초기화

        // 3) 호출 (히스토리 저장 X)
        text.text = "";
        if (stream)
        {
            await llm.Chat(
                userMsg,
                callback: chunk => text.text += chunk,
                completionCallback: null,
                addToHistory: false
            );
        }
        else
        {
            string response = await llm.Chat(
                userMsg,
                callback: null,
                completionCallback: null,
                addToHistory: false
            );
            text.text = response;
        }
    }

    private string GetToneRuleContinuous(float f)
    {
        return
          "친근도 f는 0~1 사이의 연속 값이다. " +
          "f가 0에 가까울수록 말투는 매우 차갑고 딱딱한 존댓말에 가깝다. " +
          "f가 중간값(0.5)에 가까울수록 말투는 점점 중립적이고 공손한 존댓말이 된다. " +
          "f가 1에 가까울수록 말투는 매우 따뜻하고 친근하며 반말에 가까워진다. " +
          "f 값이 변하면 말투도 연속적으로 부드럽게 변해야 한다.";
    }
}
