---
name: GitHub 원격 / 인증 분리
description: 이 레포는 LeeHyunchae 계정 소유. 사용자 본인의 또 다른 GitHub 계정인 hydo0910 과 자격증명을 분리해서 운영 중.
type: reference
originSessionId: 2d1a4930-9bd1-4707-86bb-e20d2f74db92
---
- 원격: `https://github.com/LeeHyunchae/PrisonLife_Supercent.git`
- commit author 설정: `user.name "LeeHyunchae"` (이 레포 한정)
- 사용자는 GitHub 계정 2개 보유 (`LeeHyunchae`, `hydo0910`). 이 레포는 LeeHyunchae 소유.
- 인증은 OAuth 캐시 분리(자격증명 관리자에서 한번 로그아웃 후 LeeHyunchae 로 재로그인) 방식으로 해결됨. `credential.useHttpPath` 같은 자동 분리 설정은 안 씀.

**How to apply:**
- 푸시할 때 commit author 가 `LeeHyunchae` 인지 확인
- 인증 에러(403 denied) 재발 시 → Windows 자격증명 관리자에서 `git:https://github.com` 항목 다시 정리 후 OAuth 로 LeeHyunchae 로그인
- hydo0910 레포로 push 할 일 생기면 별도 자격증명으로 또 OAuth 한 번 거쳐야 함
