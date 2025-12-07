# 🚀 Performance Optimizations Guide

## Преглед

DubiRent приложението включва множество performance оптимизации за по-бързо зареждане и по-добро потребителско изживяване.

---

## ✅ Имплементирани Оптимизации

### 1. **Response Compression (Gzip/Brotli)**

**Какво прави:**

- Компресира HTTP отговори преди изпращане до клиента
- Намалява размера на данните с 60-80%
- Браузърите автоматично декомпресират данните

**Как е имплементирано:**

```csharp
// В Program.cs
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

app.UseResponseCompression(); // В middleware pipeline
```

**Резултати:**

- ✅ CSS/JS файлове: ~70% по-малки
- ✅ HTML страници: ~60-75% по-малки
- ✅ JSON отговори: ~70-80% по-малки

**Поддържани формати:**

- HTML, CSS, JavaScript
- JSON, XML
- Всички текстови формати

---

### 2. **Response Caching**

**Какво прави:**

- Кешира HTTP отговори на сървъра
- Намалява натоварването на базата данни
- По-бърз response time за идентични заявки

**Как е имплементирано:**

```csharp
// В Program.cs
builder.Services.AddResponseCaching();
app.UseResponseCaching(); // В middleware pipeline

// В Controllers
[ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
public async Task<IActionResult> Properties(PropertySearchModel search)
```

**Cache Duration по страници:**

- **Properties List:** 5 минути (300 секунди)
- **Property Details:** 5 минути
- **Home Page:** 10 минути (600 секунди)
- **Privacy Page:** 1 час (3600 секунди)
- **Sitemap:** 1 ден (86400 секунди)
- **Admin Pages:** НЕ се кешират (sensitive data)

**Vary By:**

- Query параметри (за търсене)
- Accept-Language header (за Privacy)
- Property ID (за Details)

---

### 3. **Static File Caching Headers**

**Какво прави:**

- Добавя Cache-Control headers към статични файлове
- Браузърите кешират файловете локално
- Намалява броя HTTP заявки

**Как е имплементирано:**

```csharp
// В Program.cs - StaticFileOptions
options.OnPrepareResponse = ctx =>
{
    // Immutable files (CSS, JS, images, fonts) - 1 година
    ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=31536000, immutable");

    // Other static files - 1 ден
    ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=86400");
};
```

**Cache Duration:**

- **CSS, JS, Images (PNG/JPG/WebP), Fonts:** 1 година (immutable)
- **Други статични файлове:** 1 ден

**Преимущества:**

- ✅ Браузърът не прави повторни заявки за статични файлове
- ✅ По-бързо зареждане при повторни посещения
- ✅ Намалено натоварване на сървъра

---

### 4. **Output Caching (.NET 8)**

**Какво прави:**

- Кешира изходния HTML на страниците
- По-мощен от Response Caching (кешира до рендериране)
- Гъвкави правила за invalidation

**Как е имплементирано:**

```csharp
// В Program.cs
builder.Services.AddOutputCache(options =>
{
    options.DefaultExpirationTimeSpan = TimeSpan.FromMinutes(10);
    options.SizeLimit = 100;
});

app.UseOutputCache(); // В middleware pipeline
```

**Използване:**

- Може да се добави за конкретни actions
- По-добър контрол от Response Caching
- Може да се комбинира с Response Caching

---

### 5. **Image Optimization**

**Вече имплементирано:**

- ✅ WebP формат с fallback
- ✅ Автоматично resize до 1920x1920
- ✅ JPEG качество 85%
- ✅ Lazy loading
- ✅ Responsive images

**Виж:** `IMAGE_OPTIMIZATION.md` за повече детайли

---

### 6. **Database Query Optimization**

**Вече имплементирано:**

- ✅ `.Include()` за eager loading
- ✅ `.Select()` за проекции (малко данни)
- ✅ Пагинация за големи списъци
- ✅ Индексиране на често използвани колони

---

## 📊 Очаквани Резултати

### Before Optimizations:

- **First Contentful Paint (FCP):** ~2.5s
- **Largest Contentful Paint (LCP):** ~4.0s
- **Time to Interactive (TTI):** ~5.0s
- **Total Page Size:** ~2.5MB
- **Number of Requests:** ~50

### After Optimizations:

- **First Contentful Paint (FCP):** ~1.2s ⬇️ **52% подобрение**
- **Largest Contentful Paint (LCP):** ~2.0s ⬇️ **50% подобрение**
- **Time to Interactive (TTI):** ~2.5s ⬇️ **50% подобрение**
- **Total Page Size:** ~800KB ⬇️ **68% намаление**
- **Number of Requests:** ~25 (при повторни посещения) ⬇️ **50% намаление**

---

## 🔧 Конфигурация

### Compression Levels

Можете да промените compression level в `Program.cs`:

```csharp
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal; // Fastest, Optimal, SmallestSize
});

builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Optimal;
});
```

**Опции:**

- `Fastest` - Най-бърза компресия (по-голям файл)
- `Optimal` - Баланс (препоръчително) ✅
- `SmallestSize` - Най-добра компресия (по-бавно)

### Cache Durations

Можете да промените cache durations в контролерите:

```csharp
[ResponseCache(Duration = 300)] // 5 минути
[ResponseCache(Duration = 600)] // 10 минути
[ResponseCache(Duration = 3600)] // 1 час
[ResponseCache(Duration = 86400)] // 1 ден
```

---

## 🌐 CDN (Content Delivery Network)

### Защо да използвате CDN?

CDN доставя статичните файлове от най-близкия сървър до потребителя, което значително подобрява performance.

### Препоръчителни CDN провайдъри:

1. **Cloudflare** (Free tier available)

   - Автоматична оптимизация
   - DDoS защита
   - SSL сертификати

2. **Azure CDN**

   - Интеграция с Azure
   - Гъвкави тарифи

3. **AWS CloudFront**
   - Интеграция с AWS
   - Глобална мрежа

### Как да конфигурирате CDN:

1. **За статични файлове (CSS, JS, Images):**

   ```
   https://cdn.yourdomain.com/css/site.css
   https://cdn.yourdomain.com/js/site.js
   https://cdn.yourdomain.com/images/...
   ```

2. **В `_Layout.cshtml`:**

   ```html
   <link
     rel="stylesheet"
     href="https://cdn.yourdomain.com/css/site.css"
     asp-append-version="true"
   />
   ```

3. **В `appsettings.json`:**
   ```json
   {
     "Cdn": {
       "BaseUrl": "https://cdn.yourdomain.com"
     }
   }
   ```

### Кога да използвате CDN:

- ✅ Сайт с глобален трафик
- ✅ Голям брой статични файлове
- ✅ Високо натоварване
- ❌ Малък локален сайт (може да не се изисква)

---

## 🔍 Мониториране на Performance

### Инструменти:

1. **Chrome DevTools (Lighthouse)**

   - Performance аудит
   - Core Web Vitals метрики
   - Препоръки за подобрение

2. **Google PageSpeed Insights**

   - Онлайн тестване
   - Mobile и Desktop метрики

3. **Application Insights / New Relic**
   - Real-time monitoring
   - Server-side метрики

### Core Web Vitals:

- **LCP (Largest Contentful Paint):** < 2.5s ✅
- **FID (First Input Delay):** < 100ms ✅
- **CLS (Cumulative Layout Shift):** < 0.1 ✅

---

## ⚠️ Важни Забележки

### Кога НЕ да кеширате:

1. **Admin страници** - Съдържат sensitive данни

   ```csharp
   [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
   ```

2. **User-specific данни** - Favourites, payment status

   - Тези страници вече НЕ се кешират

3. **Динамично съдържание** - Съобщения, нови имоти
   - Използват кратки cache durations (5-10 минути)

### Cache Invalidation:

При промяна на данни (нов имот, редактиране), cache автоматично expira след зададения timeout. За instant invalidation, може да добавите:

```csharp
// В AdminController след създаване/редактиране на имот
Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");
```

---

## 📝 Допълнителни Подобрения (Опционално)

### 1. Minification на CSS/JS

Инсталирайте `BundlerMinifier` или използвайте build tools:

```bash
dotnet add package BundlerMinifier.Core
```

### 2. HTTP/2 Server Push

Подобрява зареждането на критични ресурси.

### 3. Service Worker (PWA)

Offline функционалност и instant loading.

### 4. Resource Hints

Вече има `preconnect` и `dns-prefetch` в `_Layout.cshtml` ✅

---

## ✅ Проверка на Оптимизациите

### 1. Провери Compression:

```bash
# Изпрати заявка и провери headers
curl -H "Accept-Encoding: gzip, deflate, br" -I https://yoursite.com

# Трябва да видиш:
# Content-Encoding: br (или gzip)
```

### 2. Провери Caching Headers:

```bash
curl -I https://yoursite.com/css/site.css

# Трябва да видиш:
# Cache-Control: public, max-age=31536000, immutable
```

### 3. Тествай с Lighthouse:

1. Отвори Chrome DevTools
2. Отиди на вкладката "Lighthouse"
3. Избери "Performance"
4. Кликни "Generate report"

---

## 📚 Допълнителни Ресурси

- [ASP.NET Core Performance Best Practices](https://docs.microsoft.com/en-us/aspnet/core/performance/)
- [Web Vitals](https://web.dev/vitals/)
- [Google PageSpeed Insights](https://pagespeed.web.dev/)

---

**Последна актуализация:** 2025-01-27
