# ⚡ Бърз Старт - Stripe Webhook Listener

## 🚀 От горният директорий:

```powershell
.\start-stripe-webhook.ps1
```

## 🚀 От DubiRent директорий:

```powershell
cd ..
.\start-stripe-webhook.ps1
```

## 🔑 Или Ръчно (Без Скрипт):

```powershell
stripe listen --forward-to https://localhost:5001/Payment/Webhook
```

## 📝 Важно:

1. **Първо се логни** (ако още не си):
   ```powershell
   stripe login
   ```
   - Ще се отвори браузър
   - Въведи pairing code-а който се показва

2. **Стартирай webhook listener** (в отделен терминал)
3. **Копирай webhook secret** (whsec_...)
4. **Добави го в appsettings.json**

## 🎯 Webhook Secret:

Когато стартираш listener-а, ще видиш:
```
> Ready! Your webhook signing secret is whsec_xxxxxxxxxxxxx
```

Копирай този `whsec_...` и го добави в:
```json
"Stripe": {
  "WebhookSecret": "whsec_xxxxxxxxxxxxx"
}
```

## ✅ След това:

1. Стартирай приложението: `dotnet run`
2. Тествай плащанията!

