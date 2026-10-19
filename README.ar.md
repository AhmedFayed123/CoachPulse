# CoachPulse

منصة لإدارة التدريب والعملاء والخطط والاشتراكات والمدفوعات والمتابعة والتواصل.

## بدء التشغيل باستخدام Docker

1. انسخ `.env.example` إلى `.env` وأضف القيم السرية محلياً. لا ترفع `.env` إلى Git.
2. شغّل `docker compose up --build -d`.
3. افتح `http://localhost:8080` وسجّل دخول SuperAdmin بالقيم التي ضبطتها في `SUPERADMIN_EMAIL` و`SUPERADMIN_PASSWORD`.
4. طلب إنشاء المساحة لا يفعّل الحساب مباشرة. يراجعه SuperAdmin من لوحة المنصة ويوافق عليه مع كتابة سبب. يستطيع مالك المساحة تسجيل الدخول بعد الموافقة.

## بيانات التطوير التجريبية

الـ seed التجريبي يعمل في Development فقط. فعّله عبر `DemoSeed__Enabled=true` واضبط `DemoSeed__Password` محلياً عبر .NET User Secrets أو متغير بيئة، بكلمة مرور طولها 16 حرفاً على الأقل. ينشئ بيانات نموذجية للعملاء والمدربين والباقات والاشتراكات والمدفوعات والبرامج والوجبات والمتابعات والرسائل والإشعارات. الحسابات التجريبية موضحة في `README.md`. لا تفعّله في Production.

## التطوير والاختبارات

- تفاصيل الإعداد المحلي، Paymob، المتغيرات والنسخ الاحتياطي: [README.md](README.md)
- ملاحظات التكامل: [INTEGRATION_NOTES.md](INTEGRATION_NOTES.md)
- تشغيل محلي: [docs/RUNBOOK.md](docs/RUNBOOK.md)
- الأمان: [SECURITY_REVIEW.md](SECURITY_REVIEW.md)
