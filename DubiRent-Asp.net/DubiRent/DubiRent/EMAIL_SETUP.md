# Email Configuration Setup

## Gmail SMTP Setup

За да изпращате email уведомления чрез Gmail, следвайте тези стъпки:

### 1. Създаване на App Password в Gmail

1. Отидете на [Google Account Settings](https://myaccount.google.com/)
2. Изберете **Security** (Сигурност)
3. В секцията **2-Step Verification** (Двустепенна идентификация), ако не е активирана, я активирайте
4. Скролнете надолу до **App passwords** (Пароли за приложения)
5. Изберете **Mail** и **Other (Custom name)**
6. Въведете име (напр. "DubiRent App")
7. Натиснете **Generate** (Генерирай)
8. **Запишете 16-символната парола** - ще я използвате в appsettings.json

### 2. Конфигуриране на appsettings.json

Отворете `appsettings.json` и обновете секцията `EmailSettings`:

```json
"EmailSettings": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUsername": "seckins191@gmail.com",
  "SmtpPassword": "ijtn domd dczg cdmq",
  "FromEmail": "seckins191@gmail.com",
  "FromName": "DubiRent"
}
```

**Важно:**

- Използвайте **App Password**, не основната парола за Gmail
- `SmtpUsername` и `FromEmail` трябва да са същият Gmail адрес
- `SmtpPassword` е 16-символната парола генерирана в стъпка 1

### 3. Тестване

1. Стартирайте приложението
2. Одобрете заявка за гледане на имот в админ панела
3. Проверете дали потребителят е получил email

### Бележки

- Email уведомленията се изпращат автоматично при всяка промяна на статуса на заявката за оглед:
  - **Approved** - Когато заявката е одобрена
  - **Completed** - Когато заявката е завършена
  - **Cancelled** - Когато заявката е отменена
  - **Pending** - При промяна на статус обратно на чакащ
- Ако има проблем с изпращането на email, ще се покаже предупреждение, но статусът ще бъде обновен
- Всички email-и се конфигурират от `EmailSettings` в `appsettings.json`
- За production среда, препоръчително е да използвате специален Gmail акаунт за приложението

### Email Уведомления

Системата изпраща автоматично HTML email-и на потребителите при промяна на статуса на заявката им за оглед. Всеки статус има специфичен дизайн и съдържание.
