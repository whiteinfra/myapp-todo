; MyApp — інсталятор для Windows (NSIS 3, Modern UI 2)
; Збірка:  makensis installer\myapp_installer.nsi   (або build.cmd / build.sh у корені проєкту)
; Результат: installer\MyAppInstaller.exe

Unicode true

!define APPNAME     "MyApp"
!define DESCRIPTION "менеджер справ"
!define VERSION     "1.0.0"
!define PUBLISHER   "Білоус Владислав (К-41)"
!define URL         "https://github.com/whiteinfra/myapp-todo"
!define REGKEY      "Software\${APPNAME}"
!define UNINSTKEY   "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APPNAME}"

Name "${APPNAME} ${VERSION}"
OutFile "MyAppInstaller.exe"
InstallDir "$PROGRAMFILES64\${APPNAME}"
InstallDirRegKey HKLM "${REGKEY}" "InstallDir"
RequestExecutionLevel admin
SetCompressor /SOLID lzma
BrandingText "${APPNAME} ${VERSION} · ${PUBLISHER}"
ShowInstDetails show
ShowUninstDetails show

!include "MUI2.nsh"
!include "x64.nsh"
!include "FileFunc.nsh"

!define MUI_ICON   "..\app\src\app.ico"
!define MUI_UNICON "..\app\src\app.ico"
!define MUI_ABORTWARNING
!define MUI_WELCOMEFINISHPAGE_BITMAP "welcome.bmp"
!define MUI_UNWELCOMEFINISHPAGE_BITMAP "welcome.bmp"
!define MUI_FINISHPAGE_NOAUTOCLOSE
!define MUI_UNFINISHPAGE_NOAUTOCLOSE
!define MUI_WELCOMEPAGE_TEXT "Програма встановлення допоможе встановити ${APPNAME} ${VERSION} — ${DESCRIPTION} із пріоритетами, дедлайнами, фільтрами й пошуком.$\r$\n$\r$\nДля встановлення потрібні права адміністратора й 64-розрядна Windows 10 або 11.$\r$\n$\r$\nНатисніть «Далі», щоб продовжити."
!define MUI_FINISHPAGE_RUN "$INSTDIR\myapp.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Запустити ${APPNAME}"
!define MUI_FINISHPAGE_SHOWREADME ""
!define MUI_FINISHPAGE_SHOWREADME_NOTCHECKED
!define MUI_FINISHPAGE_SHOWREADME_TEXT "Відкрити посібник користувача"
!define MUI_FINISHPAGE_SHOWREADME_FUNCTION ShowReadme

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "..\LICENSE.txt"
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH

!insertmacro MUI_LANGUAGE "Ukrainian"

; відомості про версію файлу інсталятора (вкладка «Подробиці» у властивостях файлу)
VIProductVersion "${VERSION}.0"
VIAddVersionKey /LANG=${LANG_UKRAINIAN} "ProductName" "${APPNAME}"
VIAddVersionKey /LANG=${LANG_UKRAINIAN} "FileDescription" "Інсталятор ${APPNAME} — ${DESCRIPTION}"
VIAddVersionKey /LANG=${LANG_UKRAINIAN} "CompanyName" "${PUBLISHER}"
VIAddVersionKey /LANG=${LANG_UKRAINIAN} "LegalCopyright" "© 2026 ${PUBLISHER}"
VIAddVersionKey /LANG=${LANG_UKRAINIAN} "FileVersion" "${VERSION}"
VIAddVersionKey /LANG=${LANG_UKRAINIAN} "ProductVersion" "${VERSION}"

Function .onInit
  ${IfNot} ${RunningX64}
    MessageBox MB_ICONSTOP "${APPNAME} потребує 64-розрядної Windows 10 або 11."
    Abort
  ${EndIf}
  SetRegView 64
FunctionEnd

Function ShowReadme
  Exec '"$WINDIR\notepad.exe" "$INSTDIR\docs\README.md"'
FunctionEnd

Section "${APPNAME} (обов’язково)" SecMain
  SectionIn RO
  SetShellVarContext all
  SetOutPath "$INSTDIR"
  File "..\app\myapp.exe"
  File "..\LICENSE.txt"
  SetOutPath "$INSTDIR\docs"
  File "..\docs\README.md"
  File "..\CHANGELOG.md"
  File "..\RELEASE_NOTES.md"
  WriteUninstaller "$INSTDIR\uninstall.exe"

  ; налаштування програми
  WriteRegStr HKLM "${REGKEY}" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "${REGKEY}" "Version" "${VERSION}"

  ; запис для «Програми та компоненти» / «Параметри → Програми»
  WriteRegStr   HKLM "${UNINSTKEY}" "DisplayName" "${APPNAME} — ${DESCRIPTION}"
  WriteRegStr   HKLM "${UNINSTKEY}" "DisplayVersion" "${VERSION}"
  WriteRegStr   HKLM "${UNINSTKEY}" "Publisher" "${PUBLISHER}"
  WriteRegStr   HKLM "${UNINSTKEY}" "DisplayIcon" "$INSTDIR\myapp.exe"
  WriteRegStr   HKLM "${UNINSTKEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr   HKLM "${UNINSTKEY}" "URLInfoAbout" "${URL}"
  WriteRegStr   HKLM "${UNINSTKEY}" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegStr   HKLM "${UNINSTKEY}" "QuietUninstallString" '"$INSTDIR\uninstall.exe" /S'
  WriteRegDWORD HKLM "${UNINSTKEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINSTKEY}" "NoRepair" 1
  SetDetailsPrint none                     ; GetSize інакше пише службові рядки в журнал
  ${GetSize} "$INSTDIR" "/S=0K" $0 $1 $2
  SetDetailsPrint both
  IntFmt $0 "0x%08X" $0
  WriteRegDWORD HKLM "${UNINSTKEY}" "EstimatedSize" "$0"
SectionEnd

Section "Робочий стіл" SecDesktop
  SetShellVarContext all
  CreateShortCut "$DESKTOP\${APPNAME}.lnk" "$INSTDIR\myapp.exe" "" "$INSTDIR\myapp.exe" 0
SectionEnd

Section "Меню «Пуск»" SecStart
  SetShellVarContext all
  CreateDirectory "$SMPROGRAMS\${APPNAME}"
  CreateShortCut "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk" "$INSTDIR\myapp.exe" "" "$INSTDIR\myapp.exe" 0
  CreateShortCut "$SMPROGRAMS\${APPNAME}\Видалити ${APPNAME}.lnk" "$INSTDIR\uninstall.exe"
SectionEnd

LangString DESC_Main    ${LANG_UKRAINIAN} "Програма ${APPNAME}, посібник користувача й ліцензія."
LangString DESC_Desktop ${LANG_UKRAINIAN} "Ярлик ${APPNAME} на робочому столі для всіх користувачів."
LangString DESC_Start   ${LANG_UKRAINIAN} "Папка ${APPNAME} у меню «Пуск» з ярликами запуску й видалення."
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecMain}    $(DESC_Main)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDesktop} $(DESC_Desktop)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecStart}   $(DESC_Start)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Function un.onInit
  SetRegView 64
FunctionEnd

Section "Uninstall"
  SetShellVarContext all
  Delete "$INSTDIR\myapp.exe"
  Delete "$INSTDIR\LICENSE.txt"
  Delete "$INSTDIR\docs\README.md"
  Delete "$INSTDIR\docs\CHANGELOG.md"
  Delete "$INSTDIR\docs\RELEASE_NOTES.md"
  RMDir  "$INSTDIR\docs"
  Delete "$INSTDIR\uninstall.exe"
  RMDir  "$INSTDIR"

  Delete "$DESKTOP\${APPNAME}.lnk"
  Delete "$SMPROGRAMS\${APPNAME}\${APPNAME}.lnk"
  Delete "$SMPROGRAMS\${APPNAME}\Видалити ${APPNAME}.lnk"
  RMDir  "$SMPROGRAMS\${APPNAME}"

  DeleteRegKey HKLM "${UNINSTKEY}"
  DeleteRegKey HKLM "${REGKEY}"
  ; дані користувача (%APPDATA%\MyApp\tasks.tsv) навмисно не видаляються
SectionEnd
