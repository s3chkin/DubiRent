# Fix Google OAuth redirect_uri_mismatch Error

## Проблем

Грешка: `Error 400: redirect_uri_mismatch`

Това означава, че redirect URI-то в Google Cloud Console не съвпада с това, което приложението изпраща.

## Решение

### Стъпка 1: Намери точния redirect URI

Приложението използва следния callback path:

```
/Identity/Account/ExternalLogin/Callback
```

### Стъпка 2: Определи порта на приложението

1. Стартирай приложението
2. Провери в конзолата на какъв порт работи (обикновено `https://localhost:5001` или `http://localhost:5000`)
3. Провери URL-а в адресната лента на браузъра

### Стъпка 3: Добави redirect URI в Google Cloud Console

1. Отиди на [Google Cloud Console](https://console.cloud.google.com/)
2. Избери проекта си
3. Отиди на **APIs & Services** > **Credentials**
4. Кликни на OAuth 2.0 Client ID (същият, който използваш)
5. Под **Authorized redirect URIs** добави следните URI-та:

За **HTTPS** (препоръчително):

```
https://localhost:5001/Identity/Account/ExternalLogin/Callback
https://localhost:7000/Identity/Account/ExternalLogin/Callback
```

За **HTTP** (ако не използваш HTTPS):

```
http://localhost:5000/Identity/Account/ExternalLogin/Callback
http://localhost:7000/Identity/Account/ExternalLogin/Callback
```

**ВАЖНО:**

- URI-тата трябва да са **ТОЧНО** същите (с главни/малки букви, наклонени черти, и т.н.)
- Не забравяй да кликнеш **SAVE** след като ги добавиш

### Стъпка 4: Проверка

След като добавиш redirect URI-тата:

1. Изчакай 1-2 минути (Google може да има забавяне)
2. Рестартирай приложението
3. Опитай отново входа с Google

## Възможни проблеми

### Проблем 1: Все още не работи

- Провери дали портът е правилен
- Провери дали използваш HTTP или HTTPS (трябва да съвпадат)
- Увери се че URI-то е точно като в конзолата (без интервали, същия порт)

### Проблем 2: Различни портове

Ако приложението работи на друг порт, добави и този порт в Google Console. Например:

- Ако работи на `https://localhost:44300`, добави `https://localhost:44300/Identity/Account/ExternalLogin/Callback`

### Проблем 3: Production URL

Когато деплойнеш приложението, добави и production URL:

```
https://yourdomain.com/Identity/Account/ExternalLogin/Callback
```

## Проверка на текущия redirect URI

За да видиш точно какво URI изпраща приложението:

1. Отвори Developer Tools в браузъра (F12)
2. Отиди на **Network** tab
3. Кликни на бутона "Continue with Google"
4. Намери заявката към Google (обикновено `accounts.google.com`)
5. Провери параметъра `redirect_uri` в URL-а
6. Това е URI-то, което трябва да добавиш в Google Console

## Пример на правилно конфигурирани URI-та в Google Console:

```
Authorized JavaScript origins:
https://localhost:5001
http://localhost:5000

Authorized redirect URIs:
https://localhost:5001/Identity/Account/ExternalLogin/Callback
http://localhost:5000/Identity/Account/ExternalLogin/Callback
```
