# 🔑 Помощ за Stripe Login

## ✅ Сега (в текущия терминал):

Виждаш:
```
Your pairing code is: idol-led-tender-safely
Press Enter to open the browser...
```

### Стъпка 1: Натисни Enter

Това ще отвори браузър автоматично.

### Стъпка 2: В браузъра

1. Ще видиш страница от Stripe
2. Въведи pairing code: **`idol-led-tender-safely`**
3. Кликни **"Allow access"** или **"Confirm"**

### Стъпка 3: Проверка

След успешен login, в терминала ще видиш:
```
Done! The Stripe CLI is configured...
```

## 🚀 След Login - Стартирай Webhook Listener

### Вариант 1: От текущата директория

```powershell
.\start-stripe-webhook.ps1
```

### Вариант 2: Ръчно (По-Бързо)

```powershell
stripe listen --forward-to https://localhost:5001/Payment/Webhook
```

## 📋 След стартиране:

Ще видиш нещо като:
```
> Ready! Your webhook signing secret is whsec_xxxxxxxxxxxxx
```

**ВАЖНО:** Копирай този `whsec_...` secret!

## 🔧 Добави Secret в appsettings.json

Отвори `DubiRent\appsettings.json` и обнови:

```json
"Stripe": {
  "PublishableKey": "pk_test_...",
  "SecretKey": "sk_test_...",
  "WebhookSecret": "whsec_xxxxxxxxxxxxx"  // ← Този отгоре
}
```

## ✅ Готово!

След това:
1. Стартирай приложението: `cd DubiRent; dotnet run`
2. Тествай плащанията!

