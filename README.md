# Bellerophon

Unity 6.3 LTS 기반 Steam 출시용 1인칭 우주 화물 운송 협동 생존/공포 게임 프로젝트입니다.

현재 목표는 게임 기능을 만들기 전에 하네스 엔지니어링 기반을 고정하는 것입니다. 하네스는 에이전트와 사람이 같은 방식으로 프로젝트를 수정하고, 테스트하고, 빌드하도록 만드는 문서/제약/자동화 계층입니다.

## 기준 환경

- Unity: `6000.3.16f1`
- Target: Windows / Steam
- Shell: PowerShell
- Version Control: Git + Git LFS
- Remote: `https://github.com/goldmembrane/bellerophon.git`

## 자주 쓰는 명령

```powershell
.\scripts\Setup-GitForUnity.ps1
.\scripts\Bootstrap-UnityProject.ps1
.\scripts\Run-HarnessValidation.ps1
.\scripts\Run-EditModeTests.ps1
.\scripts\Run-PlayModeTests.ps1
.\scripts\Run-AllChecks.ps1
.\scripts\Build-WindowsDev.ps1
```

자세한 개발 규칙은 `AGENTS.md`와 `docs/HARNESS.md`를 봅니다.
