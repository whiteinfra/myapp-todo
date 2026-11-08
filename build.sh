#!/bin/sh
# Збірка на Linux / в CI: програма (Mono C#) і інсталятор (NSIS).
set -e
cd "$(dirname "$0")"
# компіляція проти еталонних збірок .NET Framework 4.8 (а не бібліотек Mono), щоб програма не використала
# методи, яких немає у Windows (.NET Framework 4.8)
API=/usr/lib/mono/4.8-api
mcs -langversion:5 -target:winexe -codepage:utf8 -optimize+ -nostdlib -lib:$API \
    -r:$API/mscorlib.dll -r:$API/System.dll -r:$API/System.Core.dll -r:$API/System.Drawing.dll -r:$API/System.Windows.Forms.dll \
    -out:app/myapp.exe -win32icon:app/src/app.ico app/src/MyApp.cs
makensis -V2 -INPUTCHARSET UTF8 installer/myapp_installer.nsi
echo "Готово:"
ls -go app/myapp.exe installer/MyAppInstaller.exe
