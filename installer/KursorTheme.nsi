Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"

!ifndef APP_EXE
  !error "APP_EXE is not defined"
!endif
!ifndef OUT_FILE
  !error "OUT_FILE is not defined"
!endif
!ifndef APP_ICON
  !error "APP_ICON is not defined"
!endif

!define APP_NAME "Kursor Theme"
!define APP_VERSION "0.2.0"
!define APP_PUBLISHER "Kursor Theme"
!define APP_UNINSTALL_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\KursorTheme"

Name "${APP_NAME}"
BrandingText "${APP_NAME} ${APP_VERSION}"
OutFile "${OUT_FILE}"
InstallDir "$LOCALAPPDATA\Programs\KursorTheme"
InstallDirRegKey HKCU "Software\KursorTheme" "InstallDir"
RequestExecutionLevel user
SetCompressor /SOLID lzma
SetCompress auto
Icon "${APP_ICON}"
UninstallIcon "${APP_ICON}"
ShowInstDetails nevershow
ShowUninstDetails nevershow

VIProductVersion "0.2.0.0"
VIAddVersionKey "ProductName" "${APP_NAME}"
VIAddVersionKey "CompanyName" "${APP_PUBLISHER}"
VIAddVersionKey "FileDescription" "${APP_NAME} installer"
VIAddVersionKey "FileVersion" "${APP_VERSION}"
VIAddVersionKey "ProductVersion" "${APP_VERSION}"
VIAddVersionKey "LegalCopyright" ""

!define MUI_ABORTWARNING
!define MUI_ICON "${APP_ICON}"
!define MUI_UNICON "${APP_ICON}"
!define MUI_FINISHPAGE_RUN "$INSTDIR\KursorTheme.exe"
!define MUI_FINISHPAGE_RUN_TEXT "Запустить ${APP_NAME}"
!define MUI_WELCOMEPAGE_TITLE "Установка ${APP_NAME}"
!define MUI_WELCOMEPAGE_TEXT "Живые эффекты вокруг курсора и стилизованное выделение области.$\r$\n$\r$\nПриложение устанавливается для текущего пользователя — права администратора не требуются.$\r$\n$\r$\nНажмите «Далее», чтобы продолжить."

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_COMPONENTS
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Russian"

LangString DESC_Main ${LANG_RUSSIAN} "Само приложение Kursor Theme и ярлыки в меню «Пуск»."
LangString DESC_Desktop ${LANG_RUSSIAN} "Дополнительный ярлык на рабочем столе."

Section "Kursor Theme" SecMain
  SetShellVarContext current

  ; Close a running instance so the executable can be replaced.
  nsExec::Exec 'taskkill /IM KursorTheme.exe /F'

  SetOutPath "$INSTDIR"
  File "${APP_EXE}"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  CreateDirectory "$SMPROGRAMS\${APP_NAME}"
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\KursorTheme.exe" "" "$INSTDIR\KursorTheme.exe" 0
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\Удалить ${APP_NAME}.lnk" "$INSTDIR\Uninstall.exe"

  WriteRegStr HKCU "Software\KursorTheme" "InstallDir" "$INSTDIR"

  WriteRegStr HKCU "${APP_UNINSTALL_KEY}" "DisplayName" "${APP_NAME}"
  WriteRegStr HKCU "${APP_UNINSTALL_KEY}" "DisplayIcon" "$INSTDIR\KursorTheme.exe"
  WriteRegStr HKCU "${APP_UNINSTALL_KEY}" "DisplayVersion" "${APP_VERSION}"
  WriteRegStr HKCU "${APP_UNINSTALL_KEY}" "Publisher" "${APP_PUBLISHER}"
  WriteRegStr HKCU "${APP_UNINSTALL_KEY}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "${APP_UNINSTALL_KEY}" "UninstallString" '"$INSTDIR\Uninstall.exe"'
  WriteRegStr HKCU "${APP_UNINSTALL_KEY}" "QuietUninstallString" '"$INSTDIR\Uninstall.exe" /S'
  WriteRegDWORD HKCU "${APP_UNINSTALL_KEY}" "NoModify" 1
  WriteRegDWORD HKCU "${APP_UNINSTALL_KEY}" "NoRepair" 1
SectionEnd

Section /o "Ярлык на рабочем столе" SecDesktop
  SetShellVarContext current
  CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\KursorTheme.exe" "" "$INSTDIR\KursorTheme.exe" 0
SectionEnd

!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SecMain} $(DESC_Main)
  !insertmacro MUI_DESCRIPTION_TEXT ${SecDesktop} $(DESC_Desktop)
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Section "Uninstall"
  SetShellVarContext current

  nsExec::Exec 'taskkill /IM KursorTheme.exe /F'

  Delete "$INSTDIR\KursorTheme.exe"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR"

  Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
  Delete "$SMPROGRAMS\${APP_NAME}\Удалить ${APP_NAME}.lnk"
  RMDir "$SMPROGRAMS\${APP_NAME}"

  Delete "$DESKTOP\${APP_NAME}.lnk"

  DeleteRegKey HKCU "${APP_UNINSTALL_KEY}"
  DeleteRegKey HKCU "Software\KursorTheme"
  DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "KursorTheme"
SectionEnd
