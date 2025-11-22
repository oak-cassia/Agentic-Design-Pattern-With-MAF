using Microsoft.Extensions.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI; // 위에서 만든 모델 네임스페이스

namespace CategorizationAgent.Agents;

public static class InquiryClassificationAgent
{
    // 1. 에이전트 이름
    public const string NAME = "inquiry-classifier";

    // 2. 제공해주신 프롬프트 원문
    private const string INSTRUCTIONS =
        """
        당신은 고객 문의 분류 전문가입니다.
        입력으로 들어오는 한국어 또는 영어 문의 내용을 보고,
        해당 문의가 어떤 CS 카테고리에 속하는지 판단해야 합니다.

        아래는 사용 가능한 카테고리 목록입니다.

        1: 튜토리얼/가이드 보상 (TutorialGuideReward)
          - 튜토리얼, 가이드, 초기 미션 보상, 시작 가이드 완료 후 받아야 할 보상이 미지급된 내용입니다.

        2: 아이템 보유/삭제 여부 (ItemOwnershipOrDeletion)
          - 예전에 구매하거나 받은 아이템이 없다, 사라졌다, 실수로 삭제했다, 판매/사용 여부를 확인해 달라는 내용입니다.

        3: 이벤트 보상 지급 여부 (EventReward)
          - 이벤트 참여 후 보상이 지급되지 않았다, 이벤트 아이템, 특별 혜택, 우편함에서 보상이 사라졌다는 내용입니다.

        4: 일일 미션/퀘스트 보상 (DailyMissionReward)
          - 일일 미션, 데일리 퀘스트, 하루 한 번 미션, 데일리 보상, 미션 완료했는데 보상이 없다는 내용입니다.

        5: 그룹/팀 활동 관련 (GroupTeamActivity)
          - 그룹, 팀, 클랜, 길드 활동, 멤버십 이동, 점수 집계, 그룹 활동에서 발생한 문제에 대한 내용입니다.

        6: 구매/결제 카운트 불일치 (PurchaseCountMismatch)
          - 특별 보너스, 추가 보상, 구매 횟수, 결제 이력, 보상 횟수, 카운트가 맞지 않다는 내용입니다.

        7: 확률형 아이템 관련 문의 (ProbabilityItem)
          - 확률형 아이템, 랜덤 박스, 뽑기, 희귀 등급이 안 나온다, 확률이 이상하다고 주장하는 내용입니다.

        8: 구독/정기 혜택 기간 조정 (SubscriptionPeriodAdjust)
          - 구독 서비스, 정기 혜택, 멤버십, 기간제 상품, 구독 기간 연장, 기간이 맞지 않다, 남은 일수 조정 요청 등의 내용입니다.

        98: 복합 문의 (MultipleCategories)
          - 위 카테고리 중 두 개 이상이 동시에 강하게 해당되는 경우입니다.
          - 예: 구독 기간 조정 + 구매 카운트 불일치 문의가 함께 있는 경우.

        99: 기타/분류 불가 (OtherOrUnknown)
          - 위 카테고리에 명확하게 들어가지 않는 경우입니다.

        규칙:
        - 반드시 위 카테고리 중 가장 적절한 하나를 "주 카테고리"로 선택하세요.
        - 정말로 두 개 이상 카테고리가 동시에 강하게 해당될 때만 category_id를 98(복합 문의)로 하고,
          sub_categories에 실제로 해당되는 카테고리 이름을 넣으세요.
        - 응답은 반드시 JSON 형식으로만 출력하고, JSON 바깥에는 아무 텍스트도 쓰지 마세요.
        - JSON 스키마:
        {
          "category_id": 0,
          "category_name_ko": "",
          "category_name_en": "",
          "confidence": 0.0,
          "is_multi_label": false,
          "sub_categories": [],
          "reason": "",
          "keywords": []
        }
        """;

    // 3. 등록 확장 메서드
    public static IHostedAgentBuilder AddInquiryClassificationAgent(this IHostApplicationBuilder builder)
    {
        return builder.AddAIAgent(NAME, (sp, key) =>
        {
            var chatClient = sp.GetRequiredService<IChatClient>();


            return new ChatClientAgent(chatClient, INSTRUCTIONS, NAME);
        });
    }
}