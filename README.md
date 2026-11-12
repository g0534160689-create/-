# מערכת ניהול הזמנות לבוש

יישום Windows לניהול הזמנות פריטי לבוש, עם הגדרת שדות ומאפיינים מראש, מעקב הזמנות, סיכומי כמויות וייצוא ל-Excel.

## יכולות
- הגדרת קטגוריות, מוצרים ושדות מותאמים אישית (מידה, צבע, גזרה וכל מאפיין נוסף).
- ניהול ערכי בחירה לכל שדה, והפעלה/השבתה של שדות.
- רישום הזמנות לפי שם המזמין, מוצר, מאפיינים, כמות והערות.
- חיפוש, סינון, עריכה ומחיקה של הזמנות.
- סיכום כמויות לפי מוצר ולפי שילובי מאפיינים.
- ייצוא XLSX של ההזמנות, סיכום המוצרים וההגדרות; אפשר גם CSV.
- שמירה מקומית במחשב באמצעות localStorage של WebView2.

## דרישות
- Windows 10/11
- .NET 10 SDK
- Microsoft Edge WebView2 Runtime

## הרצה
פתחו את `src/ClothingOrders/ClothingOrders.csproj` ב-Visual Studio 2026 / Rider עם .NET 10, ואז הריצו:
```powershell
dotnet restore src/ClothingOrders/ClothingOrders.csproj
dotnet run --project src/ClothingOrders/ClothingOrders.csproj
```

הנתונים נשמרים בפרופיל המקומי של WebView2 במחשב. מומלץ ליצור גיבוי JSON באופן תקופתי.