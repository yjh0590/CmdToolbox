# 명령어 도구함 (CmdToolbox)

cmd에서 직접 입력하던 Windows 명령어들을 버튼으로 모아둔 데스크톱 앱입니다. 다크 테마의 WinForms 단일 파일 프로그램입니다.

## 기능

| # | 기능 | 명령어 |
|---|---|---|
| 1 | 종료 예약 ("몇 시간 뒤" 또는 "몇 시 몇 분에", 앱을 다시 열어도 유지) | `shutdown -s -t`, `shutdown -a` |
| 2 | 전원 (잠금, 절전, 로그아웃, 재시작, 종료) | `shutdown -l`, `shutdown -r -t 0` 등 |
| 3 | IP 확인 (공인 IP 포함) | `ipconfig`, `ipconfig /all`, `curl ifconfig.me` |
| 4 | 인터넷 연결 확인, IP 다시 받기, 네트워크 초기화 | `ping`, `tracert`, `ipconfig /renew`, `netsh winsock reset` |
| 5 | DNS 캐시 비우기 | `ipconfig /flushdns` |
| 6 | 와이파이 비밀번호 보기 | `netsh wlan show profile key=clear` |
| 7 | 포트·프로세스 (포트 번호나 이름으로 찾아 강제 종료) | `netstat -ano`, `taskkill /F /PID` |
| 8 | 디스크 정리 (임시 파일, 휴지통, 최대 절전 파일) | `%TEMP%` 정리, `powercfg /h off` |
| 9 | 프로그램 업데이트 | `winget upgrade --all` |
| 10 | 시스템 정보, 디스크 용량, 시간 맞추기, 파일 해시 | `systeminfo`, `w32tm /resync`, `certutil -hashfile` |
| 11 | 배터리 리포트 | `powercfg /batteryreport` |
| 12 | 시스템 파일 검사, 디스크 검사 | `sfc /scannow`, `DISM`, `chkdsk /scan` |
| 13 | Windows 도구 바로가기 | `taskmgr`, `regedit`, `services.msc` 등 |

## 화면 크기 조절

Windows 디스플레이 배율을 자동으로 따릅니다. 노트북 등에서 너무 작거나 크게 보이면 창 안에서 직접 조절할 수 있습니다.

- `Ctrl + 마우스 휠` 또는 `Ctrl + +` / `Ctrl + -`: 80%~200% 사이에서 한 단계씩 확대/축소
- `Ctrl + 0`: 원래 크기(100%)

고른 크기는 저장되어 다음에 열 때도 유지됩니다. 명령이 실행 중일 때는 바뀌지 않습니다.

## 빌드

Windows에 기본으로 들어 있는 .NET Framework 컴파일러를 사용하므로 따로 설치할 것이 없습니다.

```
build.bat
```

같은 폴더에 `CmdToolbox.exe`가 생성됩니다. 원하는 이름으로 바꾸거나 바탕화면에 옮겨서 쓰면 됩니다.
