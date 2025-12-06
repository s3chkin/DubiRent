# Детайлна инструкция за Google Console Setup

## Проблем: redirect_uri_mismatch

URI-то, което приложението изпраща е:

```
https://localhost:7008/Identity/Account/ExternalLogin/Callback
```

## Стъпка по стъпка решение:

### 1. Отиди на Google Cloud Console

- Отвори [Google Cloud Console](https://console.cloud.google.com/)
- Увери се че си в правилния проект

### 2. Отвори OAuth 2.0 Client ID

- Отиди на **APIs & Services** → **Credentials**
- Намери OAuth 2.0 Client ID (който започва с `784457774292-...`)
- Кликни на него за да го редактираш

### 3. Добави JavaScript Origins

В полето **Authorized JavaScript origins**, добави точно (без интервали, без крайни точки):

```
https://localhost:7008
http://localhost:5028
```

**ВАЖНО:**

- Без `http://` или `https://` в началото НЕ работи
- Без `/` в края
- Без интервали преди или след

### 4. Добави Redirect URIs

В полето **Authorized redirect URIs**, добави точно (един по един, всеки на нов ред):

```
https://localhost:7008/Identity/Account/ExternalLogin/Callback
http://localhost:5028/Identity/Account/ExternalLogin/Callback
```

**ВАЖНО:**

- Първата наклонена черта `/` след домейна е ОБЯЗАТЕЛНА
- Пътят трябва да е точно `/Identity/Account/ExternalLogin/Callback` (с главни букви I, A, E, L, C)
- Без интервали преди или след URI-то
- Без крайни точки или слешове в края

### 5. Запази промените

- Кликни **SAVE** в долния десен ъгъл
- Изчакай да се покаже съобщение за успех

### 6. Проверка

- Рефрешни страницата в браузъра
- Провери дали URI-тата са още там (понякога Google Console ги изтрива)
- Изчакай 2-3 минути за да се синхронизират промените

### 7. Рестартирай приложението

- Спри приложението (Ctrl+C)
- Стартирай го отново
- Опитай входа отново

## Често срещани грешки:

### Грешка 1: Неправилна наклонена черта

❌ Грешно: `https://localhost:7008Identity/Account/ExternalLogin/Callback`
✅ Правилно: `https://localhost:7008/Identity/Account/ExternalLogin/Callback`

### Грешка 2: Главни/малки букви

URI-то трябва да е точно с главни/малки букви както е посочено.

### Грешка 3: Интервали

❌ Грешно: `https://localhost:7008/Identity/Account/ExternalLogin/Callback ` (интервал в края)
✅ Правилно: `https://localhost:7008/Identity/Account/ExternalLogin/Callback`

### Грешка 4: Липсващ протокол

❌ Грешно: `localhost:7008/Identity/Account/ExternalLogin/Callback`
✅ Правилно: `https://localhost:7008/Identity/Account/ExternalLogin/Callback`

## Алтернативно решение:

Ако все още не работи, опитай да добавиш и тези варианти (с различни наклонени черти или без тях):

```
https://localhost:7008/Identity/Account/ExternalLogin/Callback/
https://localhost:7008/identity/account/externallogin/callback
http://localhost:5028/Identity/Account/ExternalLogin/Callback/
```

## Проверка на текущите настройки:

След като добавиш URI-тата в Google Console, провери:

1. Че са точно същите като в грешката
2. Че няма допълнителни интервали или символи
3. Че портът е правилен (7008 за HTTPS, 5028 за HTTP)
4. Че протоколът е правилен (https:// или http://)

## Скрийншот какво трябва да изглежда:

```
Authorized JavaScript origins:
  https://localhost:7008
  http://localhost:5028

Authorized redirect URIs:
  https://localhost:7008/Identity/Account/ExternalLogin/Callback
  http://localhost:5028/Identity/Account/ExternalLogin/Callback
```

## Ако все още не работи:

1. Провери в логовете на приложението какъв точно redirect URL се генерира
2. Копирай точно този URL от логовете
3. Добави го в Google Console
4. Рестартирай приложението
