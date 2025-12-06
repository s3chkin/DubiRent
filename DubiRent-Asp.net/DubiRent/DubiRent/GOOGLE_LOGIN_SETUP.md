# Google Login Setup Guide

## Стъпка 1: Създаване на Google Cloud Project

1. Отиди на [Google Cloud Console](https://console.cloud.google.com/)
2. Създай нов проект или избери съществуващ
3. Запиши Project ID

## Стъпка 2: Включване на Google+ API

1. Отиди на **APIs & Services** > **Library**
2. Търси "Google+ API" или "Google Identity"
3. Кликни **Enable**

**Важно:** Google вече препоръчва да използваш "Google Identity Services" вместо стария Google+ API.

## Стъпка 3: Създаване на OAuth 2.0 Credentials

1. Отиди на **APIs & Services** > **Credentials**
2. Кликни **+ CREATE CREDENTIALS** > **OAuth client ID**
3. Ако се иска, първо създай **OAuth consent screen**:
   - Избери **External** (за тестване)
   - Попълни **App name**: "DubiRent"
   - Добави **User support email**
   - Добави **Developer contact information**
   - Кликни **Save and Continue**
   - Пропусни Scopes (или добави `email` и `profile`)
   - Добави тестови потребители ако е необходимо
   - Кликни **Save and Continue**

4. Сега създай **OAuth client ID**:
   - **Application type**: Web application
   - **Name**: DubiRent Web Client
   - **Authorized JavaScript origins**: 
     - `https://localhost:5001` (за development)
     - `https://localhost:7000` (алтернативен порт)
     - `http://localhost:5000` (ако не използваш HTTPS)
   - **Authorized redirect URIs**: 
     - `https://localhost:5001/Identity/Account/ExternalLogin/Callback`
     - `https://localhost:7000/Identity/Account/ExternalLogin/Callback`
     - `http://localhost:5000/Identity/Account/ExternalLogin/Callback` (ако не използваш HTTPS)
     - Добави и production URL когато деплойнеш
   - Кликни **Create**

5. Копирай **Client ID** и **Client Secret**

## Стъпка 4: Конфигурация в appsettings.json

Добави в `appsettings.json`:

```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "YOUR_CLIENT_SECRET"
    }
  }
}
```

## Стъпка 5: Проверка

1. Стартирай приложението
2. Отиди на Login страницата
3. Трябва да видиш бутон "Continue with Google"
4. Кликни и тествай входа

## Често срещани проблеми:

### Проблем 1: "redirect_uri_mismatch"
**Решение:** Увери се че redirect URI в Google Console точно съвпада с URL-а на приложението:
- Провери порта (5001, 7000, 5000, и т.н.)
- Провери дали използваш HTTP или HTTPS
- Провери пътя: `/Identity/Account/ExternalLogin/Callback`

### Проблем 2: "invalid_client"
**Решение:** 
- Провери дали Client ID и Client Secret са правилно копирани
- Увери се че не са с интервали или нови редове

### Проблем 3: Бутонът не се показва
**Решение:**
- Провери дали ExternalLogins не е празен в Login.cshtml
- Провери логовете за грешки
- Увери се че Google API е enabled в Google Cloud Console

### Проблем 4: "access_denied"
**Решение:**
- Провери OAuth consent screen настройките
- Ако приложението е в "Testing" режим, добави email адреса си като тестов потребител

## За Production:

1. Промени OAuth consent screen на **Published**
2. Добави production URL в Authorized JavaScript origins
3. Добави production callback URL в Authorized redirect URIs
4. Обнови appsettings.json с production credentials
5. Използвай appsettings.Production.json за production настройки

## Полезни линкове:

- [Google OAuth 2.0 Documentation](https://developers.google.com/identity/protocols/oauth2)
- [Google Identity Services](https://developers.google.com/identity)
- [OAuth Consent Screen](https://console.cloud.google.com/apis/credentials/consent)

