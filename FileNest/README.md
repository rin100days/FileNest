# FileNest

FileNest는 Windows용 자동 파일 정리 앱입니다. 여러 폴더에 흩어진 파일을 확장자, 파일명 키워드, 날짜 기준으로 분류해 정리하는 것을 목표로 합니다.

이 프로젝트는 별도 개발 프로그램을 설치하지 않아도 GitHub Actions에서 Windows 실행 파일을 자동으로 빌드하도록 구성되어 있습니다.

## 중요한 안전 경고

FileNest는 파일을 실제로 이동하는 앱입니다.

반드시 처음에는 아무 파일이나 들어 있는 테스트 폴더를 하나 만든 뒤, 그 폴더만 추가해서 동작을 확인하세요.

실제 다운로드, 바탕화면, 문서 폴더를 정리하기 전에 미리보기 표를 끝까지 확인해야 합니다. 앱은 [정리 실행] 버튼을 누르기 전까지 파일을 이동하지 않습니다.

## 포함된 기능

- 다운로드, 바탕화면, 문서 폴더 기본 등록
- 사용자 지정 폴더 추가
- 정리 미리보기
- 사용자가 [정리 실행]을 눌렀을 때만 실제 이동
- 마지막 정리 되돌리기
- `settings.json` 설정 저장
- `history.json` 이동 기록 저장
- 시스템 폴더 자동 제외
- 같은 이름의 파일이 있으면 `파일명 (1).확장자` 형식으로 자동 변경
- 사용 중인 파일은 건너뛰고 로그 저장

## 저장 파일 위치

앱 실행 후 아래 위치에 저장 파일이 만들어집니다.

```txt
%LocalAppData%\FileNest\settings.json
%LocalAppData%\FileNest\history.json
%LocalAppData%\FileNest\log.txt
```

## GitHub 웹사이트만 사용해서 빌드하는 방법

### 1. 새 저장소 만들기

1. GitHub에 로그인합니다.
2. 오른쪽 위의 `+` 버튼을 누릅니다.
3. `New repository`를 누릅니다.
4. 저장소 이름을 입력합니다. 예: `FileNest`
5. 공개 여부는 원하는 대로 선택합니다.
6. `Create repository`를 누릅니다.

### 2. 프로젝트 zip 풀기

1. 받은 `FileNest.zip` 파일의 압축을 풉니다.
2. 압축을 풀면 `FileNest` 폴더가 나옵니다.
3. 그 안에 `.github`, `Models`, `Services`, `App.xaml`, `FileNest.csproj`, `MainWindow.xaml` 등이 있어야 합니다.

### 3. GitHub에 파일 업로드하기

1. 방금 만든 GitHub 저장소 페이지로 들어갑니다.
2. `Add file`을 누릅니다.
3. `Upload files`를 누릅니다.
4. 압축을 푼 `FileNest` 폴더 안의 모든 파일과 폴더를 업로드 영역에 끌어다 놓습니다.
   - `FileNest` 폴더 자체가 아니라, 그 안의 내용물이 저장소 맨 위에 올라가야 합니다.
   - 저장소 맨 위에 `.github`, `Models`, `Services`, `FileNest.csproj`가 보여야 정상입니다.
5. 아래쪽의 `Commit changes` 버튼을 누릅니다.

### 4. Actions에서 자동 빌드 확인하기

1. 저장소 위쪽 메뉴에서 `Actions` 탭을 누릅니다.
2. `Build Windows EXE`라는 workflow가 보이면 클릭합니다.
3. 가장 최근 실행 항목을 클릭합니다.
4. 노란색이면 빌드 중입니다.
5. 초록색 체크 표시가 뜨면 빌드 성공입니다.
6. 빨간색 X가 뜨면 빌드 실패입니다. 이 경우 실패한 단계의 로그를 열어 오류 내용을 확인합니다.

### 5. 수동으로 다시 빌드하기

코드를 바꾸지 않고 다시 빌드하고 싶을 때는 다음 순서로 실행합니다.

1. `Actions` 탭으로 들어갑니다.
2. 왼쪽에서 `Build Windows EXE`를 선택합니다.
3. `Run workflow` 버튼을 누릅니다.
4. 초록색 `Run workflow` 버튼을 한 번 더 누릅니다.

### 6. 빌드 결과물 다운로드하기

1. `Actions` 탭으로 들어갑니다.
2. 성공한 `Build Windows EXE` 실행 항목을 클릭합니다.
3. 화면 아래쪽의 `Artifacts` 영역을 찾습니다.
4. `FileNest-win-x64`를 클릭해서 zip 파일을 다운로드합니다.
5. 다운로드한 zip의 압축을 풉니다.
6. 안에 있는 `FileNest.exe`를 실행합니다.

## Windows 보안 경고가 뜰 때

GitHub Actions에서 직접 빌드한 개인 앱은 코드 서명이 되어 있지 않기 때문에 Windows에서 보안 경고가 뜰 수 있습니다.

경고가 뜨면 다음 순서로 실행합니다.

1. `추가 정보`를 누릅니다.
2. 앱 이름이 `FileNest.exe`인지 확인합니다.
3. 본인이 직접 빌드한 파일이 맞는지 확인합니다.
4. `실행` 또는 `실행 허용`을 누릅니다.

모르는 사람이 보낸 exe 파일은 실행하지 마세요. 본인이 GitHub Actions에서 직접 빌드한 파일만 실행하세요.

## 사용 방법

1. 앱을 실행합니다.
2. 먼저 테스트 폴더를 만듭니다.
3. 테스트 폴더에 복사본 파일 몇 개를 넣습니다.
4. FileNest에서 [폴더 추가]를 눌러 테스트 폴더를 추가합니다.
5. [미리보기 새로고침]을 누릅니다.
6. 표에서 원래 파일명, 현재 위치, 이동될 폴더, 분류 이유를 확인합니다.
7. 문제가 없을 때만 [정리 실행]을 누릅니다.
8. 결과가 마음에 들지 않으면 [마지막 정리 되돌리기]를 누릅니다.

## 기본 분류 규칙

### 확장자 기준

- 이미지: `jpg`, `jpeg`, `png`, `gif`, `webp`, `heic`
- 문서: `pdf`, `docx`, `hwp`, `txt`, `md`, `pptx`, `xlsx`
- 영상: `mp4`, `mov`, `mkv`, `avi`, `webm`
- 음악: `mp3`, `wav`, `flac`, `m4a`
- 압축파일: `zip`, `rar`, `7z`
- 설치파일: `exe`, `msi`
- 코드: `html`, `css`, `js`, `ts`, `py`, `json`, `cs`
- 기타: `Others`

### 파일명 키워드 기준

- `coc`, `크툴루`, `trpg`, `시나리오`, `캐릭터시트` 포함: `Documents/TRPG`
- `과제`, `수행`, `학교`, `영어`, `국어` 포함: `Documents/School`
- `커미션`, `reference`, `ref`, `자료` 포함: `Pictures/Reference`
- `카노`, `라이카`, `yume`, `드림` 포함: `Documents/Yume`

## GitHub Actions 빌드 설정

빌드 설정 파일은 아래 위치에 있습니다.

```txt
.github/workflows/build-windows.yml
```

이 workflow는 다음 작업을 수행합니다.

1. Windows 빌드 서버에서 실행
2. 저장소 코드 가져오기
3. .NET 8 SDK 설치
4. 의존성 복원
5. `win-x64`용 단일 exe 게시
6. `publish` 폴더를 `FileNest-win-x64` artifact로 업로드

## 직접 명령어로 빌드할 때

컴퓨터에 .NET 8 SDK가 설치되어 있다면 아래 명령어로도 빌드할 수 있습니다.

```powershell
dotnet restore FileNest.csproj

dotnet publish FileNest.csproj `
  --configuration Release `
  --runtime win-x64 `
  --self-contained true `
  --output publish `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:EnableCompressionInSingleFile=true
```

완성된 파일은 `publish` 폴더에 생성됩니다.
