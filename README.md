# Agentic Workflow를 활용한 CS 자동화 시스템

> Microsoft Agent Framework와 LLM을 활용하여 고객 문의(CS)를 자동으로 분류하고 처리하는 시스템

## 📋 목차
- [문제 정의](#-문제-정의)
- [해결 방안](#-해결-방안)
- [기술 스택](#-기술-스택)
- [시스템 설계](#-시스템-설계)
- [구현 내용](#-구현-내용)
- [실행 결과](#-실행-결과)
- [기대 효과](#-기대-효과)

## 🎯 문제 정의

### 현황
게임 서비스 운영 중 발생하는 고객 문의(CS) 처리 과정에서 다음과 같은 문제들이 발견되었습니다:

- **지속적인 CS 발생**: 일 평균 수 건에서 피크 시 수십 건까지 발생
- **반복적인 문의 유형**: 유사한 패턴의 문의가 빈번하게 반복

### 핵심 문제
- 반복적인 로그 조회 작업의 비효율성
- CS 처리 수작업 부담

## 💡 해결 방안

### Agentic Workflow 도입
**Agentic Workflow**란 LLM의 추론 능력을 엔진으로 삼아, 개발자가 정의한 루프(계획-실행-검토)를 통해 자율적으로 문제를 해결하는 시스템입니다.

### 기존 LLM의 한계와 해결책

#### 한계점
- LLM은 확률적으로 결과를 생성하여 같은 입력에도 매번 다른 출력 생성
- 일관성이 보장되지 않아 중요한 작업 위임이 어려움

#### 해결책
- **Workflow와 Tool을 통한 행동 범위 통제**
- LLM은 오직 **문의 분류**에만 사용
- 분류 후 처리는 **코드로 수행**하여 예측 가능하고 일관된 결과 보장

### 자동화 가능 영역
1. **간단한 작업 자동화**: 로그 조회 및 데이터 확인
2. **권한이 필요한 작업**: 개발자 확인 후 수행
3. **사전 정의된 케이스**: 분류 기준에 따른 자동 응답 생성
4. **미정의 케이스**: 필요 정보 요청 또는 개발자 에스컬레이션

## 🛠 기술 스택

### Core Framework
- **[Microsoft Agent Framework](https://learn.microsoft.com/en-us/agent-framework/)**: C# 기반 에이전트 프레임워크 (2025년 12월 기준 프리뷰)

### LLM
- **[OpenAI API](https://platform.openai.com/)**: 클라우드 기반 LLM API
- **[GPT-5 nano](https://platform.openai.com/docs/models/gpt-5-nano)**: 비용 효율적인 경량 모델
  - 빠른 응답 속도와 합리적인 비용으로 **실시간 분류**에 최적화
  - 추론 능력과 안정성의 균형

### 기타
- **MySQL**: 로그 데이터베이스 (우편함 로그, 유저 정보)
- **CSV**: 문의 데이터 저장 및 관리
- **JSON**: 분류 규칙 정의 (Knowledge Base)

## 🏗 시스템 설계

### 아키텍처 개요
4개의 노드로 구성된 선형 Workflow

```
[CSV 문의 데이터] → [조회] → [분류] → [처리] → [갱신] → [갱신된 CSV]
                   ↓       ↓       ↓       ↓
                 Executor  LLM    Code  Executor
```
### Workflow 노드 구성

#### 1. SimpleInquiryReadExecutor
- **역할**: 문의 조회
- **기능**: CSV 파일에서 미처리 문의를 읽어옴
- **입력**: CSV 파일 경로
- **출력**: Inquiry 객체 리스트

#### 2. InquiryClassificationExecutor
- **역할**: 문의 분류 (LLM 사용)
- **기능**: 사전 정의된 분류 규칙(Knowledge Base)을 바탕으로 문의 유형 분류
- **입력**: Inquiry 객체 + 분류 규칙(RAG)
- **출력**: ClassificationResult (카테고리, 신뢰도)

#### 3. CategoryHandlerExecutor
- **역할**: 문의 처리 (코드 실행)
- **기능**: 분류된 카테고리에 따라 정의된 액션 수행
- **입력**: ClassificationResult
- **출력**: CategoryActionResponse (처리 결과, 응답 메시지)
- **처리 예시**:
  - 우편함 로그 조회 (MailboxService)
  - 유저 정보 조회 (UserNumberService)
  - 초보자 보상 상태 확인 (BeginnerRewardService)

#### 4. InquiryStatusUpdateExecutor
- **역할**: 문의 상태 갱신
- **기능**: 처리 결과를 CSV 파일에 반영
- **입력**: CategoryActionResponse
- **출력**: 갱신된 CSV 파일

### 데이터 흐름
각 노드는 **구조화된 DTO**를 통해 결과를 전달하며, 다음 노드의 입력으로 사용됩니다.

```csharp
Inquiry → ClassificationResult → CategoryActionResponse → CSV Update
```

### RAG (Retrieval-Augmented Generation) 적용

#### RAG란?
LLM이 답변 생성 전에 외부 지식 베이스에서 신뢰할 수 있는 정보를 검색(Retrieval)하고, 이를 참고하여 답변을 생성(Generation)하는 기술

#### 적용 방식
- **Knowledge Base**: `KnowledgeBase/CsCategoryRule.json`
- **분류 규칙**: JSON 형식으로 CS 유형별 키워드, 패턴 정의
- **컨텍스트 주입**: LLM에게 분류 규칙을 프롬프트와 함께 제공

#### 확장성 고려사항
- **우려점**: CS 종류가 증가하면 컨텍스트가 커져 분류 품질 저하 가능
- **해결 방안**: 검색 알고리즘(벡터 유사도, 키워드 매칭)으로 관련 규칙만 선택적으로 검색
- **LLM 특성**: 자기회귀 모델로 불필요한 정보가 문맥에 포함되면 결과에 부정적 영향

## 🔨 구현 내용

### PoC 범위
실제 서비스 환경 대신 **일반화된 데이터**로 PoC 구현

#### 입력 데이터 (CSV)
```csv
id,userId,description,status,responseMessage
1,user001,우편함에 아이템이 안 왔어요,pending,
2,user002,초보자 보상을 받았는지 확인해주세요,pending,
```

**필드 설명**:
- `id`: 문의 고유 번호
- `userId`: 사용자 ID
- `description`: 문의 내용
- `status`: 문의 상태 (pending, resolved, escalated)
- `responseMessage`: 자동 생성된 응답 메시지

#### 출력 데이터 (CSV)
```csv
id,userId,description,status,responseMessage
1,user001,우편함에 아이템이 안 왔어요,resolved,"우편함 로그를 확인한 결과, 메일이 존재합니다. 게임 내 우편함을 다시 확인해주세요."
2,user002,초보자 보상을 받았는지 확인해주세요,resolved,"초보자 보상은 이미 수령 완료된 상태입니다."
```

### 주요 처리 시나리오

#### 시나리오 1: 우편함 관련 문의 (행 1~5)
우편함 로그 테이블 조회 (`mailbox_log` 테이블)
- `0`: 미삭제 (메일 존재)
- `1`: 수령 (사용자가 수령)
- `2`: 만료 (시스템 만료)

**처리 결과**:
1. **메일 존재**: "메일이 존재합니다. 게임 내 우편함을 확인해주세요."
2. **수령 완료**: "해당 메일은 {수령일시}에 수령 완료되었습니다."
3. **만료**: "해당 메일은 {만료일시}에 만료되었습니다."
4. **메일 없음**: "해당 메일 기록이 존재하지 않습니다. 추가 정보가 필요합니다."

#### 시나리오 2: 미정의 케이스 (행 6~11)
분류 규칙에 없는 문의에 대한 처리

**처리 방식**:
- **추가 정보 필요 (행 6, 10, 11)**: "다음 정보를 추가로 제공해주세요: ..."
- **개발자 확인 필요 (행 7, 8, 9)**: "해당 문의는 개발팀 확인이 필요합니다. 에스컬레이션 처리되었습니다."

### 핵심 코드 구조

#### Agents
- `InquiryClassificationAgent`: LLM 기반 문의 분류 에이전트
- `L1ResolverAgent`: 1차 문의 처리 에이전트
- `NotificationAgent`: 알림 발송 에이전트

#### Executors (Workflow 노드)
- `SimpleInquiryReadExecutor`: CSV 읽기
- `InquiryClassificationExecutor`: LLM 분류
- `CategoryHandlerExecutor`: 액션 실행
- `InquiryStatusUpdateExecutor`: CSV 갱신
- `ClassificationResultPrinterExecutor`: 결과 출력 (디버깅용)

#### Services
- `MailboxService`: 우편함 로그 조회
- `UserNumberService`: 유저 정보 조회
- `BeginnerRewardService`: 초보자 보상 상태 확인
- `CsvService`: CSV 파일 I/O
- `CategoryActionService`: 카테고리별 액션 매핑

#### Data Models
- `Inquiry`: 문의 데이터
- `ClassificationResult`: 분류 결과
- `CategoryActionResponseBase`: 처리 결과 베이스 클래스
- `MailboxLog`: 우편함 로그 엔티티

## 📊 실행 결과

### 처리 흐름 예시

```
[입력] 문의 #1: "우편함에 아이템이 안 왔어요"
  ↓
[분류] Category: MailboxInquiry, Confidence: 0.95
  ↓
[처리] MailboxService.GetLog(userId) → status: 0 (메일 존재)
  ↓
[응답] "우편함 로그를 확인한 결과, 메일이 존재합니다..."
  ↓
[갱신] CSV status: resolved, responseMessage: "..."
```

### 성능 지표
- **처리 시간**: 평균 1~2초/건 (OpenAI API 사용)
- **분류 정확도**: PoC 환경에서 약 95% (GPT-4o-mini 기준)
- **자동 해결률**: 정의된 케이스 약 80%
- **API 비용**: 약 $0.001~0.002/건 (토큰 사용량 기준)

## 🎁 기대 효과

### 정량적 효과
- **일 평균 처리량 감소**: 1~4건 감소 예상
- **반복 작업 시간 절감**: 로그 조회 작업 자동화로 50% 이상 시간 단축
- **피크 대응력 향상**: 대량 문의 발생 시 병렬 처리 가능

### 정성적 효과
- **일관된 CS 품질**: 정의된 규칙에 따라 동일한 응답 제공
- **CS 담당자 부담 감소**: 단순 반복 작업에서 해방
- **개발자 집중도 향상**: 복잡한 케이스에만 집중 가능
- **확장 가능성**: 새로운 CS 유형 추가 용이 (JSON 규칙 추가만으로 확장)

## 향후 적용 방향

### 단기
- [ ] 더 많은 CS 유형 추가 (재화 지급, 계정 관련 등)
- [ ] 실제 Google Sheets 연동

### 중기
- [ ] 분류 정확도 향상 (컨텍스트 조정)
- [ ] 실시간 모니터링 대시보드
- [ ] 개발자 승인 워크플로우 구현

### 장기
- [ ] 벡터 검색 기반 RAG 고도화
- [ ] 학습 데이터 축적 및 모델 파인튜닝

## 📚 참고 자료

- [Microsoft Agent Framework Documentation](https://learn.microsoft.com/en-us/agent-framework/)
- [OpenAI Platform Documentation](https://platform.openai.com/docs)
- [Agentic Design Patterns](https://discuss.pytorch.kr/t/agentic-design-patterns-google-docs-424p/7661)
