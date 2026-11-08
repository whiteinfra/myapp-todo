# Встановлення MyApp

## Системні вимоги

- Windows 10 (1903+) або Windows 11, **64-розрядна**;
- .NET Framework 4.8 (вбудований у Windows 10/11);
- 1 МБ вільного місця;
- обліковий запис із правами адміністратора (для встановлення й видалення).

## Встановлення через інсталятор

1. Завантажте `MyAppInstaller.exe` з [GitHub Releases](https://github.com/whiteinfra/myapp-todo/releases).
2. Запустіть файл і підтвердьте запит контролю облікових записів (UAC). Інсталятор не має цифрового підпису, тому SmartScreen може показати попередження: «Докладніше» → «Однаково запустити».
3. Пройдіть кроки майстра:
   1. **Вітання** → «Далі».
   2. **Ліцензійна угода** (MIT) → «Приймаю».
   3. **Компоненти**: «MyApp (обов’язково)», «Робочий стіл» (ярлик), «Меню «Пуск»» (ярлики запуску й видалення) → «Далі».
   4. **Тека встановлення** (типово `C:\Program Files\MyApp`) → «Встановити».
   5. **Завершення**: за потреби залиште позначку «Запустити MyApp» → «Готово».

### Що робить інсталятор

| Що | Де |
|---|---|
| Файли програми | `C:\Program Files\MyApp\myapp.exe`, `LICENSE.txt`, `uninstall.exe`, `docs\` (README, CHANGELOG, RELEASE_NOTES) |
| Ярлики | `C:\Users\Public\Desktop\MyApp.lnk`; меню «Пуск» → `MyApp` (запуск і «Видалити MyApp») |
| Реєстр: налаштування | `HKLM\SOFTWARE\MyApp` — `InstallDir`, `Version` |
| Реєстр: запис для видалення | `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MyApp` — `DisplayName`, `DisplayVersion`, `Publisher`, `UninstallString`, `QuietUninstallString`, `DisplayIcon`, `EstimatedSize` |

Дані користувача програма зберігає окремо від програми — у `%APPDATA%\MyApp\tasks.tsv`.

### Тихе встановлення (для адміністраторів)

```bat
MyAppInstaller.exe /S                          :: усі компоненти, тека за замовчуванням
MyAppInstaller.exe /S /D=D:\Tools\MyApp        :: інша тека (параметр /D — останній, без лапок)
```

## Перевірка встановлення

```bat
dir "C:\Program Files\MyApp"
reg query HKLM\SOFTWARE\MyApp
reg query HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\MyApp /v DisplayName
"C:\Program Files\MyApp\myapp.exe" --version
```

Програма має відкриватися з ярлика на робочому столі або з меню «Пуск».

## Деінсталяція

Будь-який із способів:

- **Параметри → Програми → Інстальовані програми** → «MyApp — менеджер справ» → «Видалити»;
- **Панель керування → Програми та компоненти** → «MyApp — менеджер справ» → «Видалити»;
- меню «Пуск» → `MyApp` → «Видалити MyApp»;
- тихо: `"C:\Program Files\MyApp\uninstall.exe" /S`.

Деінсталятор видаляє файли й теку програми, ярлики та обидва ключі реєстру. Файл даних `%APPDATA%\MyApp\tasks.tsv` навмисно залишається, щоб після повторного встановлення список справ не зник. Якщо він не потрібен, видаліть теку `%APPDATA%\MyApp` вручну.
