# Stripe Payment Integration Setup

## Преглед

DubiRent интегрира **Stripe** за обработка на плащания за резервиране на имоти.

## ⚠️ Важно: Не е нужно сайтът да е публикуван!

**Можете да тествате цялата Stripe интеграция локално!**

- ✅ Stripe Checkout работи перфектно на localhost
- ✅ Можете да тествате плащания локално
- ✅ За webhooks използвайте Stripe CLI (безплатно)

**Вижте `STRIPE_LOCAL_TESTING.md` за подробни инструкции за локално тестване!**

---

## Стъпки за конфигуриране

### 1. Създаване на Stripe Акаунт

1. Отидете на [Stripe.com](https://stripe.com)
2. Създайте безплатен акаунт
3. Потвърдете email адреса си

### 2. Получаване на API Ключове

1. Влезте в [Stripe Dashboard](https://dashboard.stripe.com)
2. Отидете на **Developers** → **API keys**
3. За тестване използвайте **Test mode** ключовете:
   - **Publishable key** (започва с `pk_test_`)
   - **Secret key** (започва с `sk_test_`)

### 3. Конфигуриране на Webhook

**Вариант А: За Local Development (тестване локално)**

1. Инсталирайте [Stripe CLI](https://stripe.com/docs/stripe-cli)
2. Регистрирайте се с Stripe CLI: `stripe login`
3. Препратете webhook events към локалния сървър:
   ```
   stripe listen --forward-to localhost:5000/Payment/Webhook
   ```
4. CLI ще покаже webhook secret (започва с `whsec_`) - използвайте го в `appsettings.json`
5. Webhook events ще се пренасочват автоматично към вашия локален сървър

**Вариант Б: За Production (публикуван сайт)**

1. В Stripe Dashboard отидете на **Developers** → **Webhooks**
2. Натиснете **Add endpoint**
3. Въведете URL: `https://your-domain.com/Payment/Webhook`
4. Изберете следните събития:
   - `checkout.session.completed`
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
5. Запишете **Signing secret** (започва с `whsec_`)

### 4. Конфигуриране на appsettings.json

Отворете `appsettings.json` и добавете:

```json
"Stripe": {
  "PublishableKey": "pk_test_YOUR_PUBLISHABLE_KEY",
  "SecretKey": "sk_test_YOUR_SECRET_KEY",
  "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET"
}
```

**Важно:** Не качвайте `appsettings.json` в git! Използвайте `appsettings.example.json` като шаблон.

### 5. За Production

Когато сте готови за production:

1. Превключете в **Live mode** в Stripe Dashboard
2. Вземете **Live** API ключовете
3. Обновете `appsettings.json` с live ключовете
4. Обновете webhook URL с production домейна
5. Обновете webhook secret с новия signing secret

## Как работи

### Процес на плащане:

1. **Потребител кликва "Pay with Stripe"** на страницата с детайли на имот
2. **Създава се Stripe Checkout Session** с детайлите на плащането
3. **Потребителят се пренасочва към Stripe Checkout** страница
4. **Потребителят въвежда данните за карта** и плаща
5. **Stripe обработва плащането** и пренасочва обратно
6. **Webhook уведомява приложението** за успешно плащане
7. **Запис се създава в базата данни** с детайлите на плащането

### Структура на данните:

- **Payment Model** - съхранява информация за плащането
- **StripeService** - обработва комуникацията с Stripe API
- **PaymentController** - управлява плащанията и webhooks

## Тестване

### Тестови карти:

Използвайте следните тестови карти за тестване:

- **Успешно плащане:** `4242 4242 4242 4242`
- **Недостатъчни средства:** `4000 0000 0000 9995`
- **Отказано плащане:** `4000 0000 0000 0002`

За всяка карта:

- **Expiry:** Всяка бъдеща дата (напр. 12/25)
- **CVC:** Всяка 3-цифрена стойност (напр. 123)
- **ZIP:** Всяка 5-цифрена стойност

## Безопасност

- ✅ API ключовете са в `appsettings.json` (не в git)
- ✅ Webhook requests се валидират с signing secret
- ✅ Плащанията се обработват чрез Stripe (не съхраняваме данни за карти)
- ✅ HTTPS е задължителен за production

## Поддържани функции

- ✅ Stripe Checkout Sessions (hosted payment page)
- ✅ Webhook обработка за автоматично обновяване на статуси
- ✅ Записване на плащания в базата данни
- ✅ Успешна/неуспешна страници
- ✅ Защита с автентикация (само логнати потребители)

## Документация

- [Stripe Documentation](https://stripe.com/docs)
- [Stripe Checkout](https://stripe.com/docs/payments/checkout)
- [Stripe Webhooks](https://stripe.com/docs/webhooks)
