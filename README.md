# 명령어 도구함 (CmdToolbox)

cmd에서 직접 입력하던 Windows 명령어들을 버튼으로 모아둔 데스크톱 앱입니다. 다크 테마의 WinForms 단일 파일 프로그램입니다.

## 기능

| # | 기능 | 명령어 |
|---|---|---|
| 1 | 종료 예약 ("몇 시간 뒤" 또는 "몇 시 몇 분에", 앱을 다시 열어도 유지) | `shutdown -s -t`, `shutdown -a` |
| 2 | 전원 (잠금, 절전, 로그아웃, 재시작, 종료) | `shutdown -l`, `shutdown -r -t 0` 등 |
| 3 | IP 확인 | `ipconfig`, `ipconfig /all` |
| 4 | 인터넷 연결 확인 | `ping`, `tracert` |
| 5 | DNS 캐시 비우기 | `ipconfig /flushdns` |
| 6 | 와이파이 비밀번호 보기 | `netsh wlan show profile key=clear` |
| 7 | 임시 파일 정리 | `%TEMP%` 정리 |
| 8 | 시스템 정보, 디스크 용량 | `systeminfo` |
| 9 | 배터리 리포트 | `powercfg /batteryreport` |
| 10 | 시스템 파일 검사 | `sfc /scannow`, `DISM` |
| 11 | Windows 도구 바로가기 | `taskmgr`, `regedit`, `services.msc` 등 |

## 빌드

Windows에 기본으로 들어 있는 .NET Framework 컴파일러를 사용하므로 따로 설치할 것이 없습니다.

```
build.bat
```

같은 폴더에 `CmdToolbox.exe`가 생성됩니다. 원하는 이름으로 바꾸거나 바탕화면에 옮겨서 쓰면 됩니다.
