/* eslint-disable react-refresh/only-export-components */
import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'

export type Language = 'ar' | 'en'
type LanguageContextValue = { language: Language; setLanguage: (language: Language) => void; t: (text: string) => string }
const LanguageContext = createContext<LanguageContextValue | null>(null)

const arabic: Record<string, string> = {
  'Club': 'النادي', 'Clubs': 'الأندية', 'Workspace': 'النادي', 'Workspaces': 'الأندية', 'My club': 'ناديي',
  'Search by club name': 'ابحث باسم النادي', 'Available clubs': 'الأندية المتاحة',
  ...{ Overview: 'نظرة عامة', Clients: 'العملاء', 'Coaching team': 'فريق التدريب', 'Check-in reviews': 'مراجعة المتابعات', Programs: 'البرامج', Exercises: 'التمارين', 'Meal plans': 'خطط الوجبات', Plans: 'الباقات', Conversations: 'المحادثات', Payments: 'المدفوعات', Subscriptions: 'الاشتراكات', Notifications: 'الإشعارات', 'AI tools': 'أدوات الذكاء الاصطناعي', 'AI review': 'مراجعة الذكاء الاصطناعي', 'Workspace settings': 'إعدادات النادي', Analytics: 'التحليلات', 'Audit log': 'سجل التدقيق', Account: 'الحساب', 'Sign out': 'تسجيل الخروج', Menu: 'القائمة' },
  ...{ 'Training programs': 'البرامج التدريبية', 'Create a program, add its exercises, then assign it to a client.': 'أنشئ برنامجاً، أضف التمارين، ثم اسنده إلى عميل.', 'Meal plans': 'خطط الوجبات', 'Create a meal plan, add nutrition details, and assign it to a client.': 'أنشئ خطة وجبات، أضف تفاصيل التغذية، ثم اسندها إلى عميل.', 'Workspace settings': 'إعدادات النادي', 'Only the workspace owner can change these settings.': 'يمكن لمالك النادي فقط تعديل هذه الإعدادات.', 'Manage workspace details, branding and payment methods.': 'أدر بيانات النادي وهويته وطرق الدفع.' },
  ...{ 'The page': 'الصفحة', 'Items per page': 'عدد العناصر في الصفحة', Items: 'العناصر', 'Total count': 'إجمالي النتائج', 'No data available': 'لا توجد بيانات متاحة', 'No items found': 'لا توجد عناصر', 'Loading data...': 'جارٍ تحميل البيانات...', 'Select from list': 'اختر من القائمة', 'No items available': 'لا توجد عناصر متاحة', 'No messages yet': 'لا توجد رسائل بعد', 'Send message': 'إرسال رسالة', 'Search clients': 'ابحث عن عميل', 'Unread only': 'غير المقروءة فقط', Inbox: 'صندوق الوارد', 'Select a conversation': 'اختر محادثة', 'Back to conversations': 'العودة للمحادثات' },
  ...{ 'My dashboard': 'لوحة العميل', 'My program': 'برنامجي', 'My progress': 'تقدمي', 'My meal plan': 'خطة وجباتي', 'My messages': 'رسائلي', 'My check-ins': 'متابعاتي', 'My payments': 'مدفوعاتي', 'My subscription': 'اشتراكي', 'My profile': 'ملفي الشخصي', 'My coach': 'مدربي', 'Workout logs': 'سجل التمارين', 'Client details': 'بيانات العميل', 'Tenant plans': 'باقات النادي', 'Payment settings': 'إعدادات الدفع' },
  ...{ 'Start date': 'تاريخ البدء', 'Program name': 'اسم البرنامج', 'Duration in days': 'المدة بالأيام', 'Create draft': 'إنشاء مسودة', 'Workspace programs': 'برامج النادي', 'Open a program to review or edit it. Status and assignment are shown below.': 'افتح البرنامج لمراجعته أو تعديله. تظهر حالته وإسناده أدناه.', 'No programs yet. Create your first draft above.': 'لا توجد برامج بعد. أنشئ أول مسودة أعلاه.', 'Manage plan': 'إدارة الخطة', 'Manage': 'إدارة', 'Create a meal plan': 'إنشاء خطة وجبات', 'Plan name': 'اسم الخطة', 'Create meal draft': 'إنشاء مسودة', 'Workspace meal plans': 'خطط وجبات النادي', 'Open a plan to manage meals or assign it.': 'افتح الخطة لإدارة الوجبات أو إسنادها.', 'No meal plans yet.': 'لا توجد خطط وجبات بعد.' },
  ...{ 'Exercise library': 'مكتبة التمارين', 'AI progress review': 'مراجعة الذكاء الاصطناعي والتقدم', 'Tenant plans': 'الباقات', 'Client details': 'تفاصيل العميل', 'Coach team': 'فريق التدريب', 'Check-in review': 'مراجعة المتابعات', 'My workout log': 'سجل تماريني', 'Payment history': 'سجل المدفوعات', 'Notification center': 'مركز الإشعارات', 'Account settings': 'إعدادات الحساب', 'Billing and subscriptions': 'الفوترة والاشتراكات', 'Platform dashboard': 'لوحة إدارة المنصة', 'Dashboard': 'لوحة التحكم', 'Getting started': 'ابدأ هنا', 'My health': 'بياناتي الصحية', 'Workout log': 'سجل التمارين', 'Check-ins': 'المتابعات', 'Subscription & payments': 'الاشتراك والمدفوعات', 'Messages': 'الرسائل' },
  ...{ page: 'الصفحة', pageSize: 'عدد العناصر في الصفحة', totalCount: 'إجمالي النتائج', items: 'العناصر', id: 'المعرّف', name: 'الاسم', title: 'العنوان', email: 'البريد الإلكتروني', status: 'الحالة', role: 'الدور', amount: 'المبلغ', currency: 'العملة', price: 'السعر', createdAt: 'تاريخ الإنشاء', updatedAt: 'آخر تحديث', clientId: 'العميل', coachId: 'المدرب', tenantId: 'معرّف النادي', planId: 'الباقة', subscriptionId: 'الاشتراك', paymentId: 'الدفعة', programId: 'البرنامج', exerciseId: 'التمرين', purpose: 'الغرض', originalName: 'اسم الملف', contentType: 'نوع الملف', sizeBytes: 'حجم الملف', riskLevel: 'مستوى المخاطر', riskScore: 'درجة المخاطر', unreadCount: 'رسائل غير مقروءة', durationDays: 'المدة بالأيام', durationWeeks: 'المدة بالأسابيع', startDate: 'تاريخ البدء', endDate: 'تاريخ الانتهاء', expiresAt: 'تاريخ الانتهاء', features: 'المميزات', meals: 'الوجبات', exercises: 'التمارين', workoutPlan: 'خطة التمرين', alerts: 'التنبيهات', message: 'الرسالة', body: 'التفاصيل', type: 'النوع', action: 'الإجراء', entityType: 'نوع السجل', entityId: 'السجل', actorRole: 'دور المنفّذ', calories: 'السعرات الحرارية', protein: 'بروتين', carbs: 'كربوهيدرات', fat: 'دهون', muscleGroup: 'المجموعة العضلية', equipment: 'المعدات', rowVersion: 'إصدار السجل', frequency: 'التكرار', dayOfWeek: 'يوم الأسبوع' },
  ...{ 'Exercise library': 'مكتبة التمارين', Clients: 'العملاء', 'Coach team': 'فريق المدربين', 'Check-ins review': 'مراجعة Check-ins', 'Paymob return': 'عودة Paymob', 'Getting started': 'التهيئة الأولى', 'My profile': 'ملفي الشخصي والصحي', 'My check-in': 'Check-ins', 'My subscriptions and payments': 'الاشتراكات والمدفوعات', 'Conversation with coach': 'المحادثة مع المدرب', Analytics: 'التحليلات', 'AI workspace': 'الذكاء الاصطناعي', 'My dashboard': 'لوحة العميل', 'My program': 'برنامجي', 'My progress': 'تقدمي', 'My meal plan': 'خطة وجباتي', 'A quick look at your training and account.': 'نظرة سريعة على تدريبك وحسابك.', 'Your current plan, today\'s workout and training history': 'خطتك الحالية وتمرين اليوم وسجل تدريبك.', 'Your measurements and progress': 'قياساتك وتقدمك.', 'Your meals and nutrition': 'وجباتك وتغذيتك.', 'Keep client conversations and new messages organized in one place.': 'تابع رسائل عملائك ورد عليهم من مكان واحد.', 'Your updates, reminders and account activity.': 'تحديثاتك وتذكيراتك ونشاط حسابك.', 'Read-only activity history. The server limits results to your workspace.': 'سجل نشاط للقراءة فقط. تقتصر النتائج على ناديك.', 'Review each client\'s plan and status, then take the appropriate action.': 'راجع باقة كل عميل وحالته، ثم اتخذ الإجراء المناسب.', 'A workspace summary': 'ملخص النادي' },
  ...{ Message: 'الرسالة', 'Write to': 'اكتب إلى', 'Attach photos, videos or files': 'أرفق صوراً أو فيديوهات أو ملفات', 'Uploading files…': 'جارٍ رفع الملفات…', 'Sending…': 'جارٍ الإرسال…', 'Send message': 'إرسال الرسالة', 'Manage my messages': 'إدارة رسائلي', 'Select a message to delete': 'اختر رسالة لحذفها', 'Delete my message': 'حذف رسالتي', 'Client conversation': 'محادثة العميل', 'Presence unknown': 'حالة الاتصال غير معروفة', 'Online now': 'متصل الآن', Offline: 'غير متصل', 'Typing…': 'يكتب الآن…', Unread: 'غير مقروءة', 'Mark as read': 'تحديد كمقروءة', 'Back to conversations': 'العودة للمحادثات', Inbox: 'صندوق الوارد', 'No messages yet': 'لا توجد رسائل بعد', 'Send a message to start the conversation.': 'أرسل رسالة لبدء المحادثة.', 'Conversation history': 'سجل المحادثة', Attachment: 'مرفق', 'Message deleted': 'تم حذف الرسالة', Read: 'مقروءة', 'You can attach up to 10 files per message.': 'يمكنك إرفاق 10 ملفات كحد أقصى في الرسالة.', 'The uploaded file could not be identified.': 'تعذر التعرف على الملف بعد رفعه.', 'Could not send the message.': 'تعذر إرسال الرسالة.', 'Delete your message?': 'هل تريد حذف رسالتك؟', 'Could not delete the message.': 'تعذر حذف الرسالة.', 'Could not update read status.': 'تعذر تحديث حالة القراءة.', 'Loading attachment…': 'جارٍ تحميل المرفق…', 'Could not load attachment.': 'تعذر تحميل المرفق.', 'Open attachment': 'فتح المرفق', New: 'جديد', 'All notifications': 'كل الإشعارات', 'Mark all as read': 'تحديد الكل كمقروء', 'Notification preferences': 'تفضيلات الإشعارات', 'Delivery settings': 'إعدادات الإرسال', 'Choose where each type of update is delivered.': 'اختر طريقة استلام كل نوع من التحديثات.' },


}

Object.assign(arabic, {
  'Menu': '\u0627\u0644\u0642\u0627\u0626\u0645\u0629',
  'Protein': 'بروتين', 'Carbs': 'كربوهيدرات', 'Fat': 'دهون', 'Weekly': 'أسبوعي', 'BiWeekly': 'كل أسبوعين', 'Monthly': 'شهرياً',
  'INBOX': 'صندوق الوارد', 'Your notifications': 'إشعاراتك', 'Important updates from your coaching workspace.': 'تحديثات مهمة من ناديك التدريبي.',
  'unread': 'غير مقروء', 'Previous': 'السابق', 'Page': 'صفحة', 'of': 'من', 'Next': 'التالي', 'Notification type': 'نوع الإشعار',
  'In app': 'داخل التطبيق', 'Email': 'البريد الإلكتروني', 'Security and payment critical alerts are always enabled.': 'إشعارات الأمان والمدفوعات المهمة مفعّلة دائماً.',
  'Save preferences': 'حفظ التفضيلات', 'No check-ins recorded yet. Your progress updates will appear here.': 'لم تسجل أي متابعة بعد. ستظهر تحديثات تقدمك هنا.',
  'No subscription history yet.': 'لا يوجد سجل اشتراكات بعد.', 'InstaPay is not enabled': 'InstaPay غير مفعّل',
  'Ask the club owner to enable it, or use another available payment method.': 'اطلب من مالك النادي تفعيله أو استخدم طريقة دفع أخرى.',
  'InstaPay instructions': 'تعليمات InstaPay', 'Activate a subscription manually': 'تفعيل اشتراك يدوياً',
  'This activates access immediately. Use Payments to record a cash collection.': 'سيؤدي هذا إلى تفعيل الوصول فوراً. استخدم المدفوعات لتسجيل التحصيل النقدي.',
  'Note (optional)': 'ملاحظة (اختياري)', 'Activate subscription': 'تفعيل الاشتراك', 'Workspace subscriptions': 'اشتراكات النادي',
  'Select a row to view details and management actions.': 'اختر اشتراكاً لعرض تفاصيله وإجراءاته.', 'subscriptions': 'اشتراكات',
  'No subscriptions yet.': 'لا توجد اشتراكات بعد.', 'Plan': 'الباقة', 'Status': 'الحالة', 'Price': 'السعر', 'End date': 'تاريخ الانتهاء',
  'Subscription details': 'تفاصيل الاشتراك', 'Close': 'إغلاق', 'Reason': 'السبب', 'Extension days': 'أيام التمديد', 'Extend': 'تمديد',
  'Pause': 'إيقاف مؤقت', 'Resume': 'استئناف', 'Cancel': 'إلغاء', 'No active subscription yet. Choose a plan to get started.': 'لا يوجد اشتراك نشط. اختر باقة للبدء.',
  'Pay by InstaPay': 'الدفع عبر InstaPay', 'Your subscription request is ready. Submit your transfer details below.': 'طلب اشتراكك جاهز. أدخل تفاصيل التحويل أدناه.',
  'InstaPay is turned off for this club': 'InstaPay غير مفعّل لهذا النادي', 'Ask the club owner to enable InstaPay, or choose Paymob if it is available.': 'اطلب من مالك النادي تفعيل InstaPay أو اختر Paymob إذا كان متاحاً.',
  'Reference number': 'رقم المرجع', 'Amount': 'المبلغ', 'Payment receipt': 'إيصال الدفع', 'Submit transfer': 'إرسال التحويل',
  'Workspace details': 'بيانات النادي', 'Currency': 'العملة', 'Time zone': 'المنطقة الزمنية', 'Subscription reminder days': 'أيام التذكير قبل انتهاء الاشتراك',
  'Default language': 'اللغة الافتراضية', 'Arabic': 'العربية', 'English': 'الإنجليزية', 'Branding': 'الهوية البصرية', 'Payment methods': 'طرق الدفع',
  'Enable Paymob': 'تفعيل Paymob', 'Enable InstaPay': 'تفعيل InstaPay', 'InstaPay ID': 'معرّف InstaPay', 'Account number': 'رقم الحساب',
  'Action': 'الإجراء', 'Entity type': 'نوع السجل', 'Actor ID': 'معرّف المنفّذ', 'Apply filters': 'تطبيق الفلاتر', 'Clear filters': 'مسح الفلاتر',
  'No events match these filters.': 'لا توجد أحداث تطابق هذه الفلاتر.', 'Time': 'الوقت', 'Actor': 'المنفّذ', 'Role': 'الدور', 'Entity': 'السجل',
  'Record': 'السجل', 'IP address': 'عنوان IP', 'events': 'أحداث', 'View today’s training': 'عرض تمرين اليوم',
  'Your coach has not assigned a program yet. Your training plan will appear here once it is assigned.': 'لم يعيّن مدربك برنامجاً بعد. ستظهر خطتك التدريبية هنا عند إسنادها.',
  'Today’s exercises': 'تمارين اليوم', 'sets': 'مجموعات', 'reps': 'تكرارات', 'Workspace': 'النادي', 'Workspace usage': 'استخدام النادي',
  'Platform audit log': 'سجل تدقيق المنصة', 'Read-only security and administrative events.': 'سجل للقراءة فقط للأحداث الأمنية والإدارية.',
  'AI usage': 'استخدام الذكاء الاصطناعي', 'Webhook events': 'أحداث Webhook', 'COACHPULSE / WORKSPACE': 'COACHPULSE / النادي',
  'COACHPULSE / TRAINING SPACE': 'COACHPULSE / النادي التدريبي', 'MOVEMENT LIBRARY': 'مكتبة الحركة', 'Open exercise demonstration': 'عرض التمرين',
  'Add a secure demo video URL to this exercise to show it here.': 'أضف رابط فيديو آمن لهذا التمرين لعرضه هنا.', 'Next step': 'الخطوة التالية',
  'Add exercises to the draft, then choose the client and start date.': 'أضف التمارين إلى المسودة، ثم اختر العميل وتاريخ البدء.', 'Assign program': 'إسناد البرنامج',
  'programs': 'برامج', 'days': 'أيام', 'plans': 'باقات', 'Retry loading exercises': 'إعادة تحميل التمارين', '&times;': '×'
});

Object.assign(arabic, {
  Messages: 'الرسائل',
  'Follow your client conversations and reply from one place.': 'تابع محادثات عملائك وأجب عن رسائلهم من مكان واحد.',
  Inbox: 'صندوق الوارد',
  conversations: 'محادثات',
  'Unread messages': 'الرسائل غير المقروءة',
  'Search clients': 'ابحث عن عميل',
  'Type a client name': 'اكتب اسم العميل',
  'Show all conversations': 'عرض كل المحادثات',
  'Unread only': 'غير المقروءة فقط',
  Client: 'عميل',
  'No messages yet. Start the conversation.': 'لا توجد رسائل بعد. ابدأ المحادثة.',
  'No conversations match your search.': 'لا توجد محادثات تطابق البحث.',
  'No conversations yet. Client messages will appear here.': 'لا توجد محادثات بعد. ستظهر رسائل العملاء هنا.',
  'Select a conversation': 'اختر محادثة',
  'Messages and client updates will appear here.': 'ستظهر الرسائل وتحديثات العميل هنا.',
  'Back to conversations': 'العودة للمحادثات',
  'Client conversation': 'محادثة العميل',
  'Presence unknown': 'حالة الاتصال غير معروفة',
  'Online now': 'متصل الآن', Offline: 'غير متصل',
  'Typing&': 'يكتب الآن', Unread: 'غير مقروءة',
  'Mark as read': 'تحديد كمقروءة',
  Message: 'الرسالة', 'Write to': 'اكتب إلى',
  'Attach photos, videos or files': 'إرفاق صور أو فيديو أو ملفات',
  'Uploading files&': 'جارٍ رفع الملفات...', 'Sending&': 'جارٍ الإرسال...',
  'Send message': 'إرسال الرسالة',
  'Manage my messages': 'إدارة رسائلي',
  'Select a message to delete': 'اختر رسالة لحذفها',
  'Delete my message': 'حذف رسالتي',
  'No messages yet': 'لا توجد رسائل بعد',
  'Send a message to start the conversation.': 'أرسل رسالة لبدء المحادثة.',
  'Conversation history': 'سجل المحادثة', Attachment: 'مرفق',
  'Message deleted': 'تم حذف الرسالة', Read: 'مقروءة',
  'You can attach up to 10 files per message.': 'يمكنك إرفاق 10 ملفات كحد أقصى في الرسالة.',
  'File is too large. Videos can be up to 100 MB; other files up to 10 MB.': 'حجم الملف كبير. حد الفيديو 100 ميجابايت، و10 ميجابايت لباقي الملفات.',
  'Remove attachment': 'إزالة المرفق',
  'The uploaded file could not be identified.': 'تعذر التعرف على الملف بعد رفعه.',
  'Could not send the message.': 'تعذر إرسال الرسالة.',
  'Delete your message?': 'هل تريد حذف رسالتك؟',
  'Could not delete the message.': 'تعذر حذف الرسالة.',
  'Could not update read status.': 'تعذر تحديث حالة القراءة.',
  'Loading attachment&': 'جارٍ تحميل المرفق...',
  'Could not load attachment.': 'تعذر تحميل المرفق.',
  'Open attachment': 'فتح المرفق',
  'No coach assigned': '\u0644\u0627 \u064a\u0648\u062c\u062f \u0645\u062f\u0631\u0628 \u0645\u0639\u064a\u0646',
  'Your club will connect you with a coach. Messaging becomes available after assignment.': '\u0633\u064a\u0631\u0628\u0637\u0643 \u0627\u0644\u0646\u0627\u062f\u064a \u0628\u0645\u062f\u0631\u0628. \u0633\u062a\u062a\u0627\u062d \u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u0628\u0639\u062f \u0627\u0644\u062a\u0639\u064a\u064a\u0646.',
  'Talk privately with your assigned coach.': '\u062a\u0648\u0627\u0635\u0644 \u0628\u062e\u0635\u0648\u0635\u064a\u0629 \u0645\u0639 \u0645\u062f\u0631\u0628\u0643 \u0627\u0644\u0645\u0633\u0624\u0648\u0644.',
  Coach: '\u0627\u0644\u0645\u062f\u0631\u0628', 'Your coach': '\u0645\u062f\u0631\u0628\u0643', 'your coach': '\u0645\u062f\u0631\u0628\u0643',
  'Your assigned coach': '\u0645\u062f\u0631\u0628\u0643 \u0627\u0644\u0645\u0633\u0624\u0648\u0644',
  'Coach is typing&': '\u0627\u0644\u0645\u062f\u0631\u0628 \u064a\u0643\u062a\u0628 \u0627\u0644\u0622\u0646',
  'Connecting&': '\u062c\u0627\u0631\u064d \u0627\u0644\u0627\u062a\u0635\u0627\u0644...',
  'Write a message or attach a file.': '\u0627\u0643\u062a\u0628 \u0631\u0633\u0627\u0644\u0629 \u0623\u0648 \u0623\u0631\u0641\u0642 \u0645\u0644\u0641\u064b\u0627.',
  'Message sent.': '\u062a\u0645 \u0625\u0631\u0633\u0627\u0644 \u0627\u0644\u0631\u0633\u0627\u0644\u0629.'
});

Object.assign(arabic, {
  'Protein': 'بروتين', 'Carbs': 'كربوهيدرات', 'Fat': 'دهون', 'Weekly': 'أسبوعي', 'BiWeekly': 'كل أسبوعين', 'Monthly': 'شهرياً',
  'INBOX': 'صندوق الوارد', 'Your notifications': 'إشعاراتك', 'Important updates from your coaching workspace.': 'تحديثات مهمة من ناديك التدريبي.',
  'unread': 'غير مقروء', 'Previous': 'السابق', 'Page': 'صفحة', 'of': 'من', 'Next': 'التالي', 'Notification type': 'نوع الإشعار',
  'In app': 'داخل التطبيق', 'Email': 'البريد الإلكتروني', 'Security and payment critical alerts are always enabled.': 'إشعارات الأمان والمدفوعات المهمة مفعّلة دائماً.',
  'Save preferences': 'حفظ التفضيلات', 'No check-ins recorded yet. Your progress updates will appear here.': 'لم تسجل أي متابعة بعد. ستظهر تحديثات تقدمك هنا.',
  'No subscription history yet.': 'لا يوجد سجل اشتراكات بعد.', 'InstaPay is not enabled': 'InstaPay غير مفعّل',
  'Ask the club owner to enable it, or use another available payment method.': 'اطلب من مالك النادي تفعيله أو استخدم طريقة دفع أخرى.',
  'InstaPay instructions': 'تعليمات InstaPay', 'Activate a subscription manually': 'تفعيل اشتراك يدوياً',
  'This activates access immediately. Use Payments to record a cash collection.': 'سيؤدي هذا إلى تفعيل الوصول فوراً. استخدم المدفوعات لتسجيل التحصيل النقدي.',
  'Note (optional)': 'ملاحظة (اختياري)', 'Activate subscription': 'تفعيل الاشتراك', 'Workspace subscriptions': 'اشتراكات النادي',
  'Select a row to view details and management actions.': 'اختر اشتراكاً لعرض تفاصيله وإجراءاته.', 'subscriptions': 'اشتراكات',
  'No subscriptions yet.': 'لا توجد اشتراكات بعد.', 'Plan': 'الباقة', 'Status': 'الحالة', 'Price': 'السعر', 'End date': 'تاريخ الانتهاء',
  'Subscription details': 'تفاصيل الاشتراك', 'Close': 'إغلاق', 'Reason': 'السبب', 'Extension days': 'أيام التمديد', 'Extend': 'تمديد',
  'Pause': 'إيقاف مؤقت', 'Resume': 'استئناف', 'Cancel': 'إلغاء', 'No active subscription yet. Choose a plan to get started.': 'لا يوجد اشتراك نشط. اختر باقة للبدء.',
  'Pay by InstaPay': 'الدفع عبر InstaPay', 'Your subscription request is ready. Submit your transfer details below.': 'طلب اشتراكك جاهز. أدخل تفاصيل التحويل أدناه.',
  'InstaPay is turned off for this club': 'InstaPay غير مفعّل لهذا النادي', 'Ask the club owner to enable InstaPay, or choose Paymob if it is available.': 'اطلب من مالك النادي تفعيل InstaPay أو اختر Paymob إذا كان متاحاً.',
  'Reference number': 'رقم المرجع', 'Amount': 'المبلغ', 'Payment receipt': 'إيصال الدفع', 'Submit transfer': 'إرسال التحويل',
  'Workspace details': 'بيانات النادي', 'Currency': 'العملة', 'Time zone': 'المنطقة الزمنية', 'Subscription reminder days': 'أيام التذكير قبل انتهاء الاشتراك',
  'Default language': 'اللغة الافتراضية', 'Arabic': 'العربية', 'English': 'الإنجليزية', 'Branding': 'الهوية البصرية', 'Payment methods': 'طرق الدفع',
  'Enable Paymob': 'تفعيل Paymob', 'Enable InstaPay': 'تفعيل InstaPay', 'InstaPay ID': 'معرّف InstaPay', 'Account number': 'رقم الحساب',
  'Action': 'الإجراء', 'Entity type': 'نوع السجل', 'Actor ID': 'معرّف المنفّذ', 'Apply filters': 'تطبيق الفلاتر', 'Clear filters': 'مسح الفلاتر',
  'No events match these filters.': 'لا توجد أحداث تطابق هذه الفلاتر.', 'Time': 'الوقت', 'Actor': 'المنفّذ', 'Role': 'الدور', 'Entity': 'السجل',
  'Record': 'السجل', 'IP address': 'عنوان IP', 'events': 'أحداث', 'View today’s training': 'عرض تمرين اليوم',
  'Your coach has not assigned a program yet. Your training plan will appear here once it is assigned.': 'لم يعيّن مدربك برنامجاً بعد. ستظهر خطتك التدريبية هنا عند إسنادها.',
  'Today’s exercises': 'تمارين اليوم', 'sets': 'مجموعات', 'reps': 'تكرارات', 'Workspace': 'النادي', 'Workspace usage': 'استخدام النادي',
  'Platform audit log': 'سجل تدقيق المنصة', 'Read-only security and administrative events.': 'سجل للقراءة فقط للأحداث الأمنية والإدارية.',
  'AI usage': 'استخدام الذكاء الاصطناعي', 'Webhook events': 'أحداث Webhook', 'COACHPULSE / WORKSPACE': 'COACHPULSE / النادي',
  'COACHPULSE / TRAINING SPACE': 'COACHPULSE / النادي التدريبي', 'MOVEMENT LIBRARY': 'مكتبة الحركة', 'Open exercise demonstration': 'عرض التمرين',
  'Add a secure demo video URL to this exercise to show it here.': 'أضف رابط فيديو آمن لهذا التمرين لعرضه هنا.', 'Next step': 'الخطوة التالية',
  'Add exercises to the draft, then choose the client and start date.': 'أضف التمارين إلى المسودة، ثم اختر العميل وتاريخ البدء.', 'Assign program': 'إسناد البرنامج',
  'programs': 'برامج', 'days': 'أيام', 'plans': 'باقات', 'Retry loading exercises': 'إعادة تحميل التمارين', '&times;': '×'
});

Object.assign(arabic, {
  'Better coaching.': 'تدريب أفضل.', 'Clearer progress.': 'متابعة أوضح.', 'Welcome back': 'مرحباً بعودتك', 'Sign in': 'تسجيل الدخول',
  'Club': 'النادي', 'Clubs': 'الأندية', 'Workspace': 'النادي', 'Workspaces': 'الأندية', 'Club settings': 'إعدادات النادي',
  'Club name': 'اسم النادي', 'Club link': 'رابط النادي', 'Choose your club, then enter your sign-in details.': 'اختر النادي المرتبط بحسابك، ثم أدخل بيانات الدخول.',
  'Choose a club': 'اختر النادي', 'Search by club name': 'ابحث باسم النادي', 'Available clubs': 'الأندية المتاحة', 'Loading clubs...': 'جارٍ تحميل الأندية...',
  'Could not load clubs right now.': 'تعذر تحميل الأندية الآن.', 'Club code from your invitation': 'رمز النادي في الدعوة',
  'Type the first two letters of the club name to search.': 'ابدأ بكتابة أول حرفين من اسم النادي للبحث.', 'No results. Try another name.': 'لا توجد نتائج. جرّب اسماً آخر.',
  'Can’t find your club? Open the invitation link you received.': 'لا تجد ناديك؟ افتح رابط الدعوة الذي وصلك.', 'Forgot password?': 'نسيت كلمة المرور؟',
  'First time here?': 'أول مرة هنا؟', 'Create a club': 'أنشئ نادياً', 'For coaches starting a new club': 'للمدربين الراغبين في بدء نادٍ جديد',
  'Join as a client': 'انضم كعميل', 'Have an invitation from a coach? Finish registration here.': 'لديك دعوة من مدرب؟ أكمل التسجيل هنا.',
  'Have a staff invitation?': 'لديك دعوة للانضمام كمدرب؟', 'Other account options': 'خيارات الحساب الأخرى', 'Reset password with a code': 'إعادة تعيين كلمة المرور برمز',
  'Better training and client management': 'منصة التدريب وإدارة العملاء', 'Organized programs and exercises': 'برامج وتمارين منظمة',
  'Progress tracking and communication': 'متابعة التقدم والتواصل', 'Designed for coaches and clients': 'تجربة مناسبة للمدرب والعميل',
  'Manage training, clients, and progress in one place.': 'كل ما تحتاجه لإدارة التمارين والعملاء والتقدم في مكان واحد.',
  'Your club starts here.': 'ناديك التدريبي يبدأ من هنا.', 'Your account and club data are protected by your access permissions.': 'بيانات حسابك وناديك محمية بصلاحيات الوصول الخاصة بك.',
  'Reason for activation or suspension': 'سبب التفعيل أو التعليق', 'Activate club': 'تفعيل النادي', 'Suspend club': 'تعليق النادي',
  'Schedule club deletion': 'جدولة حذف النادي', 'Selected club': 'النادي المحدد', 'Choose a club to manage its status and usage.': 'اختر النادي لإدارة حالته واستخدامه.',
  'No clubs to display.': 'لا توجد أندية لعرضها.', 'Choose a club from the list or use Manage club in the clubs table.': 'اختر نادياً من القائمة أو استخدم إدارة النادي في جدول الأندية.',
  'Training clubs': 'الأندية التدريبية', 'Club management': 'إدارة النادي', 'Platform administration': 'إدارة المنصة',
  'Create a program': 'إنشاء برنامج', 'Program details': 'تفاصيل البرنامج', 'Program exercises': 'تمارين البرنامج', 'New title': 'عنوان جديد',
  'Rest seconds': 'الراحة بالثواني', 'Save exercise changes': 'حفظ تعديل التمرين', 'Reorder today’s exercises': 'ترتيب تمارين اليوم',
  'Save order': 'حفظ الترتيب', 'Duplicate': 'نسخ', 'Save as template': 'حفظ كقالب', 'Templates': 'القوالب', 'Create program from template': 'إنشاء برنامج من القالب',
  'Muscle groups and equipment': 'المجموعات العضلية والمعدات', 'New exercise': 'تمرين جديد', 'Edit details': 'تفاصيل التعديل',
  'Plan details': 'تفاصيل الخطة', 'Plan title': 'عنوان الخطة', 'Meal name': 'اسم الوجبة', 'Calories': 'السعرات الحرارية',
  'Add meal': 'إضافة وجبة', 'Update meal': 'تحديث الوجبة', 'Delete meal': 'حذف الوجبة', 'Create plan': 'إنشاء باقة', 'Manage plan': 'إدارة الباقة',
  'Archive plan': 'أرشفة الباقة', 'Invite a client': 'دعوة عميل', 'Send invitation': 'إرسال دعوة', 'Invitations': 'الدعوات',
  'Coaches in this club': 'المدربون في النادي', 'Resend invitation': 'إعادة إرسال الدعوة', 'Cancel invitation': 'إلغاء الدعوة',
  'Public platform exercises': 'تمارين المنصة العامة', 'Choose an exercise to edit or delete. Deletion may be rejected if it is in use.': 'اختر تمريناً لتعديله أو حذفه. قد يُرفض الحذف إذا كان التمرين مستخدماً.',
  'Show health information (audited)': 'عرض البيانات الصحية (مسجّل تدقيقياً)', 'Save assigned coach': 'حفظ المدرب المسؤول', 'Check-in schedule': 'موعد المتابعة',
  'Pending payments': 'الدفعات المعلّقة', 'Record cash payment': 'تسجيل دفعة نقدية', 'Refund payment': 'استرداد دفعة', 'Export CSV': 'تصدير CSV',
  'Save profile': 'حفظ الملف الشخصي', 'Delete account and hide its data': 'حذف الحساب وإخفاء بياناته', 'Export my data': 'تصدير بياناتي',
  'My subscriptions': 'اشتراكاتي', 'Current subscription': 'اشتراكي الحالي', 'Request subscription': 'طلب اشتراك', 'Payment details and status': 'تفاصيل الدفع وحالته',
  'Download PDF invoice': 'تنزيل الفاتورة PDF', 'Pay by card through Paymob': 'الدفع بالبطاقة عبر Paymob', 'Generate program': 'توليد برنامج',
  'Review check-ins': 'مراجعة المتابعات', 'Client details': 'تفاصيل العميل', 'Health information': 'المعلومات الصحية', 'Save settings': 'حفظ الإعدادات',
  'Save branding': 'حفظ الهوية', 'Save payment methods': 'حفظ طرق الدفع', 'Workspace usage': 'استخدام النادي', 'Audit log': 'سجل التدقيق',
  'Your workspace': 'ناديك', 'Quick access': 'وصول سريع', 'View today’s training': 'عرض تمرين اليوم', 'Record a check-in': 'تسجيل متابعة',
  'Open meal plan': 'فتح خطة الوجبات', 'Message your coach': 'مراسلة مدربك', 'Training plan': 'الخطة التدريبية', 'Current program': 'البرنامج الحالي',
  'Today’s exercises': 'تمارين اليوم', 'Training history': 'سجل التدريب', 'Past and current programs': 'البرامج السابقة والحالية',
  'No training history yet.': 'لا يوجد سجل تدريب بعد.', 'Your progress starts with a check-in': 'يبدأ تقدمك بتسجيل متابعة',
  'Create a check-in': 'إنشاء متابعة', 'Check-in history': 'سجل المتابعات', 'No active meal plan': 'لا توجد خطة وجبات نشطة',
  'Your meal plan will show here when your coach shares one with you.': 'ستظهر خطة وجباتك هنا عندما يشاركها مدربك.', 'No meals have been added to this plan yet.': 'لم تتم إضافة وجبات إلى هذه الخطة بعد.'
});
Object.assign(arabic, {
  "Today's training": 'تمرين اليوم', "Today's exercises": 'تمارين اليوم', 'After you submit your first check-in, your measurements and changes over time will appear here.': 'بعد إرسال أول متابعة، ستظهر قياساتك وتغيراتك بمرور الوقت.', 'Your plan': 'خطتك', 'kcal': 'سعرة حرارية', 'total)': 'إجمالي النتائج)',
  'Client details': 'تفاصيل العميل', 'Delete': 'حذف', 'Import CSV': 'استيراد CSV', 'Invitation details (email partially hidden):': 'بيانات الدعوة العامة (البريد مقنّع):', 'Complete registration': 'إكمال التسجيل'
});
Object.assign(arabic, {
  'Not available': 'غير متاح', 'No items': 'لا توجد عناصر', 'No details available': 'لا توجد تفاصيل', 'Yes': 'نعم', 'No': 'لا',
  'SuperAdmin': 'مسؤول المنصة', 'TenantOwner': 'مالك النادي', 'CoachStaff': 'مدرب', 'Client': 'عميل', 'Active': 'نشط', 'Paused': 'متوقف مؤقتاً',
  'Archived': 'مؤرشف', 'Pending': 'قيد الانتظار', 'Accepted': 'مقبول', 'Expired': 'منتهي', 'Confirmed': 'مؤكد', 'Rejected': 'مرفوض',
  'PendingVerification': 'بانتظار التحقق', 'Cancelled': 'ملغي', 'Draft': 'مسودة', 'Template': 'قالب', 'Completed': 'مكتمل', 'Low': 'منخفض',
  'Medium': 'متوسط', 'High': 'مرتفع', 'Weekly': 'أسبوعي', 'BiWeekly': 'كل أسبوعين', 'Monthly': 'شهري', 'Cash': 'نقدي', "View today's training": 'عرض تمرين اليوم'
});
const english: Record<string, string> = Object.fromEntries(Object.entries(arabic).map(([key, value]) => [value, key]))

const englishUi: Record<string, string> = {
    'لوحة SuperAdmin': 'SuperAdmin dashboard',
    'إدارة المساحات': 'Club management',
    'الأندية التدريبية': 'Training clubs', 'مساحات التدريب': 'Training clubs',
    'مساحة الإدارة': 'Platform administration',
    'إدارة ومراقبة المنصة': 'Manage and monitor the platform',
    'إدارة النادي': 'Manage club', 'إدارة مساحة': 'Manage club',
    'اختر مساحة': 'Choose a club', 'مساحة التدريب': 'Training club', 'النادي': 'Club', 'الأندية': 'Clubs',
    'اسم المساحة': 'Club name',
    'رابط المساحة': 'Club link',
    'الحالة': 'Status',
    'تاريخ الإنشاء': 'Created',
    'الاستخدام': 'Usage',
    'العملاء': 'Clients',
    'طلبات AI اليوم': 'AI requests today',
    'التخزين': 'Storage',
    'سبب التفعيل أو التعليق': 'Reason for activation or suspension',
    'تفعيل المساحة': 'Activate club',
    'تعليق المساحة': 'Suspend club',
    'جدولة الحذف': 'Schedule deletion',
    'طلبات إنشاء المساحات': 'Club requests',
    'لا توجد طلبات تنتظر الموافقة.': 'No workspace requests are waiting for approval.',
    'مؤشرات المنصة': 'Platform metrics',
    'إجمالي المساحات': 'Total clubs',
    'المساحات النشطة': 'Active clubs',
    'المساحات الموقوفة': 'Suspended clubs',
    'إجمالي المستخدمين': 'Total users',
    'إجمالي العملاء': 'Total clients',
    'استخدام AI اليوم': 'AI usage today',
    'آخر أحداث التدقيق': 'Recent audit events',
    'حالة Webhook': 'Webhook status',
    'سجل التدقيق': 'Audit log',
    'التمارين العامة': 'Platform exercises',
    'تسجيل الخروج': 'Sign out',
    'القائمة': 'Menu',
    'تحديث البيانات': 'Refresh data',
    'نظرة عامة': 'Overview',
    'إغلاق القائمة': 'Close menu',
    'فتح قائمة الإدارة': 'Open platform menu',
    'إدارة المنصة': 'Platform administration',
    'صلاحيات مسؤول المنصة': 'Platform administrator access',
    'طلبات تنتظر الموافقة': 'Requests awaiting approval',
    'أحدث المساحات وحالتها': 'Recent workspaces and their status',
    'لا توجد مساحات بعد.': 'No workspaces yet.',
    'أحداث المنصة والمساحات للقراءة فقط.': 'Read-only platform and workspace events.',
    'البريد الإلكتروني': 'Email address',
    'كلمة المرور': 'Password',
    'اختر عميلاً': 'Choose a client',
    'لا توجد بيانات': 'No data available',
    'لا توجد نتائج': 'No results',
    'تحميل البيانات': 'Loading data',
    'تعذر تحميل البيانات': 'Could not load data',
    'إرسال': 'Send',
    'حفظ': 'Save',
    'إلغاء': 'Cancel',
    'الاسم': 'Name', 'البريد': 'Email', 'الهاتف': 'Phone', 'الدور': 'Role',
    'نشط': 'Active', 'معلّق': 'Paused', 'مؤرشف': 'Archived',
    'المبلغ': 'Amount', 'السعر': 'Price', 'المدة بالأيام': 'Duration in days',
    'المدة بالأسابيع': 'Duration in weeks', 'تاريخ البدء': 'Start date', 'تاريخ الانتهاء': 'End date',
    'المجموعة العضلية': 'Muscle group', 'المعدات': 'Equipment', 'رابط الفيديو': 'Video URL',
    'التكرارات': 'Repetitions', 'المجموعات': 'Sets', 'اليوم': 'Day', 'يوم الأسبوع': 'Day of week',
    'الوزن': 'Weight', 'الوزن بالكجم': 'Weight (kg)', 'القياسات': 'Measurements',
    'الأهداف': 'Goals', 'التفاصيل': 'Details', 'الإجراء': 'Action', 'نوع السجل': 'Record type',
    'مستوى المخاطر': 'Risk level', 'التنبيهات': 'Alerts', 'الطريقة': 'Method', 'اللغة': 'Language',
    'حفظ التعديل': 'Save changes', 'تحديث': 'Update', 'إضافة تمرين': 'Add exercise',
    'حذف التمرين': 'Delete exercise', 'حذف السجل': 'Delete record', 'أرشفة': 'Archive',
    'حذف مسودة': 'Delete draft', 'إرسال التعليق': 'Send feedback', 'ملاحظات المدرب': 'Coach notes',
    'تسجيل الدخول': 'Sign in', 'تسجيل دخول مسؤول المنصة': 'Platform administrator sign in',
    'مسؤول المنصة': 'Platform administrator', 'تأكيد البريد الإلكتروني': 'Confirm email',
    'إعادة إرسال رسالة التأكيد': 'Resend confirmation email', 'العودة لتسجيل الدخول': 'Back to sign in',
    'كلمة المرور الحالية': 'Current password', 'كلمة المرور الجديدة': 'New password',
    'تغيير كلمة المرور': 'Change password', 'معرّف العميل': 'Client ID', 'معرّف المدرب': 'Coach ID',
    'معرّف النادي': 'Club ID', 'معرّف الباقة': 'Plan ID', 'معرّف الاشتراك': 'Subscription ID',
    'معرّف الدفعة': 'Payment ID', 'معرّف البرنامج': 'Program ID', 'النادي المحدد': 'Selected club', 'المساحة المحددة': 'Selected club',
    'المساحة': 'Club', 'مساحة': 'Club', 'المساحات': 'Clubs', 'مساحات': 'Clubs', 'مساحتك': 'Your club', 'مساحتي': 'My club', 'الرابط': 'URL', 'الرابط المختصر': 'Slug', 'العملة': 'Currency',
    'آخر تحديث': 'Last updated', 'مقروء': 'Read', 'نعم': 'Yes', 'لا': 'No',
    'التمرين': 'Exercise', 'البرنامج': 'Program', 'خطة التمرين': 'Workout plan',
    'خطة الوجبات': 'Meal plan', 'الباقة': 'Plan', 'الاشتراك': 'Subscription', 'الدفعة': 'Payment',
    'العميل': 'Client', 'المدرب': 'Coach', 'التاريخ': 'Date', 'الملاحظات': 'Notes',
    'سبب الإجراء': 'Reason', 'سبب الرفض': 'Rejection reason', 'اكتب سبباً واضحاً للتفعيل أو التعليق': 'Enter a clear reason for activation or suspension', 'إرسال رسالة': 'Send message',
    'إنشاء': 'Create', 'إنشاء باقة': 'Create plan', 'إنشاء خطة': 'Create plan', 'إنشاء برنامج': 'Create program',
    'حفظ البيانات': 'Save details', 'حفظ الملف': 'Save profile', 'إسناد': 'Assign', 'إسناد الخطة': 'Assign plan',
    'إعادة المحاولة': 'Try again', 'بحث': 'Search', 'تصفية': 'Filter', 'مسح الفلاتر': 'Clear filters',
    'السابق': 'Previous', 'التالي': 'Next', 'صفحة': 'Page', 'النتائج': 'Results',
    'جارٍ تحميل البيانات...': 'Loading data...',
    'لا توجد عناصر': 'No items', 'لا توجد رسائل بعد': 'No messages yet',
    'البيانات غير متاحة': 'Data is unavailable', 'تعذّر تحميل البيانات': 'Could not load data',
    'الصلاحية غير متاحة': 'Access unavailable', 'حسابك لا يملك صلاحية تنفيذ هذا الإجراء.': 'Your account is not allowed to perform this action.',
    'الخدمة غير متاحة مؤقتاً': 'Service temporarily unavailable', 'الخدمة المطلوبة متوقفة مؤقتاً. حاول مرة أخرى بعد قليل.': 'The requested service is temporarily unavailable. Please try again shortly.',
    'انتهت جلسة الدخول': 'Your session has expired', 'سجّل الدخول مرة أخرى للمتابعة.': 'Sign in again to continue.',
    'تعذر الاتصال بالخادم. تحقق من تشغيل الخدمة واتصال الشبكة.': 'Could not connect to the server. Check that the service is running and your network is available.',
    'طلبات كثيرة خلال وقت قصير': 'Too many requests', 'انتظر قليلاً قبل إعادة المحاولة.': 'Please wait before trying again.',
    'تعذّر إتمام التغيير': 'Could not save changes', 'تغيّرت البيانات منذ آخر مرة فتحتها. حدّث الصفحة ثم حاول مجدداً.': 'This data changed since you opened it. Refresh the page and try again.',
    'راجع البيانات المدخلة': 'Check the information you entered', 'تحقق من الحقول المطلوبة والقيم ثم أعد المحاولة.': 'Check the required fields and values, then try again.',
    'قد يكون العنصر غير موجود أو لم يعد متاحاً لحسابك.': 'This item may not exist or may no longer be available to your account.',
    'رقم التتبع:': 'Trace ID:', 'مخاطر الانسحاب': 'Churn risk', 'أحداث Webhook': 'Webhook events',
    'اشتراكات قاربت على الانتهاء': 'Subscriptions nearing expiry', 'الخطة الحالية': 'Current plan',
    'البرنامج الحالي': 'Current program', 'لا يوجد برنامج حالي': 'No current program',
    'ملخص التقدم': 'Progress summary', 'سجل التمارين': 'Workout log', 'سجل المتابعات': 'Check-in history',
    'المعلومات الصحية': 'Health information', 'ملفي الشخصي': 'My profile', 'لوحتي': 'My dashboard',
    'ناديي': 'My club', 'المحادثات': 'Conversations', 'الإشعارات': 'Notifications',
    'الاشتراكات والمدفوعات': 'Subscriptions and payments', 'مكتبة التمارين': 'Exercise library',
    'الخطط الغذائية': 'Meal plans', 'إدارة الاشتراكات': 'Subscription management',
    'إدارة المدفوعات': 'Payment management', 'فريق التدريب': 'Coaching team'
}

Object.assign(englishUi, {
  'غير متاح': 'Not available', 'لا توجد تفاصيل': 'No details available', 'Check-ins للمراجعة:': 'Check-ins to review:', '· دفعات للتأكيد:': '· Payments to confirm:', '· رسائل غير مقروءة:': '· Unread messages:',
  'تحديث الخطة': 'Update plan', 'إضافة مدرب': 'Add coach', 'كلمة مرور مؤقتة': 'Temporary password', 'إضافة': 'Add', 'بيانات المدرب': 'Coach details', 'البريد الجديد': 'New email', 'تفعيل': 'Activate', 'إيقاف': 'Deactivate', 'العملاء المخصصون': 'Assigned clients',
  'Check-ins وتعليق المدرب': 'Check-ins and coach feedback', 'التعليق': 'Feedback', 'الالتزام والتمارين': 'Adherence and workouts', 'تحميل الملاحظات': 'Load notes', 'ملاحظة': 'Note', 'إضافة ملاحظة': 'Add note', 'تأكيد': 'Confirm', 'رفض': 'Reject', 'السبب': 'Reason',
  'صورة الحساب': 'Profile photo', 'تم اختيار صورة الحساب.': 'Profile photo selected.', 'حفظ الحساب': 'Save account', 'تسجيل الخروج من كل الأجهزة': 'Sign out of all devices', 'ملفات الحساب': 'Account files', 'تنزيل صورة الحساب': 'Download profile photo', 'حذف صورة الحساب': 'Delete profile photo',
  'الملف يضم بيانات حسابك فقط، ورابط التنزيل صالح للاستخدام مرة واحدة وينتهي بعد ساعة. يمكن طلب تصدير جديد كل 24 ساعة.': 'The export contains only your account data. Its download link works once, expires in one hour, and a new export can be requested every 24 hours.',
  'طلب تصدير البيانات': 'Request data export', 'الحالة:': 'Status:', 'تنزيل الملف الجاهز': 'Download completed file', 'سجلات Check-in': 'Check-in records', 'تسجيل وجبة من خطتي': 'Log a meal from my plan', 'تسجيل الالتزام': 'Log meal adherence',
  'سجل الاشتراكات': 'Subscription history', 'طلب تجديد الاشتراك': 'Request subscription renewal', 'سبب الإلغاء': 'Cancellation reason', 'إلغاء الاشتراك': 'Cancel subscription', 'تحديث الحالة': 'Refresh status', 'حالة الدفع': 'Payment status',
  'سيتم تحويلك إلى صفحة Paymob الآمنة. تأكيد الدفع يعتمد على إشعار الخادم الموثق.': 'You will be redirected to Paymob’s secure checkout. Payment is confirmed by the verified server notification.',
  'لا توجد محاولة دفع محفوظة لهذه الجلسة. افتح صفحة الاشتراكات لبدء الدفع.': 'No payment attempt is saved for this session. Open subscriptions to start a payment.', 'الاشتراك:': 'Subscription:', 'حفظ التعديلات': 'Save changes', 'إعادة التوليد بملاحظات المدرب': 'Regenerate with coach notes',
  'اعتماد المسودة': 'Approve draft', 'رفض المسودة': 'Reject draft', 'إنشاء ملخص': 'Generate summary', 'سجل مخاطر الانسحاب': 'Churn risk history', 'تعديل بيانات العميل': 'Edit client details', 'حذف ملاحظة': 'Delete note', 'حذف الملاحظة': 'Delete note',
  'إجراءات الإدارة': 'Management actions', 'إدارة مساحة تدريب': 'Manage training club', 'اختر مساحة من القائمة أو استخدم زر «إدارة المساحة» في جدول المساحات.': 'Choose a club or select Manage club from the clubs table.',
  'إجراءات حذف المساحة': 'Club deletion actions', 'سيتم جدولة الحذف بعد فترة السماح. اكتب رابط المساحة كما هو لتأكيد العملية.': 'Deletion will be scheduled after the grace period. Enter the club link exactly to confirm.', 'جدولة الحذف بعد فترة السماح': 'Schedule deletion after the grace period',
  'لا توجد سجلات بانتظار المراجعة.': 'No records are waiting for review.', 'تعليق المدرب': 'Coach feedback', 'سجل': 'Record', 'ملخص المدفوعات': 'Payment summary', 'من': 'From', 'إلى': 'To', 'تفاصيل دفعة': 'Payment details',
  'معلومات صحية': 'Health information', 'حفظ التهيئة': 'Save onboarding', 'الملف': 'File', 'التحديث': 'Update', 'حفظ المعلومات الصحية': 'Save health information', 'صور (حتى 4)': 'Photos (up to 4)', 'إرسال Check-in': 'Submit check-in',
  'رقم المجموعة': 'Set number', 'وقت الأداء': 'Performed at', 'تسجيل المجموعة': 'Log set', 'تحديث السجل': 'Update log', 'هذه الهوية تظهر للعملاء في صفحة المساحة.': 'Clients see this branding on the club page.', 'اسم النادي': 'Club name', 'رابط الشعار': 'Logo URL', 'اللون الأساسي': 'Primary color', 'اللون الثانوي': 'Secondary color', 'حفظ الهوية': 'Save branding',
  'توليد برنامج': 'Generate program', 'الهدف': 'Goal', 'زيادة القوة': 'Build strength', 'خسارة الوزن': 'Weight loss', 'زيادة العضلات': 'Build muscle', 'لياقة عامة': 'General fitness', 'إعادة التأهيل': 'Rehabilitation', 'المستوى': 'Level', 'مبتدئ': 'Beginner', 'متوسط': 'Intermediate', 'متقدم': 'Advanced', 'توليد البرنامج': 'Generate program',
  'تم إنشاء مسودة البرنامج.': 'Program draft created.', 'اعتماد': 'Approve', 'الحصة والمسودات': 'Quota and drafts', 'عرض درجة المخاطر': 'View risk score', 'إعادة الاحتساب': 'Recompute', 'لا توجد بيانات حالياً.': 'No data available.',
  'مركز التحكم': 'Control center', 'بصورة أوضح': 'At a glance', 'تابع صحة المساحات، راجع النشاط الأخير، وانتقل مباشرةً إلى الإجراءات التي تحتاجها.': 'Monitor club health, review recent activity, and open the actions you need.',
  'استعراض المساحات': 'Browse clubs', 'منصة التدريب والإدارة': 'Training and club management', 'أرقام مجمّعة لمتابعة نمو المساحات واستخدام المنصة.': 'Aggregated metrics for club growth and platform usage.', 'ملخص المنصة': 'Platform summary', 'لا توجد مؤشرات حالياً.': 'No metrics available.',
  'ابحث بالاسم أو الرابط، ثم افتح المساحة لإدارة حالتها واستخدامها.': 'Search by name or link, then open a club to manage its status and usage.', 'نتيجة': 'Result', 'إدارة المساحة': 'Manage club', 'استخدام الذكاء الاصطناعي': 'AI usage', 'الطلبات المسجلة لهذا اليوم': 'Requests recorded today',
  'أحدث عمليات استقبال ومعالجة الدفع': 'Latest payment notifications received and processed', 'مراقبة النشاط': 'Activity monitoring', 'سجل التدقيق الأخير': 'Recent audit log', 'أحداث إدارية وأمنية على مستوى المنصة.': 'Platform security and administrative events.',
  'استكشف مساحة تدريب': 'Explore a training club', 'عرض الصفحة والباقات': 'View club and plans', 'التحقق من توفر الرابط': 'Check link availability', 'الباقات النشطة': 'Active plans', 'دعوة CoachPulse': 'CoachPulse invitation'
});

Object.assign(englishUi, {
  'منصة التدريب وإدارة العملاء': 'Training and client management', 'تدريب أفضل.': 'Better coaching.', 'متابعة أوضح.': 'Clearer progress.',
  'كل ما تحتاجه لإدارة التمارين والعملاء والتقدم في مساحة عمل واحدة.': 'Manage training, clients, and progress in one place.',
  'برامج وتمارين منظمة': 'Organized programs and exercises', 'متابعة التقدم والتواصل': 'Progress tracking and communication', 'تجربة مناسبة للمدرب والعميل': 'Designed for coaches and clients',
  'مساحتك التدريبية تبدأ من هنا': 'Your club starts here.', 'مرحباً بعودتك': 'Welcome back', 'اختر مساحة التدريب المرتبطة بحسابك، ثم أدخل بيانات الدخول.': 'Choose your club, then enter your sign-in details.',
  'مساحة التدريب': 'Club', 'جارٍ تحميل الأندية': 'Loading clubs...', 'تعذر تحميل الأندية الآن.': 'Could not load clubs right now.', 'رمز النادي من الدعوة': 'Club code from your invitation',
  'ابدأ بكتابة أول حرفين من اسم النادي للبحث.': 'Type the first two letters of the club name to search.', 'للدخول بحساب SuperAdmin': 'For SuperAdmin sign-in',
  'لا توجد نتائج. جرّب اسماً آخر.': 'No results. Try another name.', 'لا تجد ناديك؟ افتح رابط الدعوة الذي وصلك.': 'Can’t find your club? Open the invitation link you received.',
  'نسيت كلمة المرور؟': 'Forgot password?', 'مسؤول المنصة؟ اختر «مسؤول المنصة» من قائمة المساحة واترك النادي دون اختيار.': 'SuperAdmin? Choose Platform administrator and leave the club unselected.',
  'أول مرة هنا؟': 'First time here?', 'أنشئ مساحة تدريب': 'Create a club', 'للمدربين الراغبين في بدء مساحة جديدة': 'For coaches starting a new club',
  'انضم كعميل': 'Join as a client', 'لديك دعوة من مدرب؟ أكمل التسجيل هنا': 'Have an invitation from a coach? Finish registration here.',
  'لديك دعوة للانضمام كمدرب؟': 'Have a staff invitation?', 'خيارات الحساب الأخرى': 'Other account options', 'إعادة تعيين كلمة المرور برمز': 'Reset password with a code',
  'بيانات حسابك ومساحة التدريب محمية بصلاحيات الوصول الخاصة بك.': 'Your account and club data are protected by your access permissions.',
  'سجّل الدخول الآن بكلمة المرور الجديدة.': 'Sign in with your new password.', 'يلزم تعيين كلمة مرور جديدة قبل استخدام لوحة التحكم.': 'Set a new password before using the dashboard.',
  'تأكيد كلمة المرور الجديدة': 'Confirm new password', 'الرمز': 'Code', 'تفاصيل البرنامج': 'Program details', 'تمارين البرنامج': 'Program exercises',
  'عنوان جديد': 'New title', 'راحة بالثواني': 'Rest seconds', 'حفظ تعديل التمرين': 'Save exercise changes', 'ترتيب تمارين اليوم': 'Reorder today’s exercises',
  'حفظ الترتيب': 'Save order', 'نسخ': 'Duplicate', 'حفظ كقالب': 'Save as template', 'القوالب': 'Templates', 'إنشاء برنامج من القالب': 'Create program from template',
  'المجموعات والأدوات': 'Muscle groups and equipment', 'تمرين جديد': 'New exercise', 'تفاصيل أو تعديل': 'Edit details', 'تفاصيل الخطة': 'Plan details',
  'عنوان الخطة': 'Plan title', 'اسم الوجبة': 'Meal name', 'السعرات': 'Calories', 'إضافة وجبة': 'Add meal', 'تحديث الوجبة': 'Update meal', 'حذف الوجبة': 'Delete meal',
  'اسم الباقة': 'Plan name', 'إدارة باقة': 'Manage plan', 'أرشفة الباقة': 'Archive plan', 'دعوة عميل': 'Invite a client', 'إرسال دعوة': 'Send invitation',
  'رمز الدعوة للتطوير:': 'Development invitation token:', 'فتح تسجيل العميل': 'Open client registration', 'الدعوات': 'Invitations', 'المدربون في المساحة': 'Coaches in this club',
  'إعادة الإرسال': 'Resend invitation', 'إلغاء الدعوة': 'Cancel invitation', 'تمارين المنصة العامة': 'Public platform exercises',
  'اختر تمريناً لتعديله أو حذفه. الحذف قد يُرفض إذا كان التمرين مستخدماً.': 'Choose an exercise to edit or delete. Deletion may be rejected if it is in use.',
  'إيقاف مؤقت': 'Paused', 'عرض البيانات الصحية (مسجّل تدقيقياً)': 'View health information (audited)', 'حفظ المدرب المسؤول': 'Save assigned coach',
  'تكرار Check-in': 'Check-in frequency', 'يوم الأسبوع': 'Day of week', 'حفظ الموعد': 'Save schedule', 'الدفعات المعلّقة': 'Pending payments',
  'تسجيل دفعة كاش': 'Record cash payment', 'تسجيل الدفعة': 'Record payment', 'استرداد دفعة': 'Refund payment', 'تنفيذ الاسترداد': 'Process refund',
  'تصدير CSV': 'Export CSV', 'حذف الحساب وإخفاء بياناته': 'Delete account and hide its data', 'تصدير بياناتي': 'Export my data',
  'الباقات': 'Plans', 'اشتراكي الحالي': 'Current subscription', 'إنشاء طلب اشتراك': 'Request subscription', 'مدفوعاتي': 'My payments',
  'تفاصيل الدفع وحالته': 'Payment details and status', 'تنزيل الفاتورة PDF': 'Download PDF invoice', 'الدفع بالبطاقة عبر Paymob': 'Pay by card through Paymob',
  'لا توجد خطة وجبات نشطة': 'No active meal plan', 'حصة الاستخدام': 'Usage quota', 'إدارة العملاء المؤسسيين': 'Club management',
  'إدارة المساحة': 'Manage club', 'اختر المساحة ثم نفّذ الإجراء المناسب. تظهر تفاصيلها واستخدامها هنا.': 'Choose a club to manage its status and usage.',
  'اختر مساحة التدريب': 'Choose a club', 'لا توجد مساحات لعرضها.': 'No clubs to display.', 'استخدام الذكاء الاصطناعي': 'AI usage',
  'مراقبة النشاط': 'Activity monitoring', 'سجل التدقيق الأخير': 'Recent audit log', 'أحداث إدارية وأمنية على مستوى المنصة.': 'Platform security and administrative events.',
  'استكشف مساحة تدريب': 'Explore a training club', 'عرض الصفحة والباقات': 'View club and plans', 'التحقق من توفر الرابط': 'Check link availability',
  'الباقات النشطة': 'Active plans', 'دعوة CoachPulse': 'CoachPulse invitation'
});
Object.assign(englishUi, {
  'حذف': 'Delete', 'استيراد CSV': 'Import CSV', 'تفاصيل العميل': 'Client details', 'بيانات الدعوة العامة (البريد مقنّع):': 'Invitation details (email partially hidden):', 'إكمال التسجيل': 'Complete registration'
});
Object.assign(arabic, {
  conversations: '\u0645\u062d\u0627\u062f\u062b\u0627\u062a', conversation: '\u0645\u062d\u0627\u062f\u062b\u0629',
  'Your conversations': '\u0645\u062d\u0627\u062f\u062b\u0627\u062a\u0643',
  'Follow your client conversations and reply from one place.': '\u062a\u0627\u0628\u0639 \u0645\u062d\u0627\u062f\u062b\u0627\u062a \u0639\u0645\u0644\u0627\u0626\u0643 \u0648\u0631\u062f \u0639\u0644\u064a\u0647\u0645 \u0645\u0646 \u0645\u0643\u0627\u0646 \u0648\u0627\u062d\u062f.',
  'Messages and client updates will appear here.': '\u0633\u062a\u0638\u0647\u0631 \u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u0648\u062a\u062d\u062f\u064a\u062b\u0627\u062a \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0647\u0646\u0627.',
  'A quick look at your training and account.': '\u0646\u0638\u0631\u0629 \u0633\u0631\u064a\u0639\u0629 \u0639\u0644\u0649 \u062a\u062f\u0631\u064a\u0628\u0643 \u0648\u062d\u0633\u0627\u0628\u0643.',
  'Your next workout starts here': '\u062a\u0645\u0631\u064a\u0646\u0643 \u0627\u0644\u0642\u0627\u062f\u0645 \u064a\u0628\u062f\u0623 \u0645\u0646 \u0647\u0646\u0627',
  'Keep your plan close, stay in touch with your coach, and celebrate each step forward.': '\u062a\u0627\u0628\u0639 \u062e\u0637\u062a\u0643\u060c \u0648\u062a\u0648\u0627\u0635\u0644 \u0645\u0639 \u0645\u062f\u0631\u0628\u0643\u060c \u0648\u0627\u062d\u062a\u0641\u0644 \u0628\u0643\u0644 \u062e\u0637\u0648\u0629.',
  "Today's training": '\u062a\u0645\u0631\u064a\u0646 \u0627\u0644\u064a\u0648\u0645', Subscription: '\u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643',
  'Unread messages': '\u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u063a\u064a\u0631 \u0627\u0644\u0645\u0642\u0631\u0648\u0621\u0629',
  'Next check-in': '\u0627\u0644\u0645\u062a\u0627\u0628\u0639\u0629 \u0627\u0644\u0642\u0627\u062f\u0645\u0629',
  'No program': '\u0644\u0627 \u062a\u0648\u062c\u062f \u062e\u0637\u0629 \u062a\u062f\u0631\u064a\u0628', 'Not active': '\u063a\u064a\u0631 \u0646\u0634\u0637',
  'Not scheduled': '\u063a\u064a\u0631 \u0645\u062d\u062f\u062f\u0629', 'Rest day': '\u064a\u0648\u0645 \u0631\u0627\u062d\u0629',
  exercises: '\u062a\u0645\u0627\u0631\u064a\u0646', 'Online now': '\u0645\u062a\u0635\u0644 \u0627\u0644\u0622\u0646', Offline: '\u063a\u064a\u0631 \u0645\u062a\u0635\u0644',
  'Type a client name': '\u0627\u0628\u062d\u062b \u0628\u0627\u0633\u0645 \u0627\u0644\u0639\u0645\u064a\u0644',
  'Unread only': '\u063a\u064a\u0631 \u0627\u0644\u0645\u0642\u0631\u0648\u0621\u0629 \u0641\u0642\u0637',
  'Show all conversations': '\u0639\u0631\u0636 \u0643\u0644 \u0627\u0644\u0645\u062d\u0627\u062f\u062b\u0627\u062a',
  'Select a conversation': '\u0627\u062e\u062a\u0631 \u0645\u062d\u0627\u062f\u062b\u0629',
  'Search clients': '\u0627\u0628\u062d\u062b \u0639\u0646 \u0639\u0645\u064a\u0644', Inbox: '\u0635\u0646\u062f\u0648\u0642 \u0627\u0644\u0648\u0627\u0631\u062f',
  'Client conversation': '\u0645\u062d\u0627\u062f\u062b\u0629 \u0627\u0644\u0639\u0645\u064a\u0644',
  'Mark as read': '\u062a\u0639\u0644\u064a\u0645 \u0643\u0645\u0642\u0631\u0648\u0621\u0629', Unread: '\u063a\u064a\u0631 \u0645\u0642\u0631\u0648\u0621\u0629'
});

Object.assign(arabic, {
  'Subscriptions & payments': '\u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643\u0627\u062a \u0648\u0627\u0644\u0645\u062f\u0641\u0648\u0639\u0627\u062a',
  'Choose a plan, submit payment, then follow its review status.': '\u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629\u060c \u0623\u0631\u0633\u0644 \u0627\u0644\u062f\u0641\u0639\u0629\u060c \u0648\u062a\u0627\u0628\u0639 \u062d\u0627\u0644\u062a\u0647\u0627 \u062d\u062a\u0649 \u0627\u0644\u062a\u0623\u0643\u064a\u062f.',
  'Choose a plan': '\u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629', 'Compare the active plans offered by your club.': '\u0642\u0627\u0631\u0646 \u0627\u0644\u0628\u0627\u0642\u0627\u062a \u0627\u0644\u0646\u0634\u0637\u0629 \u0627\u0644\u0645\u062a\u0627\u062d\u0629 \u0644\u062f\u064a\u0643.',
  'No active plans are available': '\u0644\u0627 \u062a\u0648\u062c\u062f \u0628\u0627\u0642\u0627\u062a \u0646\u0634\u0637\u0629 \u062d\u0627\u0644\u064a\u0627\u064b',
  'Ask your coach or club owner to publish a plan before you request a subscription.': '\u0627\u0637\u0644\u0628 \u0645\u0646 \u0645\u062f\u0631\u0628\u0643 \u0623\u0648 \u0645\u0627\u0644\u0643 \u0627\u0644\u0646\u0627\u062f\u064a \u0625\u0636\u0627\u0641\u0629 \u0628\u0627\u0642\u0629 \u0642\u0628\u0644 \u0625\u0646\u0634\u0627\u0621 \u0637\u0644\u0628.',
  'Training plan': '\u0628\u0627\u0642\u0629 \u062a\u062f\u0631\u064a\u0628', days: '\u064a\u0648\u0645', 'Continue to payment': '\u0627\u0644\u0645\u062a\u0627\u0628\u0639\u0629 \u0644\u0644\u062f\u0641\u0639',
  'Creating request?': '\u062c\u0627\u0631\u064d \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0637\u0644\u0628...', 'Request already pending': '\u0644\u062f\u064a\u0643 \u0637\u0644\u0628 \u0645\u0639\u0644\u0642',
  'You already have a pending request for this plan. Check payment history below or choose another plan.': '\u0644\u062f\u064a\u0643 \u0637\u0644\u0628 \u0645\u0639\u0644\u0642 \u0644\u0647\u0630\u0647 \u0627\u0644\u0628\u0627\u0642\u0629. \u0631\u0627\u062c\u0639 \u0633\u062c\u0644 \u0627\u0644\u062f\u0641\u0639\u0627\u062a \u0623\u0648 \u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629 \u0623\u062e\u0631\u0649.',
  'Your request is ready. Choose a payment method below.': '\u062c\u0627\u0647\u0632\u064a\u0646 \u0637\u0644\u0628 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643. \u0627\u062e\u062a\u0631\u064a \u0637\u0631\u064a\u0642\u0629 \u0627\u0644\u062f\u0641\u0639 \u0623\u062f\u0646\u0627\u0647.',
  'Could not start the subscription.': '\u062a\u0639\u0630\u0631 \u0625\u0646\u0634\u0627\u0621 \u0637\u0644\u0628 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643.',
  'Current subscription': '\u0627\u0634\u062a\u0631\u0627\u0643\u064a \u0627\u0644\u062d\u0627\u0644\u064a', 'Your plan becomes active after the payment is confirmed.': '\u064a\u062a\u0641\u0639\u0644 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643 \u0628\u0639\u062f \u062a\u0623\u0643\u064a\u062f \u0627\u0644\u062f\u0641\u0639.',
  'No active subscription': '\u0644\u0627 \u064a\u0648\u062c\u062f \u0627\u0634\u062a\u0631\u0627\u0643 \u0646\u0634\u0637', 'Choose a plan above to get started. A request remains pending until your payment is confirmed.': '\u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629 \u0644\u0644\u0628\u062f\u0621. \u064a\u0638\u0644 \u0627\u0644\u0637\u0644\u0628 \u0645\u0639\u0644\u0642\u0627\u064b \u062d\u062a\u0649 \u062a\u0623\u0643\u064a\u062f \u0627\u0644\u062f\u0641\u0639.',
  Status: '\u0627\u0644\u062d\u0627\u0644\u0629', Started: '\u0628\u062f\u0623 \u0641\u064a', 'Renews or ends': '\u0627\u0644\u062a\u062c\u062f\u064a\u062f \u0623\u0648 \u0627\u0644\u0627\u0646\u062a\u0647\u0627\u0621',
  'Choose how to pay': '\u0627\u062e\u062a\u0631 \u0637\u0631\u064a\u0642\u0629 \u0627\u0644\u062f\u0641\u0639', 'Pay online with Paymob': '\u0627\u062f\u0641\u0639 \u0625\u0644\u0643\u062a\u0631\u0648\u0646\u064a\u0627\u064b \u0639\u0628\u0631 Paymob',
  'Continue to the secure checkout. Your subscription activates after the server confirms payment.': '\u0623\u0643\u0645\u0644 \u0627\u0644\u062f\u0641\u0639 \u0639\u0628\u0631 \u0627\u0644\u0635\u0641\u062d\u0629 \u0627\u0644\u0622\u0645\u0646\u0629. \u064a\u062a\u0641\u0639\u0644 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643 \u0628\u0639\u062f \u062a\u0623\u0643\u064a\u062f \u0627\u0644\u062e\u0627\u062f\u0645.',
  'Pay by InstaPay': '\u0627\u062f\u0641\u0639 \u0639\u0628\u0631 InstaPay', 'Transfer the exact plan amount, then upload the receipt for club review.': '\u062d\u0648\u0651\u0644 \u0642\u064a\u0645\u0629 \u0627\u0644\u0628\u0627\u0642\u0629 \u0643\u0627\u0645\u0644\u0629\u060c \u062b\u0645 \u0627\u0631\u0641\u0639 \u0627\u0644\u0625\u064a\u0635\u0627\u0644 \u0644\u0645\u0631\u0627\u062c\u0639\u0629 \u0627\u0644\u0646\u0627\u062f\u064a.',
  'InstaPay is not enabled by this club.': '\u062e\u062f\u0645\u0629 InstaPay \u063a\u064a\u0631 \u0645\u0641\u0639\u0644\u0629 \u0644\u062f\u0649 \u0627\u0644\u0646\u0627\u062f\u064a.',
  'Transfer reference': '\u0631\u0642\u0645 \u0645\u0631\u062c\u0639 \u0627\u0644\u062a\u062d\u0648\u064a\u0644', Amount: '\u0627\u0644\u0645\u0628\u0644\u063a', Receipt: '\u0625\u064a\u0635\u0627\u0644 \u0627\u0644\u062a\u062d\u0648\u064a\u0644',
  'Upload a screenshot or PDF of your transfer.': '\u0627\u0631\u0641\u0639 \u0635\u0648\u0631\u0629 \u0623\u0648 \u0645\u0644\u0641 PDF \u0644\u0644\u062a\u062d\u0648\u064a\u0644.', 'Receipt uploaded.': '\u062a\u0645 \u0631\u0641\u0639 \u0627\u0644\u0625\u064a\u0635\u0627\u0644.', 'Could not upload the receipt.': '\u062a\u0639\u0630\u0631 \u0631\u0641\u0639 \u0627\u0644\u0625\u064a\u0635\u0627\u0644.',
  'Submitting?': '\u062c\u0627\u0631\u064d \u0625\u0631\u0633\u0627\u0644 \u0627\u0644\u062a\u062d\u0648\u064a\u0644...', 'Submit transfer for review': '\u0623\u0631\u0633\u0644 \u0627\u0644\u062a\u062d\u0648\u064a\u0644 \u0644\u0644\u0645\u0631\u0627\u062c\u0639\u0629',
  'Transfer submitted. The club will review it before activating your subscription.': '\u062a\u0645 \u0625\u0631\u0633\u0627\u0644 \u0627\u0644\u062a\u062d\u0648\u064a\u0644. \u0633\u064a\u0631\u0627\u062c\u0639\u0647 \u0627\u0644\u0646\u0627\u062f\u064a \u0642\u0628\u0644 \u062a\u0641\u0639\u064a\u0644 \u0627\u0634\u062a\u0631\u0627\u0643\u0643.',
  'Could not submit the transfer.': '\u062a\u0639\u0630\u0631 \u0625\u0631\u0633\u0627\u0644 \u0627\u0644\u062a\u062d\u0648\u064a\u0644.', 'Payment history': '\u0633\u062c\u0644 \u0627\u0644\u062f\u0641\u0639\u0627\u062a',
  'See payment status and download invoices for confirmed payments.': '\u062a\u0627\u0628\u0639 \u062d\u0627\u0644\u0629 \u0627\u0644\u062f\u0641\u0639\u0627\u062a \u0648\u062d\u0645\u0651\u0644 \u0641\u0648\u0627\u062a\u064a\u0631 \u0627\u0644\u062f\u0641\u0639\u0627\u062a \u0627\u0644\u0645\u0624\u0643\u062f\u0629.',
  'No payments yet. Payments you submit will appear here.': '\u0644\u0627 \u062a\u0648\u062c\u062f \u062f\u0641\u0639\u0627\u062a \u0628\u0639\u062f. \u0633\u062a\u0638\u0647\u0631 \u0627\u0644\u062f\u0641\u0639\u0627\u062a \u0627\u0644\u062a\u064a \u062a\u0631\u0633\u0644\u0647\u0627 \u0647\u0646\u0627.',
  'Subscription payment': '\u062f\u0641\u0639\u0629 \u0627\u0634\u062a\u0631\u0627\u0643', 'Subscription history & changes': '\u0633\u062c\u0644 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643 \u0648\u0625\u062f\u0627\u0631\u062a\u0647',
  'Review past requests, request a renewal, or cancel an active subscription.': '\u0631\u0627\u062c\u0639 \u0637\u0644\u0628\u0627\u062a\u0643\u060c \u0627\u0637\u0644\u0628 \u062a\u062c\u062f\u064a\u062f\u0627\u064b\u060c \u0623\u0648 \u0623\u0644\u063a\u0650 \u0627\u0634\u062a\u0631\u0627\u0643\u0627\u064b \u0646\u0634\u0637\u0627\u064b.',
  'No subscription requests yet. Choose a plan above to begin.': '\u0644\u0627 \u062a\u0648\u062c\u062f \u0637\u0644\u0628\u0627\u062a \u0627\u0634\u062a\u0631\u0627\u0643 \u0628\u0639\u062f. \u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629 \u0644\u0644\u0628\u062f\u0621.',
  'Request submitted': '\u062a\u0627\u0631\u064a\u062e \u0627\u0644\u0637\u0644\u0628', 'Request renewal': '\u0637\u0644\u0628 \u062a\u062c\u062f\u064a\u062f', 'Choose a plan to renew or change your subscription': '\u0627\u062e\u062a\u0631 \u0628\u0627\u0642\u0629 \u0644\u0644\u062a\u062c\u062f\u064a\u062f \u0623\u0648 \u062a\u063a\u064a\u064a\u0631 \u0627\u0634\u062a\u0631\u0627\u0643\u0643',
  'Renewal request created. Complete payment to extend your access.': '\u062a\u0645 \u0625\u0646\u0634\u0627\u0621 \u0637\u0644\u0628 \u0627\u0644\u062a\u062c\u062f\u064a\u062f. \u0623\u0643\u0645\u0644 \u0627\u0644\u062f\u0641\u0639 \u0644\u062a\u0645\u062f\u064a\u062f \u0627\u0644\u0648\u0635\u0648\u0644.',
  'Could not request renewal.': '\u062a\u0639\u0630\u0631 \u0637\u0644\u0628 \u0627\u0644\u062a\u062c\u062f\u064a\u062f.', 'Why are you cancelling?': '\u0645\u0627 \u0633\u0628\u0628 \u0627\u0644\u0625\u0644\u063a\u0627\u0621\u061f',
  'Confirm cancellation': '\u062a\u0623\u0643\u064a\u062f \u0627\u0644\u0625\u0644\u063a\u0627\u0621', 'Keep subscription': '\u0627\u0644\u0627\u062d\u062a\u0641\u0627\u0638 \u0628\u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643',
  'Subscription cancelled.': '\u062a\u0645 \u0625\u0644\u063a\u0627\u0621 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643.', 'Could not cancel subscription.': '\u062a\u0639\u0630\u0631 \u0625\u0644\u063a\u0627\u0621 \u0627\u0644\u0627\u0634\u062a\u0631\u0627\u0643.',
  'Cancelling stops renewal; your current access remains subject to the club?s subscription policy.': '\u064a\u0648\u0642\u0641 \u0627\u0644\u0625\u0644\u063a\u0627\u0621 \u0627\u0644\u062a\u062c\u062f\u064a\u062f\u060c \u0648\u064a\u0638\u0644 \u0627\u0644\u0648\u0635\u0648\u0644 \u0627\u0644\u062d\u0627\u0644\u064a \u062e\u0627\u0636\u0639\u0627\u064b \u0644\u0633\u064a\u0627\u0633\u0629 \u0627\u0644\u0646\u0627\u062f\u064a.',
  'Payment details': '\u062a\u0641\u0627\u0635\u064a\u0644 \u0627\u0644\u062f\u0641\u0639\u0629', 'Select a payment to see its verified status and invoice options.': '\u0627\u062e\u062a\u0631 \u062f\u0641\u0639\u0629 \u0644\u0639\u0631\u0636 \u062d\u0627\u0644\u062a\u0647\u0627 \u0627\u0644\u0645\u0624\u0643\u062f\u0629 \u0648\u062e\u064a\u0627\u0631\u0627\u062a \u0627\u0644\u0641\u0627\u062a\u0648\u0631\u0629.',
  'There are no payments to review yet.': '\u0644\u0627 \u062a\u0648\u062c\u062f \u062f\u0641\u0639\u0627\u062a \u0644\u0639\u0631\u0636\u0647\u0627 \u062d\u0627\u0644\u064a\u0627\u064b.', 'Verified payment status': '\u062d\u0627\u0644\u0629 \u0627\u0644\u062f\u0641\u0639 \u0627\u0644\u0645\u0624\u0643\u062f\u0629',
  'This status comes from the server; a return-page message alone does not confirm payment.': '\u0645\u0635\u062f\u0631 \u0627\u0644\u062d\u0627\u0644\u0629 \u0647\u0648 \u0627\u0644\u062e\u0627\u062f\u0645. \u0639\u0648\u062f\u0629 \u0635\u0641\u062d\u0629 \u0627\u0644\u062f\u0641\u0639 \u0644\u0627 \u062a\u0639\u0646\u064a \u062a\u0623\u0643\u064a\u062f\u0647.',
  'Refresh status': '\u062a\u062d\u062f\u064a\u062b \u0627\u0644\u062d\u0627\u0644\u0629', 'Download invoice (PDF)': '\u062a\u0646\u0632\u064a\u0644 \u0627\u0644\u0641\u0627\u062a\u0648\u0631\u0629 (PDF)', 'Invoice is not available yet.': '\u0627\u0644\u0641\u0627\u062a\u0648\u0631\u0629 \u063a\u064a\u0631 \u0645\u062a\u0627\u062d\u0629 \u062d\u062a\u0649 \u062a\u0623\u0643\u064a\u062f \u0627\u0644\u062f\u0641\u0639.',
  'The payment provider returned an invalid checkout link.': '\u0631\u0627\u0628\u0637 \u0627\u0644\u062f\u0641\u0639 \u0627\u0644\u0645\u0633\u062a\u0644\u0645 \u063a\u064a\u0631 \u0635\u0627\u0644\u062d.', 'Could not start Paymob checkout.': '\u062a\u0639\u0630\u0631 \u0628\u062f\u0621 \u0627\u0644\u062f\u0641\u0639 \u0639\u0628\u0631 Paymob.',
  'Opening secure checkout?': '\u062c\u0627\u0631\u064d \u0641\u062a\u062d \u0635\u0641\u062d\u0629 \u0627\u0644\u062f\u0641\u0639 \u0627\u0644\u0622\u0645\u0646\u0629...', 'Continue to Paymob': '\u0627\u0644\u0645\u062a\u0627\u0628\u0639\u0629 \u0625\u0644\u0649 Paymob'
});

// Keep complete translations for the English-first sign-in and AI review flows.
// These are intentionally plain text keys because the global language switch
// translates rendered text nodes as well as React labels.
Object.assign(arabic, {
  'TRAINING AND CLIENT MANAGEMENT': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0648\u0627\u0644\u0639\u0645\u0644\u0627\u0621',
  'Manage training plans, client progress and communication in one place.': '\u0623\u062F\u0631 \u062E\u0637\u0637 \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0648\u062A\u0642\u062F\u0645 \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0648\u0627\u0644\u062A\u0648\u0627\u0635\u0644 \u0645\u0646 \u0645\u0643\u0627\u0646 \u0648\u0627\u062D\u062F.',
  'Coaching plans and exercise tracking': '\u062E\u0637\u0637 \u062A\u062F\u0631\u064A\u0628 \u0648\u0645\u062A\u0627\u0628\u0639\u0629 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646',
  'Progress reviews and client messaging': '\u0645\u0631\u0627\u062C\u0639\u0629 \u0627\u0644\u062A\u0642\u062F\u0645 \u0648\u0627\u0644\u062A\u0631\u0627\u0633\u0644 \u0645\u0639 \u0627\u0644\u0639\u0645\u0644\u0627\u0621',
  'Clear tools for coaches and club owners': '\u0623\u062F\u0648\u0627\u062A \u0648\u0627\u0636\u062D\u0629 \u0644\u0644\u0645\u062F\u0631\u0628\u064A\u0646 \u0648\u0645\u0627\u0644\u0643\u064A \u0627\u0644\u0646\u0648\u0627\u062F\u064A',
  'Your coaching club starts here.': '\u0628\u062F\u0627\u064A\u062A\u0643 \u0641\u064A \u0631\u062D\u0644\u0629 \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0645\u0646 \u0647\u0646\u0627.',
  'Choose the club linked to your account, then enter your sign-in details.': '\u0627\u062E\u062A\u0631 \u0627\u0644\u0646\u0627\u062F\u064A \u0627\u0644\u0645\u0631\u062A\u0628\u0637 \u0628\u062D\u0633\u0627\u0628\u0643\u060C \u062B\u0645 \u0623\u062F\u062E\u0644 \u0628\u064A\u0627\u0646\u0627\u062A \u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u062F\u062E\u0648\u0644.',
  'Select a club': '\u0627\u062E\u062A\u0631 \u0627\u0644\u0646\u0627\u062F\u064A',
  'Select your club': '\u0627\u062E\u062A\u0631 \u0646\u0627\u062F\u064A\u0643',
  'Choose the club you use to sign in': '\u0627\u062E\u062A\u0631 \u0627\u0644\u0646\u0627\u062F\u064A \u0627\u0644\u0630\u064A \u062A\u0633\u062C\u0644 \u0627\u0644\u062F\u062E\u0648\u0644 \u0645\u0646 \u062E\u0644\u0627\u0644\u0647',
  'Email address': '\u0627\u0644\u0628\u0631\u064A\u062F \u0627\u0644\u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A',
  'Password': '\u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631',
  'Forgot password?': '\u0646\u0633\u064A\u062A \u0643\u0644\u0645\u0629 \u0627\u0644\u0645\u0631\u0648\u0631\u061F',
  'New to CoachPulse?': '\u0647\u0644 \u0623\u0646\u062A \u062C\u062F\u064A\u062F \u0641\u064A CoachPulse\u061F',
  'Request a new club': '\u0627\u0637\u0644\u0628 \u0625\u0646\u0634\u0627\u0621 \u0646\u0627\u062F\u064D \u062C\u062F\u064A\u062F',
  'For coaches who want to set up a club': '\u0644\u0644\u0645\u062F\u0631\u0628\u064A\u0646 \u0627\u0644\u0631\u0627\u063A\u0628\u064A\u0646 \u0641\u064A \u0625\u0646\u0634\u0627\u0621 \u0646\u0627\u062F\u064D',
  'Join as a client': '\u0627\u0646\u0636\u0645 \u0643\u0639\u0645\u064A\u0644',
  'Complete registration with your coach’s invitation': '\u0623\u0643\u0645\u0644 \u0627\u0644\u062A\u0633\u062C\u064A\u0644 \u0628\u062F\u0639\u0648\u0629 \u0645\u062F\u0631\u0628\u0643',
  'Have a staff invitation? Accept it here.': '\u0644\u062F\u064A\u0643 \u062F\u0639\u0648\u0629 \u0644\u0644\u0627\u0646\u0636\u0645\u0627\u0645 \u0643\u0645\u062F\u0631\u0628\u061F \u0627\u0642\u0628\u0644\u0647\u0627 \u0645\u0646 \u0647\u0646\u0627.',
  'Other account options': '\u062E\u064A\u0627\u0631\u0627\u062A \u0623\u062E\u0631\u0649 \u0644\u0644\u062D\u0633\u0627\u0628',
  'Your account and club information are protected by role-based access controls.': '\u062A\u062D\u0645\u064A \u0636\u0648\u0627\u0628\u0637 \u0627\u0644\u0648\u0635\u0648\u0644 \u062D\u0633\u0627\u0628\u0643 \u0648\u0628\u064A\u0627\u0646\u0627\u062A \u0646\u0627\u062F\u064A\u0643.',
  'Generate a training program': '\u0625\u0646\u0634\u0627\u0621 \u0628\u0631\u0646\u0627\u0645\u062C \u062A\u062F\u0631\u064A\u0628\u064A',
  'Programs are saved as drafts in CoachPulse; no file is downloaded.': '\u062A\u064F\u062D\u0641\u0638 \u0627\u0644\u0628\u0631\u0627\u0645\u062C \u0643\u0645\u0633\u0648\u062F\u0627\u062A \u0641\u064A CoachPulse\u060C \u0648\u0644\u0627 \u064A\u062A\u0645 \u062A\u0646\u0632\u064A\u0644 \u0645\u0644\u0641.',
  'Training goal': '\u0647\u062F\u0641 \u0627\u0644\u062A\u062F\u0631\u064A\u0628', 'Build strength': '\u0628\u0646\u0627\u0621 \u0627\u0644\u0642\u0648\u0629', 'Lose weight': '\u062E\u0633\u0627\u0631\u0629 \u0627\u0644\u0648\u0632\u0646', 'Build muscle': '\u0628\u0646\u0627\u0621 \u0627\u0644\u0639\u0636\u0644\u0627\u062A', 'General fitness': '\u0644\u064A\u0627\u0642\u0629 \u0639\u0627\u0645\u0629', 'Rehabilitation': '\u0625\u0639\u0627\u062F\u0629 \u062A\u0623\u0647\u064A\u0644',
  'Experience level': '\u0645\u0633\u062A\u0648\u0649 \u0627\u0644\u062E\u0628\u0631\u0629', 'Beginner': '\u0645\u0628\u062A\u062F\u0626', 'Intermediate': '\u0645\u062A\u0648\u0633\u0637', 'Advanced': '\u0645\u062A\u0642\u062F\u0645', 'Generate draft': '\u0625\u0646\u0634\u0627\u0621 \u0645\u0633\u0648\u062F\u0629',
  'AI quota and saved drafts': '\u062D\u0635\u0629 \u0627\u0644\u0630\u0643\u0627\u0621 \u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A \u0648\u0627\u0644\u0645\u0633\u0648\u062F\u0627\u062A', 'Drafts stay here until approved or rejected.': '\u062A\u0628\u0642\u0649 \u0627\u0644\u0645\u0633\u0648\u062F\u0627\u062A \u0647\u0646\u0627 \u062D\u062A\u0649 \u0627\u0639\u062A\u0645\u0627\u062F\u0647\u0627 \u0623\u0648 \u0631\u0641\u0636\u0647\u0627.',
  'Daily limit': '\u0627\u0644\u062D\u062F \u0627\u0644\u064A\u0648\u0645\u064A', 'Used today': '\u0627\u0644\u0645\u0633\u062A\u062E\u062F\u0645 \u0627\u0644\u064A\u0648\u0645', 'Remaining': '\u0627\u0644\u0645\u062A\u0628\u0642\u064A', 'Quota resets': '\u062A\u062A\u062C\u062F\u062F \u0627\u0644\u062D\u0635\u0629',
  'Client': '\u0627\u0644\u0639\u0645\u064A\u0644', 'Status': '\u0627\u0644\u062D\u0627\u0644\u0629', 'Prompt version': '\u0646\u0633\u062E\u0629 \u0627\u0644\u062A\u0639\u0644\u064A\u0645\u0627\u062A', 'Model version': '\u0646\u0633\u062E\u0629 \u0627\u0644\u0646\u0645\u0648\u0630\u062C', 'Review': '\u0645\u0631\u0627\u062C\u0639\u0629', 'Review draft': '\u0631\u0627\u062C\u0639 \u0627\u0644\u0645\u0633\u0648\u062F\u0629',
  'Client churn risk': '\u0645\u062E\u0627\u0637\u0631 \u0627\u0646\u0633\u062D\u0627\u0628 \u0627\u0644\u0639\u0645\u064A\u0644', 'Scores are created by the nightly job or a manual recompute by the club owner.': '\u062A\u064F\u062D\u0633\u0628 \u0627\u0644\u062F\u0631\u062C\u0627\u062A \u0644\u064A\u0644\u0627\u064B \u0623\u0648 \u064A\u0639\u064A\u062F \u0645\u0627\u0644\u0643 \u0627\u0644\u0646\u0627\u062F\u064A \u0627\u062D\u062A\u0633\u0627\u0628\u0647\u0627 \u064A\u062F\u0648\u064A\u0627\u064B.',
  'Load latest score': '\u0639\u0631\u0636 \u0622\u062E\u0631 \u062F\u0631\u062C\u0629', 'Recompute scores': '\u0625\u0639\u0627\u062F\u0629 \u0627\u062D\u062A\u0633\u0627\u0628 \u0627\u0644\u062F\u0631\u062C\u0627\u062A', 'Risk level': '\u0645\u0633\u062A\u0648\u0649 \u0627\u0644\u062E\u0637\u0631', 'Risk score': '\u062F\u0631\u062C\u0629 \u0627\u0644\u062E\u0637\u0631', 'Last calculated': '\u0622\u062E\u0631 \u0627\u062D\u062A\u0633\u0627\u0628',
})


const legacyArabicByteMap = new Map<string, number>()
const cp1256Decoder = new TextDecoder('windows-1256')
const cp1252Decoder = new TextDecoder('windows-1252')
for (let byte = 0; byte < 256; byte++) {
  const character = cp1252Decoder.decode(Uint8Array.of(byte))
  if (character.length === 1 && !legacyArabicByteMap.has(character)) legacyArabicByteMap.set(character, byte)
}
// Some Arabic Windows-1256 bytes in the 0x80–0x9F range were previously
// mistaken for Windows-1252 punctuation (for example, ٹ represents byte 0x8A).
for (let byte = 0; byte < 256; byte++) {
  const character = cp1256Decoder.decode(Uint8Array.of(byte))
  if (character.length === 1 && character !== '\uFFFD' && !legacyArabicByteMap.has(character)) legacyArabicByteMap.set(character, byte)
}

export function repairLegacyArabic(text: string) {
  return text
}

function repairBrokenUiArabic(text: string) {
  const repairs: Array<[RegExp, string]> = [
    [/\u062A\u062D\u0645\uFFFD\??\s*\u0644/g, '\u062A\u062D\u0645\u064A\u0644'],
    [/\u0627\u0644\u0628\uFFFD\??\s*\u0627\uFFFD\??\s*\u0627\u062A/g, '\u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A'],
    [/\u0627\u0644\u0622\uFFFD\??/g, '\u0627\u0644\u0622\u0646'],
    [/\u0627\u0644\u062C\u062F\uFFFD\??\s*\u062F\u0629/g, '\u0627\u0644\u062C\u062F\u064A\u062F\u0629'],
    [/\u062A\u0633\u062C\uFFFD\??\s*\u0644/g, '\u062A\u0633\u062C\u064A\u0644'],
    [/\u0627\u0644\u062D\u0627\u0644\uFFFD\??\s*\u0629/g, '\u0627\u0644\u062D\u0627\u0644\u064A\u0629'],
    [/\u062A\u0623\u0643\uFFFD\??\s*\u062F/g, '\u062A\u0623\u0643\u064A\u062F'],
    [/\u0627\u0644\u0628\u0631\uFFFD\??\s*\u062F/g, '\u0627\u0644\u0628\u0631\u064A\u062F'],
    [/\u0627\u0644\u0625\u0644\u0643\u062A\u0631\u0648\uFFFD\??/g, '\u0627\u0644\u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A'],
    [/\u0627\u0644\u062A\uFFFD\??\s*\u0628\uFFFD\??\s*\u0647\u0627\u062A/g, '\u0627\u0644\u062A\u0646\u0628\u064A\u0647\u0627\u062A'],
    [/\u063A\uFFFD\??\s*\u0631/g, '\u063A\u064A\u0631'],
    [/\u062A\u0641\u0627\u0635\uFFFD\??\s*\u0644/g, '\u062A\u0641\u0627\u0635\u064A\u0644'],
    [/\u062A\u0645\u0627\u0631\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u062A\u0645\u0627\u0631\u064A\u0646'],
    [/\u0639\uFFFD\??\s*\u0648\u0627\uFFFD\??/g, '\u0639\u0646\u0648\u0627\u0646'],
    [/\u0628\u0627\u0644\u0623\uFFFD\??\s*\u0627\u0645/g, '\u0628\u0627\u0644\u0623\u064A\u0627\u0645'],
    [/\u0627\u0644\uFFFD\??\s*\u0648\u0645/g, '\u0627\u0644\u064A\u0648\u0645'],
    [/\u0628\u0627\u0644\u062B\u0648\u0627\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u0628\u0627\u0644\u062B\u0648\u0627\u0646\u064A'],
    [/\u062A\u0639\u062F\uFFFD\??\s*\u0644/g, '\u062A\u0639\u062F\u064A\u0644'],
    [/\u0627\u0644\u062A\u0631\u062A\uFFFD\??\s*\u0628/g, '\u0627\u0644\u062A\u0631\u062A\u064A\u0628'],
    [/\u0625\uFFFD\??\s*\u0634\u0627\u0621/g, '\u0625\u0646\u0634\u0627\u0621'],
    [/\u0628\u0631\uFFFD\??\s*\u0627\u0645\u062C/g, '\u0628\u0631\u0646\u0627\u0645\u062C'],
    [/\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\uFFFD\??\s*\u0629/g, '\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\u064A\u0629'],
    [/\u062A\u062D\u062F\uFFFD\??\s*\u062B/g, '\u062A\u062D\u062F\u064A\u062B'],
    [/\u0625\u0633\uFFFD\??\s*\u0627\u062F/g, '\u0625\u0633\u0646\u0627\u062F'],
    [/\u062A\u0641\u0639\uFFFD\??\s*\u0644/g, '\u062A\u0641\u0639\u064A\u0644'],
    [/\u0625\uFFFD\??\s*\u0642\u0627\u0641/g, '\u0625\u064A\u0642\u0627\u0641'],
    [/\u0627\u0644\u0645\u062E\u0635\u0635\u0648\uFFFD\??/g, '\u0627\u0644\u0645\u062E\u0635\u0635\u0648\u0646'],
    [/\u0627\u0644\u0641\uFFFD\??\s*\u062F\uFFFD\??\s*\u0648/g, '\u0627\u0644\u0641\u064A\u062F\u064A\u0648'],
    [/\u0627\u062E\u062A\uFFFD\??\s*\u0627\u0631/g, '\u0627\u062E\u062A\u064A\u0627\u0631'],
    [/\u062A\u063A\uFFFD\??\s*\u064A\u0631/g, '\u062A\u063A\u064A\u064A\u0631'],
    [/\u0627\u0644\u0635\u062D\uFFFD\??\s*\u0629/g, '\u0627\u0644\u0635\u062D\u064A\u0629'],
    [/\u0627\u0644\u0648\u0632\uFFFD\??/g, '\u0627\u0644\u0648\u0632\u0646'],
    [/\u0627\u0644\u0642\uFFFD\??\s*\u0627\u0633\u0627\u062A/g, '\u0627\u0644\u0642\u064A\u0627\u0633\u0627\u062A'],
    [/\u0627\u0644\u062A\u0627\u0631\uFFFD\??\s*\u062E/g, '\u0627\u0644\u062A\u0627\u0631\u064A\u062E'],
    [/\u0627\u0644\u0647\u0648\uFFFD\??\s*\u0629/g, '\u0627\u0644\u0647\u0648\u064A\u0629'],
    [/\u0627\u0644\u0645\u0646\u0635\u0641\u0629/g, '\u0627\u0644\u0645\u0646\u0635\u0629'],
    [/\u0645\u0646\u0635\u0641\u0629/g, '\u0645\u0646\u0635\u0629'],
    [/\u0645\u0624\u0634\u0631\u0627\u062A \u0627\u0644\u0645\uFFFD\??\s*\u0635\u0629/g, '\u0645\u0624\u0634\u0631\u0627\u062A \u0627\u0644\u0645\u0646\u0635\u0629'],
    [/\u062C\u062F\uFFFD\??\s*\u062F/g, '\u062C\u062F\u064A\u062F'],
    [/\u062A\u0645\u0631\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u062A\u0645\u0631\u064A\u0646'],
    [/\u0627\u0644\u062A\u0645\u0631\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u0627\u0644\u062A\u0645\u0631\u064A\u0646'],
    [/\u062A\u0631\u062A\uFFFD\??\s*\u0628/g, '\u062A\u0631\u062A\u064A\u0628'],
    [/\u0625\uFFFD\??\s*\u0634\u0627\u0621/g, '\u0625\u0646\u0634\u0627\u0621'],
    [/\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\uFFFD\??\s*\u0629/g, '\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\u064A\u0629'],
    [/\u0627\u0644\u0645\u062F\u0631\u0628\u0648\uFFFD\??/g, '\u0627\u0644\u0645\u062F\u0631\u0628\u0648\u0646'],
    [/\u0627\u0644\u0645\u062E\u0635\u0635\u0648\uFFFD\??/g, '\u0627\u0644\u0645\u062E\u0635\u0635\u0648\u0646'],
    [/\u0641\uFFFD\??\s*/g, '\u0641\u064A '],
    [/\u0627\u0644\u0641\uFFFD\??\s*\u062F\uFFFD\??\s*\u0648/g, '\u0627\u0644\u0641\u064A\u062F\u064A\u0648'],
    [/\u062A\u0641\u0639\uFFFD\??\s*\u0644/g, '\u062A\u0641\u0639\u064A\u0644'],
    [/\u0625\uFFFD\??\s*\u0642\u0627\u0641/g, '\u0625\u064A\u0642\u0627\u0641'],
    [/\u0627\u0644\u062A\u0637\u0648\uFFFD\??\s*\u0631/g, '\u0627\u0644\u062A\u0637\u0648\u064A\u0631'],
    [/\u0639\u0645\uFFFD\??\s*\u0644/g, '\u0639\u0645\u064A\u0644'],
    [/\u0627\u0644\u062A\u0635\u062D\u064A\uFFFD\??/g, '\u0627\u0644\u062A\u0635\u062D\u064A\u062D'],
    [/\u062A\u0635\u062F\uFFFD\??\s*\u0631/g, '\u062A\u0635\u062F\u064A\u0631'],
    [/\u0627\u0644\u062A\u0646\u0632\uFFFD\??\s*\u0644/g, '\u0627\u0644\u062A\u0646\u0632\u064A\u0644'],
    [/\u0627\u0644\u0645\u0644\u0641 \uFFFD\??\s*\u0636\u0645/g, '\u0627\u0644\u0645\u0644\u0641 \u064A\u0636\u0645'],
    [/\u0645\u0643\uFFFD\??\s*\u0646/g, '\u064A\u0645\u0643\u0646'],
    [/\u0627\u0644\u062A\u0641\u0627\u0635\uFFFD\??\s*\u0644/g, '\u0627\u0644\u062A\u0641\u0627\u0635\u064A\u0644'],
    [/\u0627\u0644\u0642\uFFFD\??\s*\u0627\u0633\u0627\u062A/g, '\u0627\u0644\u0642\u064A\u0627\u0633\u0627\u062A'],
    [/\u0627\u0644\u062E\u0637\u062A\uFFFD\??/g, '\u0627\u0644\u062E\u0637\u062A\u064A'],
    [/\u0627\u0644\u062A\u062F\u0631\uFFFD\??\s*\u0628/g, '\u0627\u0644\u062A\u062F\u0631\u064A\u0628'],
    [/\u0627\u0644\u0645\u0624\u0633\u0633\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u0627\u0644\u0645\u0624\u0633\u0633\u0648\u0646'],
    [/\u0627\u0644\u0627\u0635\u0637\uFFFD\??\s*\u0646\u0627\u0639\uFFFD\??/g, '\u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A'],
    [/\u0639\u0645\u0644\uFFFD\??\s*\u0627\u062A/g, '\u0639\u0645\u0644\u064A\u0627\u062A'],
    [/\u0625\u062F\u0627\u0631\uFFFD\??\s*\u0629/g, '\u0625\u062F\u0627\u0631\u064A\u0629'],
    [/\u0627\u0644\u0623\u062E\uFFFD\??\s*\u0631/g, '\u0627\u0644\u0623\u062E\u064A\u0631'],
    [/\u0627\u0644\u062A\u062D\u0642\u0642 \u0645\uFFFD\??/g, '\u0627\u0644\u062A\u062D\u0642\u0642 \u0645\u0646'],
    [/\u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\uFFFD\??\s*\u0634\u0637\u0629/g, '\u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\u0646\u0634\u0637\u0629'],
    [/\u0627\u0644\u0628\u0631\uFFFD\??\s*\u0627\u0645\u062C/g, '\u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C'],
    [/\u0627\u0644\u0639\u0645\u0644\uFFFD\??\s*\u0627\u0621/g, '\u0627\u0644\u0639\u0645\u0644\u0627\u0621'],
    [/\u0627\u0644\u0648\u062C\u0628\u0627\uFFFD\??\s*\u062A/g, '\u0627\u0644\u0648\u062C\u0628\u0627\u062A'],
    [/\u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\uFFFD\??\s*\u0634\u0637\u0629/g, '\u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\u0646\u0634\u0637\u0629'],
    [/\u0627\u0644\u0645\u0648\u0642\u0648\u0641\u0648\uFFFD\??/g, '\u0627\u0644\u0645\u0648\u0642\u0648\u0641\u0648\u0646'],
    [/\u0627\u0644\u0645\u0624\u0633\u0633\uFFFD\??(?:\s*\uFFFD\??)?/g, '\u0627\u0644\u0645\u0624\u0633\u0633\u0648\u0646'],
    [/\u0627\u0644\u0645\u062F\u0631\u0628\u0648\uFFFD\??/g, '\u0627\u0644\u0645\u062F\u0631\u0628\u0648\u0646'],
    [/\u0627\u0644\u0639\u0645\u064A\u0644\u0627\u062A/g, '\u0627\u0644\u0639\u0645\u064A\u0644\u0627\u062A'],
    [/\u0627\u0644\u0645\u0631\u0627\u062C\u0639\u0629 \u0644\u0644\u062A\uFFFD\??\s*\u0628\uFFFD\??\s*\u0647\u0627\u062A/g, '\u0645\u0631\u0627\u062C\u0639\u0629 \u0627\u0644\u0645\u062A\u0627\u0628\u0639\u0627\u062A'],
    [/\u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u063A\uFFFD\??\s*\u0631 \u0645\u0642\u0631\u0648\u0621\u0629/g, '\u0627\u0644\u0631\u0633\u0627\u0626\u0644 \u063A\u064A\u0631 \u0645\u0642\u0631\u0648\u0621\u0629'],
    [/\u0627\u0644\u062A\u0645\u0631\u064A\u0646\uFFFD\??/g, '\u0627\u0644\u062A\u0645\u0631\u064A\u0646'],
    [/\u0627\u0644\u0645\u0642\u0627\u0633\u0627\u062A/g, '\u0627\u0644\u0642\u064A\u0627\u0633\u0627\u062A'],
    [/\u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A\uFFFD\??/g, '\u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A'],
    [/\u0627\u0644\u062A\u0639\u0644\u064A\u0642\uFFFD\??/g, '\u0627\u0644\u062A\u0639\u0644\u064A\u0642'],
    [/\u0627\u0644\u062A\u0642\u064A\u064A\u0645\uFFFD\??/g, '\u0627\u0644\u062A\u0642\u064A\u064A\u0645'],
    [/\u0627\u0644\u0645\u0633\u0627\u062D\u0629 \u0627\u0644\u0639\u0627\u0645\u0629/g, '\u0627\u0644\u0645\u0646\u0635\u0629 \u0627\u0644\u0639\u0627\u0645\u0629'],
    [/\u0627\u0644\u0648\u0642\u062A \u0627\u0644\u0645\u062A\u0627\u062D/g, '\u0627\u0644\u0648\u0642\u062A \u0627\u0644\u0645\u062A\u0627\u062D'],
    [/\u062A\u0641\u0639\u064A\u0644 \u0627\u0644\u0645\u0633\u0627\u062D\u0629/g, '\u062A\u0641\u0639\u064A\u0644 \u0627\u0644\u0646\u0627\u062F\u064A'],
    [/\u0645\u0631\u0643\u0632 \u0627\u0644\u0625\u0634\u0639\u0627\u0631\u0627\u062A/g, '\u0645\u0631\u0643\u0632 \u0627\u0644\u0625\u0634\u0639\u0627\u0631\u0627\u062A'],
    [/\u0627\u0644\u0644\u0648\u0646 \u0627\u0644\u0623\u0633\u0627\u0633\uFFFD\??/g, '\u0627\u0644\u0644\u0648\u0646 \u0627\u0644\u0623\u0633\u0627\u0633\u064A'],
    [/\u0627\u0644\u0644\u0648\u0646 \u0627\u0644\u062B\u0627\u0646\u0648\uFFFD\??/g, '\u0627\u0644\u0644\u0648\u0646 \u0627\u0644\u062B\u0627\u0646\u0648\u064A'],
    [/\u0627\u0633\u0645 \u0627\u0644\u0646\u0627\u062F\uFFFD\??/g, '\u0627\u0633\u0645 \u0627\u0644\u0646\u0627\u062F\u064A'],
    [/\u062D\u0641\u0638 \u0627\u0644\u0647\u0648\uFFFD\??\s*\u0629/g, '\u062D\u0641\u0638 \u0627\u0644\u0647\u0648\u064A\u0629'],
    [/\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\uFFFD\??\s*\u0627\u0646\u0627\u062A/g, '\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\u064A\u0627\u0646\u0627\u062A'],
    [/\u062D\u0627\u0644\uFFFD\??\s*\u0627/g, '\u062D\u0627\u0644\u064A\u0627'],
    [/\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\u0627\u0642\u0629 \u0646\u0634\u0637\u0629/g, '\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\u0627\u0642\u0629 \u0646\u0634\u0637\u0629'],
    [/\u0627\u0644\u0628\u0631\uFFFD\??\s*\u064A\u062F \u0627\u0644\u062C\u062F\uFFFD\??\s*\u062F/g, '\u0627\u0644\u0628\u0631\u064A\u062F \u0627\u0644\u062C\u062F\u064A\u062F'],
    [/\u0631\u0627\u0628\u0637 \u0627\u0644\u0641\uFFFD\??\s*\u062F\uFFFD\??\s*\u0648/g, '\u0631\u0627\u0628\u0637 \u0627\u0644\u0641\u064A\u062F\u064A\u0648'],
    [/\u0627\u0644\u062A\u0642\u062F\u0645 \u0648\u0627\u0644\u062A\u0648\u0627\u0635\u0644/g, '\u0627\u0644\u062A\u0642\u062F\u0645 \u0648\u0627\u0644\u062A\u0648\u0627\u0635\u0644'],
    [/\u0646\u0638\u0631\u0629 \u0639\u0627\u0645\u0629/g, '\u0646\u0638\u0631\u0629 \u0639\u0627\u0645\u0629']
  ]
  return repairs.reduce((value, [pattern, replacement]) => value.replace(pattern, replacement), text)
}

function repairArabicUiTextPass(text: string) {
  let output = ''
  let bytes: number[] = []
  let characters: string[] = []
  let changed = false
  const decoder = new TextDecoder('utf-8')
  const flush = () => {
    if (!bytes.length) return
    const decoded = decoder.decode(Uint8Array.from(bytes))
    if (/[\u0600-\u06ff]/.test(decoded) && !decoded.includes('\uFFFD')) {
      output += decoded
      changed = true
    } else {
      output += characters.join('')
    }
    bytes = []
    characters = []
  }
  const input = [...text]
  for (let index = 0; index < input.length; index++) {
    const character = input[index]
    const byte = legacyArabicByteMap.get(character)
    if (byte === undefined) {
      flush()
      if (character.codePointAt(0)! <= 0xff) {
        let end = index
        while (end < input.length && input[end].codePointAt(0)! <= 0xff) end++
        const run = input.slice(index, end).join('')
        const decoded = decoder.decode(Uint8Array.from(input.slice(index, end).map(value => value.codePointAt(0)!)))
        if (/[\u0600-\u06ff]/.test(decoded) && !decoded.includes('\uFFFD')) {
          output += decoded
          changed = true
        } else output += run
        index = end - 1
      } else output += character
    } else {
      bytes.push(byte)
      characters.push(character)
    }
  }
  flush()
  return repairBrokenUiArabic(repairLegacyArabic(changed ? output : text))
}

export function repairArabicUiText(text: string) {
  return text
}

void repairArabicUiTextPass

Object.assign(arabic, { 'Loading data...': '\u062C\u0627\u0631\u064D \u062A\u062D\u0645\u064A\u0644 \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A...', 'Reference': '\u0627\u0644\u0645\u0631\u062C\u0639', 'Coach team': '\u0641\u0631\u064A\u0642 \u0627\u0644\u0645\u062F\u0631\u0628\u064A\u0646', 'Manage staff access and assign clients to their responsible coach.': '\u0623\u062F\u0631 \u0627\u0644\u0645\u062F\u0631\u0628\u064A\u0646 \u0648\u0623\u0633\u0646\u062F \u0643\u0644 \u0639\u0645\u064A\u0644 \u0644\u0645\u062F\u0631\u0628\u0647 \u0627\u0644\u0645\u0633\u0624\u0648\u0644.', 'coaches': '\u0645\u062F\u0631\u0628\u0648\u0646', 'Manage coaches and assignments': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u062F\u0631\u0628\u064A\u0646 \u0648\u0625\u0633\u0646\u0627\u062F \u0627\u0644\u0639\u0645\u0644\u0627\u0621' })
Object.assign(arabic, { 'Staff coaches': '\u0627\u0644\u0645\u062F\u0631\u0628\u0648\u0646', 'Select a coach to manage their account and clients.': '\u0627\u062E\u062A\u0631 \u0645\u062F\u0631\u0628\u0627\u064B \u0644\u0625\u062F\u0627\u0631\u0629 \u062D\u0633\u0627\u0628\u0647 \u0648\u0639\u0645\u0644\u0627\u0626\u0647.', 'No staff coaches yet. Add the first coach above.': '\u0644\u0627 \u064A\u0648\u062C\u062F \u0645\u062F\u0631\u0628\u0648\u0646 \u0628\u0639\u062F. \u0623\u0636\u0641 \u0623\u0648\u0644 \u0645\u062F\u0631\u0628 \u0645\u0646 \u0627\u0644\u0646\u0645\u0648\u0630\u062C \u0623\u0639\u0644\u0627\u0647.', 'clients': '\u0639\u0645\u0644\u0627\u0621', 'Add a coach': '\u0625\u0636\u0627\u0641\u0629 \u0645\u062F\u0631\u0628' })
export function toEnglish(text: string) {
  return englishUi[text] ?? english[text] ?? text
}

export function toArabic(text: string) {
  return arabic[text] ?? text
}
export function LanguageProvider({ children }: { children: ReactNode }) {
  const [language, setLanguageState] = useState<Language>(() => localStorage.getItem('coachpulse.language.v5') === 'ar' ? 'ar' : 'en')
  const setLanguage = useCallback((next: Language) => { localStorage.setItem('coachpulse.language.v5', next); setLanguageState(next) }, [])
  const t = useCallback((text: string) => language === 'ar' ? toArabic(text) : toEnglish(text), [language])
  useEffect(() => { document.documentElement.lang = language; document.documentElement.dir = language === 'ar' ? 'rtl' : 'ltr' }, [language])
  const value = useMemo(() => ({ language, setLanguage, t }), [language, setLanguage, t])
  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>
}

export function useLanguage() {
  const value = useContext(LanguageContext)
  if (!value) throw new Error('LanguageProvider is missing.')
  return value
}

export function LanguageToggle() {
  const { language, setLanguage } = useLanguage()
  return <label className="language-toggle"><span>Language</span><select aria-label="Choose language" value={language} onChange={event => setLanguage(event.target.value as Language)}><option value="en">English</option><option value="ar">&#x627;&#x644;&#x639;&#x631;&#x628;&#x64A;&#x629;</option></select></label>
}
Object.assign(arabic, {
  'Plans': '\u0627\u0644\u0628\u0627\u0642\u0627\u062A', 'Create clear membership offers and manage what clients can request.': '\u0623\u0646\u0634\u0626 \u0628\u0627\u0642\u0627\u062A \u0648\u0627\u0636\u062D\u0629 \u0648\u0623\u062F\u0631 \u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\u0645\u062A\u0627\u062D\u0629 \u0644\u0644\u0639\u0645\u0644\u0627\u0621.',
  'STEP 1': '\u0627\u0644\u062E\u0637\u0648\u0629 1', 'STEP 2': '\u0627\u0644\u062E\u0637\u0648\u0629 2', 'STEP 3': '\u0627\u0644\u062E\u0637\u0648\u0629 3', 'Create a membership plan': '\u0625\u0646\u0634\u0627\u0621 \u0628\u0627\u0642\u0629 \u0627\u0634\u062A\u0631\u0627\u0643', 'Set the price and access period. Clients can request an active plan from their subscription page.': '\u062D\u062F\u062F \u0627\u0644\u0633\u0639\u0631 \u0648\u0645\u062F\u0629 \u0627\u0644\u0648\u0635\u0648\u0644. \u064A\u0633\u062A\u0637\u064A\u0639 \u0627\u0644\u0639\u0645\u064A\u0644 \u0637\u0644\u0628 \u0627\u0644\u0628\u0627\u0642\u0629 \u0627\u0644\u0646\u0634\u0637\u0629 \u0645\u0646 \u0635\u0641\u062D\u0629 \u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A\u0647.', 'Plan name': '\u0627\u0633\u0645 \u0627\u0644\u0628\u0627\u0642\u0629', 'Example: 12-week coaching': '\u0645\u062B\u0627\u0644: \u062A\u062F\u0631\u064A\u0628 \u0644\u0645\u062F\u0629 12 \u0623\u0633\u0628\u0648\u0639\u064B\u0627', 'Price': '\u0627\u0644\u0633\u0639\u0631', 'Duration in days': '\u0627\u0644\u0645\u062F\u0629 \u0628\u0627\u0644\u0623\u064A\u0627\u0645', 'Create plan': '\u0625\u0646\u0634\u0627\u0621 \u0628\u0627\u0642\u0629',
  'Plan created. Clients can now request it.': '\u062A\u0645 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0628\u0627\u0642\u0629. \u064A\u0633\u062A\u0637\u064A\u0639 \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0637\u0644\u0628\u0647\u0627 \u0627\u0644\u0622\u0646.', 'Could not create the plan.': '\u062A\u0639\u0630\u0631 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0628\u0627\u0642\u0629.', 'Plan updated. Existing subscriptions keep their original price and duration.': '\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0628\u0627\u0642\u0629. \u062A\u062D\u062A\u0641\u0638 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0627\u0644\u0642\u0627\u0626\u0645\u0629 \u0628\u0627\u0644\u0633\u0639\u0631 \u0648\u0627\u0644\u0645\u062F\u0629 \u0627\u0644\u0623\u0635\u0644\u064A\u064A\u0646.', 'Could not update the plan.': '\u062A\u0639\u0630\u0631 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0628\u0627\u0642\u0629.', 'Archive this plan? Clients can no longer request it.': '\u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0627\u0642\u0629\u061F \u0644\u0646 \u064A\u062A\u0645\u0643\u0646 \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0645\u0646 \u0637\u0644\u0628\u0647\u0627.', 'Plan archived. Existing subscriptions are unchanged.': '\u062A\u0645\u062A \u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0627\u0642\u0629. \u0644\u0645 \u062A\u062A\u063A\u064A\u0631 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0627\u0644\u0642\u0627\u0626\u0645\u0629.', 'Could not archive the plan.': '\u062A\u0639\u0630\u0631 \u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0627\u0642\u0629.',
  'Your membership plans': '\u0628\u0627\u0642\u0627\u062A \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643', 'Active plans appear to clients. Existing subscriptions keep their original price and duration.': '\u062A\u0638\u0647\u0631 \u0627\u0644\u0628\u0627\u0642\u0627\u062A \u0627\u0644\u0646\u0634\u0637\u0629 \u0644\u0644\u0639\u0645\u0644\u0627\u0621. \u062A\u062D\u062A\u0641\u0638 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0627\u0644\u0642\u0627\u0626\u0645\u0629 \u0628\u0627\u0644\u0633\u0639\u0631 \u0648\u0627\u0644\u0645\u062F\u0629 \u0627\u0644\u0623\u0635\u0644\u064A\u064A\u0646.', 'plans': '\u0628\u0627\u0642\u0627\u062A', 'Active': '\u0646\u0634\u0637\u0629', 'Archived': '\u0645\u0624\u0631\u0634\u0641\u0629', 'Membership plan': '\u0628\u0627\u0642\u0629 \u0627\u0634\u062A\u0631\u0627\u0643', 'days of access': '\u064A\u0648\u0645 \u0645\u0646 \u0627\u0644\u0648\u0635\u0648\u0644', 'Edit plan': '\u062A\u0639\u062F\u064A\u0644 \u0627\u0644\u0628\u0627\u0642\u0629', 'No plans yet': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\u0627\u0642\u0627\u062A \u0628\u0639\u062F', 'Create your first plan above. Then clients can request it and pay online, by InstaPay or through a recorded cash payment.': '\u0623\u0646\u0634\u0626 \u0623\u0648\u0644 \u0628\u0627\u0642\u0629 \u0623\u0639\u0644\u0627\u0647. \u0628\u0639\u062F\u0647\u0627 \u064A\u0637\u0644\u0628\u0647\u0627 \u0627\u0644\u0639\u0645\u064A\u0644 \u0648\u064A\u062F\u0641\u0639 \u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A\u064B\u0627 \u0623\u0648 \u0639\u0628\u0631 InstaPay \u0623\u0648 \u0628\u062A\u0633\u062C\u064A\u0644 \u062F\u0641\u0639\u0629 \u0646\u0642\u062F\u064A\u0629.', 'Edit or archive a plan': '\u062A\u0639\u062F\u064A\u0644 \u0628\u0627\u0642\u0629 \u0623\u0648 \u0623\u0631\u0634\u0641\u062A\u0647\u0627', 'Archiving prevents new requests but preserves existing subscriptions.': '\u062A\u0645\u0646\u0639 \u0627\u0644\u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0637\u0644\u0628\u0627\u062A \u0627\u0644\u062C\u062F\u064A\u062F\u0629 \u0648\u062A\u0628\u0642\u064A \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0627\u0644\u0642\u0627\u0626\u0645\u0629.', 'Choose a plan': '\u0627\u062E\u062A\u0631 \u0628\u0627\u0642\u0629', 'Save changes': '\u062D\u0641\u0638 \u0627\u0644\u062A\u063A\u064A\u064A\u0631\u0627\u062A', 'Archive plan': '\u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0627\u0642\u0629',
  'Next: build your coaching offer': '\u0627\u0644\u062E\u0637\u0648\u0629 \u0627\u0644\u062A\u0627\u0644\u064A\u0629: \u0623\u0639\u062F\u062F \u0628\u0631\u0646\u0627\u0645\u062C \u0627\u0644\u062A\u062F\u0631\u064A\u0628', 'Add exercises and create training programs before assigning a program to a client.': '\u0623\u0636\u0641 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u0648\u0623\u0646\u0634\u0626 \u0628\u0631\u0627\u0645\u062C \u062A\u062F\u0631\u064A\u0628\u064A\u0629 \u0642\u0628\u0644 \u0625\u0633\u0646\u0627\u062F \u0628\u0631\u0646\u0627\u0645\u062C \u0644\u0639\u0645\u064A\u0644.', 'Open exercise library': '\u0641\u062A\u062D \u0645\u0643\u062A\u0628\u0629 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646', 'Manage subscriptions': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A',
  'A clear path from published plan to active client access.': '\u0645\u0633\u0627\u0631 \u0648\u0627\u0636\u062D \u0645\u0646 \u0646\u0634\u0631 \u0627\u0644\u0628\u0627\u0642\u0629 \u0625\u0644\u0649 \u062A\u0641\u0639\u064A\u0644 \u0627\u0634\u062A\u0631\u0627\u0643 \u0627\u0644\u0639\u0645\u064A\u0644.', 'Publish a plan': '\u0627\u0646\u0634\u0631 \u0628\u0627\u0642\u0629', 'Set a price and duration before clients can request membership.': '\u062D\u062F\u062F \u0627\u0644\u0633\u0639\u0631 \u0648\u0627\u0644\u0645\u062F\u0629 \u0642\u0628\u0644 \u0623\u0646 \u064A\u0637\u0644\u0628 \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643.', 'Invite the client': '\u0627\u062F\u0639 \u0627\u0644\u0639\u0645\u064A\u0644', 'The client chooses a plan and submits a payment request from their account.': '\u064A\u062E\u062A\u0627\u0631 \u0627\u0644\u0639\u0645\u064A\u0644 \u0628\u0627\u0642\u0629 \u0648\u064A\u0631\u0633\u0644 \u0637\u0644\u0628 \u0627\u0644\u062F\u0641\u0639 \u0645\u0646 \u062D\u0633\u0627\u0628\u0647.', 'Review payment and activate': '\u0631\u0627\u062C\u0639 \u0627\u0644\u062F\u0641\u0639 \u0648\u0641\u0639\u0651\u0644 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643', 'Confirm InstaPay, record a pending cash payment, or activate access manually.': '\u0623\u0643\u062F \u062A\u062D\u0648\u064A\u0644 InstaPay \u0623\u0648 \u0633\u062C\u0644 \u062F\u0641\u0639\u0629 \u0646\u0642\u062F\u064A\u0629 \u0645\u0639\u0644\u0642\u0629 \u0623\u0648 \u0641\u0639\u0644 \u0627\u0644\u0648\u0635\u0648\u0644 \u064A\u062F\u0648\u064A\u0627.',
  'Activate without online payment': '\u062A\u0641\u0639\u064A\u0644 \u0628\u062F\u0648\u0646 \u062F\u0641\u0639 \u0625\u0644\u0643\u062A\u0631\u0648\u0646\u064A', 'Use this when payment was collected outside the platform. This grants access immediately and does not create a payment record. To record cash in the ledger, first have the client submit a subscription request, then record that pending request under Payments.': '\u0627\u0633\u062A\u062E\u062F\u0645 \u0647\u0630\u0627 \u0625\u0630\u0627 \u062A\u0645 \u0627\u0644\u062A\u062D\u0635\u064A\u0644 \u062E\u0627\u0631\u062C \u0627\u0644\u0645\u0646\u0635\u0629. \u064A\u0641\u0639\u0644 \u0627\u0644\u0648\u0635\u0648\u0644 \u0641\u0648\u0631\u0627 \u0648\u0644\u0627 \u064A\u0633\u062C\u0644 \u062F\u0641\u0639\u0629. \u0644\u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u0646\u0642\u062F\u064A\u0629 \u0641\u064A \u0627\u0644\u062D\u0633\u0627\u0628\u0627\u062A\u060C \u0644\u064A\u0642\u062F\u0645 \u0627\u0644\u0639\u0645\u064A\u0644 \u0637\u0644\u0628\u0627 \u0623\u0648\u0644\u0627 \u062B\u0645 \u0633\u062C\u0644\u0647 \u0645\u0646 \u0627\u0644\u0645\u062F\u0641\u0648\u0639\u0627\u062A.', 'Subscription activated.': '\u062A\u0645 \u062A\u0641\u0639\u064A\u0644 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643.', 'Could not activate subscription.': '\u062A\u0639\u0630\u0631 \u062A\u0641\u0639\u064A\u0644 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643.', 'Reason (required)': '\u0627\u0644\u0633\u0628\u0628 (\u0625\u062C\u0628\u0627\u0631\u064A)', 'Example: payment received in person': '\u0645\u062B\u0627\u0644: \u062A\u0645 \u0627\u0633\u062A\u0644\u0627\u0645 \u0627\u0644\u0645\u0628\u0644\u063A \u0645\u0628\u0627\u0634\u0631\u0629', 'Client subscriptions': '\u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0627\u0644\u0639\u0645\u0644\u0627\u0621', 'Review status and dates. Select a subscription to manage it.': '\u0631\u0627\u062C\u0639 \u0627\u0644\u062D\u0627\u0644\u0629 \u0648\u0627\u0644\u062A\u0648\u0627\u0631\u064A\u062E. \u0627\u062E\u062A\u0631 \u0627\u0634\u062A\u0631\u0627\u0643\u0627 \u0644\u0625\u062F\u0627\u0631\u062A\u0647.', 'Manage clients': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0639\u0645\u0644\u0627\u0621', 'Review payments': '\u0645\u0631\u0627\u062C\u0639\u0629 \u0627\u0644\u0645\u062F\u0641\u0648\u0639\u0627\u062A', 'No subscriptions yet.': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0627\u0634\u062A\u0631\u0627\u0643\u0627\u062A \u0628\u0639\u062F.', 'Publish a plan, then invite clients to request it.': '\u0627\u0646\u0634\u0631 \u0628\u0627\u0642\u0629 \u062B\u0645 \u0627\u062F\u0639 \u0627\u0644\u0639\u0645\u0644\u0627\u0621 \u0644\u0637\u0644\u0628\u0647\u0627.', 'Reason for this change': '\u0633\u0628\u0628 \u0627\u0644\u062A\u063A\u064A\u064A\u0631', 'Cancel this subscription?': '\u0625\u0644\u063A\u0627\u0621 \u0647\u0630\u0627 \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643\u061F', 'Subscription updated.': '\u062A\u0645 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643.', 'Could not update the subscription.': '\u062A\u0639\u0630\u0631 \u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0627\u0634\u062A\u0631\u0627\u0643.', 'Client': '\u0627\u0644\u0639\u0645\u064A\u0644', 'days': '\u064A\u0648\u0645'
})

Object.assign(arabic, {
  'Platform management': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u0646\u0635\u0629', 'Refresh data': '\u062A\u062D\u062F\u064A\u062B \u0627\u0644\u0628\u064A\u0627\u0646\u0627\u062A', 'Sign out': '\u062A\u0633\u062C\u064A\u0644 \u0627\u0644\u062E\u0631\u0648\u062C',
  'Administration': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u0646\u0635\u0629', 'Manage and monitor the platform': '\u0625\u062F\u0627\u0631\u0629 \u0648\u0645\u0631\u0627\u0642\u0628\u0629 \u0627\u0644\u0645\u0646\u0635\u0629', 'Platform administrator permissions': '\u0635\u0644\u0627\u062D\u064A\u0627\u062A \u0645\u0633\u0624\u0648\u0644 \u0627\u0644\u0645\u0646\u0635\u0629',
  'Clearer platform management': '\u0625\u062F\u0627\u0631\u0629 \u0627\u0644\u0645\u0646\u0635\u0629 \u0628\u0648\u0636\u0648\u062D \u0623\u0643\u0628\u0631', 'A smoother experience for everyone': '\u062A\u062C\u0631\u0628\u0629 \u0623\u0633\u0647\u0644 \u0644\u0644\u062C\u0645\u064A\u0639',
  'Monitor club health, review recent activity, and open common tasks.': '\u062A\u0627\u0628\u0639 \u062D\u0627\u0644\u0629 \u0627\u0644\u0623\u0646\u062F\u064A\u0629\u060C \u0648\u0631\u0627\u062C\u0639 \u0623\u062D\u062F\u062B \u0627\u0644\u0623\u0646\u0634\u0637\u0629\u060C \u0648\u0627\u0646\u062A\u0642\u0644 \u0645\u0628\u0627\u0634\u0631\u0629 \u0625\u0644\u0649 \u0627\u0644\u0625\u062C\u0631\u0627\u0621\u0627\u062A \u0627\u0644\u0645\u0637\u0644\u0648\u0628\u0629.',
  'Control center': '\u0645\u0631\u0643\u0632 \u0627\u0644\u062A\u062D\u0643\u0645', 'Browse clubs': '\u0627\u0633\u062A\u0639\u0631\u0627\u0636 \u0627\u0644\u0623\u0646\u062F\u064A\u0629', 'Audit log': '\u0633\u062C\u0644 \u0627\u0644\u062A\u062F\u0642\u064A\u0642', 'Training and management platform': '\u0645\u0646\u0635\u0629 \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0648\u0627\u0644\u0625\u062F\u0627\u0631\u0629', 'Platform overview and service health': '\u0646\u0638\u0631\u0629 \u0639\u0627\u0645\u0629 \u0644\u0644\u0645\u0646\u0635\u0629 \u0648\u062D\u0627\u0644\u0629 \u0627\u0644\u062E\u062F\u0645\u0627\u062A',
  'Overview': '\u0646\u0638\u0631\u0629 \u0639\u0627\u0645\u0629', 'Platform metrics': '\u0645\u0624\u0634\u0631\u0627\u062A \u0627\u0644\u0645\u0646\u0635\u0629', 'Aggregated figures for club growth and platform usage.': '\u0623\u0631\u0642\u0627\u0645 \u0645\u062C\u0645\u0639\u0629 \u0644\u0645\u062A\u0627\u0628\u0639\u0629 \u0646\u0645\u0648 \u0627\u0644\u0623\u0646\u062F\u064A\u0629 \u0648\u0627\u0633\u062A\u062E\u062F\u0627\u0645 \u0627\u0644\u0645\u0646\u0635\u0629.', 'Platform summary': '\u0645\u0644\u062E\u0635 \u0627\u0644\u0645\u0646\u0635\u0629', 'No metrics are available yet.': '\u0644\u0627 \u062A\u062A\u0648\u0641\u0631 \u0645\u0624\u0634\u0631\u0627\u062A \u062D\u0627\u0644\u064A\u0627\u064B.',
  'Total clubs': '\u0625\u062C\u0645\u0627\u0644\u064A \u0627\u0644\u0623\u0646\u062F\u064A\u0629', 'Active clubs': '\u0627\u0644\u0623\u0646\u062F\u064A\u0629 \u0627\u0644\u0646\u0634\u0637\u0629', 'Suspended clubs': '\u0627\u0644\u0623\u0646\u062F\u064A\u0629 \u0627\u0644\u0645\u0648\u0642\u0648\u0641\u0629', 'Total users': '\u0625\u062C\u0645\u0627\u0644\u064A \u0627\u0644\u0645\u0633\u062A\u062E\u062F\u0645\u064A\u0646', 'Total clients': '\u0625\u062C\u0645\u0627\u0644\u064A \u0627\u0644\u0639\u0645\u0644\u0627\u0621'
})

Object.assign(arabic, {
  'Training program': '\u0628\u0631\u0646\u0627\u0645\u062C \u062A\u062F\u0631\u064A\u0628\u064A',
  'Program details': '\u062A\u0641\u0627\u0635\u064A\u0644 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Program exercises': '\u062A\u0645\u0627\u0631\u064A\u0646 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'No exercises added yet.': '\u0644\u0645 \u062A\u064F\u0636\u0641 \u062A\u0645\u0627\u0631\u064A\u0646 \u0628\u0639\u062F.',
  'Edit program': '\u062A\u0639\u062F\u064A\u0644 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Program name': '\u0627\u0633\u0645 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Duration in days': '\u0627\u0644\u0645\u062F\u0629 \u0628\u0627\u0644\u0623\u064A\u0627\u0645',
  'Save changes': '\u062D\u0641\u0638 \u0627\u0644\u062A\u063A\u064A\u064A\u0631\u0627\u062A',
  'Add an exercise': '\u0625\u0636\u0627\u0641\u0629 \u062A\u0645\u0631\u064A\u0646',
  'Exercise': '\u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Sets': '\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0627\u062A',
  'Reps': '\u0627\u0644\u062A\u0643\u0631\u0627\u0631\u0627\u062A',
  'Day': '\u0627\u0644\u064A\u0648\u0645',
  'Day number': '\u0631\u0642\u0645 \u0627\u0644\u064A\u0648\u0645',
  'Add exercise': '\u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Edit a program exercise': '\u062A\u0639\u062F\u064A\u0644 \u062A\u0645\u0631\u064A\u0646 \u0641\u064A \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Choose an exercise to edit': '\u0627\u062E\u062A\u0631 \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0627\u0644\u0645\u0631\u0627\u062F \u062A\u0639\u062F\u064A\u0644\u0647',
  'Rest seconds': '\u0648\u0642\u062A \u0627\u0644\u0631\u0627\u062D\u0629 \u0628\u0627\u0644\u062B\u0648\u0627\u0646\u064A',
  'Save exercise changes': '\u062D\u0641\u0638 \u062A\u0639\u062F\u064A\u0644 \u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Delete exercise': '\u062D\u0630\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Reorder exercises': '\u062A\u0631\u062A\u064A\u0628 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646',
  'Exercise order': '\u062A\u0631\u062A\u064A\u0628 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646',
  'Save order': '\u062D\u0641\u0638 \u0627\u0644\u062A\u0631\u062A\u064A\u0628',
  'Archive program': '\u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Duplicate program': '\u0646\u0633\u062E \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Delete draft': '\u062D\u0630\u0641 \u0627\u0644\u0645\u0633\u0648\u062F\u0629',
  'Save as template': '\u062D\u0641\u0638 \u0643\u0642\u0627\u0644\u0628',
  'Templates': '\u0627\u0644\u0642\u0648\u0627\u0644\u0628',
  'No templates available.': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0642\u0648\u0627\u0644\u0628 \u0645\u062A\u0627\u062D\u0629.',
  'Choose a template': '\u0627\u062E\u062A\u0631 \u0642\u0627\u0644\u0628\u0627\u064B',
  'Client (optional)': '\u0627\u0644\u0639\u0645\u064A\u0644 (\u0627\u062E\u062A\u064A\u0627\u0631\u064A)',
  'Create program from template': '\u0625\u0646\u0634\u0627\u0621 \u0628\u0631\u0646\u0627\u0645\u062C \u0645\u0646 \u0642\u0627\u0644\u0628',
  'Draft created. Add exercises, then assign it to a client.': '\u062A\u0645 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0645\u0633\u0648\u062F\u0629. \u0623\u0636\u0641 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u062B\u0645 \u0623\u0633\u0646\u062F\u0647\u0627 \u0644\u0644\u0639\u0645\u064A\u0644.',
  'Program assigned.': '\u062A\u0645 \u0625\u0633\u0646\u0627\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C.',
  'Draft deleted.': '\u062A\u0645 \u062D\u0630\u0641 \u0627\u0644\u0645\u0633\u0648\u062F\u0629.',
  'Program archived.': '\u062A\u0645\u062A \u0623\u0631\u0634\u0641\u0629 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C.',
  'Program duplicated.': '\u062A\u0645 \u0646\u0633\u062E \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C.',
  'Assigning…': '\u062C\u0627\u0631\u064D \u0625\u0633\u0646\u0627\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C...',
  'Creating…': '\u062C\u0627\u0631\u064D \u0627\u0644\u0625\u0646\u0634\u0627\u0621...',
  'Duration not set': '\u0627\u0644\u0645\u062F\u0629 \u063A\u064A\u0631 \u0645\u062D\u062F\u062F\u0629',
  'Assigned to': '\u0645\u0633\u0646\u062F \u0625\u0644\u0649',
  'Record': '\u0633\u062C\u0644',
  'Item': '\u0639\u0646\u0635\u0631',
  'No items available': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0639\u0646\u0627\u0635\u0631 \u0645\u062A\u0627\u062D\u0629'
})

Object.assign(arabic, {
  'Training programs': '\u0627\u0644\u0628\u0631\u0627\u0645\u062C \u0627\u0644\u062A\u062F\u0631\u064A\u0628\u064A\u0629',
  'Create a program, add its exercises, then assign it to a client.': '\u0623\u0646\u0634\u0626 \u0628\u0631\u0646\u0627\u0645\u062C\u0627\u064B\u060C \u0648\u0623\u0636\u0641 \u062A\u0645\u0627\u0631\u064A\u0646\u0647\u060C \u062B\u0645 \u0623\u0633\u0646\u062F\u0647 \u0644\u0639\u0645\u064A\u0644.',
  'Create a program': '\u0625\u0646\u0634\u0627\u0621 \u0628\u0631\u0646\u0627\u0645\u062C',
  'Example: Strength foundation': '\u0645\u062B\u0627\u0644: \u0628\u0631\u0646\u0627\u0645\u062C \u062A\u0623\u0633\u064A\u0633 \u0627\u0644\u0642\u0648\u0629',
  'Create draft': '\u0625\u0646\u0634\u0627\u0621 \u0645\u0633\u0648\u062F\u0629',
  'Add exercises to the draft, then choose the client and start date.': '\u0623\u0636\u0641 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u0625\u0644\u0649 \u0627\u0644\u0645\u0633\u0648\u062F\u0629\u060C \u062B\u0645 \u0627\u062E\u062A\u0631 \u0627\u0644\u0639\u0645\u064A\u0644 \u0648\u062A\u0627\u0631\u064A\u062E \u0627\u0644\u0628\u062F\u0621.',
  'Workspace programs': '\u0628\u0631\u0627\u0645\u062C \u0627\u0644\u0646\u0627\u062F\u064A',
  'Open a program to review or edit it. Status and assignment are shown below.': '\u0627\u0641\u062A\u062D \u0628\u0631\u0646\u0627\u0645\u062C\u0627\u064B \u0644\u0645\u0631\u0627\u062C\u0639\u062A\u0647 \u0623\u0648 \u062A\u0639\u062F\u064A\u0644\u0647. \u062A\u0638\u0647\u0631 \u0627\u0644\u062D\u0627\u0644\u0629 \u0648\u0627\u0644\u0625\u0633\u0646\u0627\u062F \u0623\u062F\u0646\u0627\u0647.',
  'programs': '\u0628\u0631\u0627\u0645\u062C',
  'No programs yet. Create your first draft above.': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0628\u0631\u0627\u0645\u062C \u0628\u0639\u062F. \u0623\u0646\u0634\u0626 \u0623\u0648\u0644 \u0645\u0633\u0648\u062F\u0629 \u0645\u0646 \u0627\u0644\u0623\u0639\u0644\u0649.',
  'Draft created. Add exercises, then assign it to a client.': '\u062A\u0645 \u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0645\u0633\u0648\u062F\u0629. \u0623\u0636\u0641 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u062B\u0645 \u0623\u0633\u0646\u062F\u0647\u0627 \u0644\u0644\u0639\u0645\u064A\u0644.',
  'Program assigned.': '\u062A\u0645 \u0625\u0633\u0646\u0627\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C.',
  'Manage': '\u0625\u062F\u0627\u0631\u0629',
  'Not assigned': '\u063A\u064A\u0631 \u0645\u0633\u0646\u062F',
  'days': '\u0623\u064A\u0627\u0645',
  'Record': '\u0633\u062C\u0644',
  'Item': '\u0639\u0646\u0635\u0631',
  'No items available': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0639\u0646\u0627\u0635\u0631 \u0645\u062A\u0627\u062D\u0629',
  'Choose an item': '\u0627\u062E\u062A\u0631 \u0639\u0646\u0635\u0631\u0627\u064B'
})

Object.assign(arabic, {
  'Program setup': '\u0625\u0639\u062F\u0627\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C',
  'Program exercises are ready. Assign this program when you are ready.': '\u062A\u0645\u062A \u0625\u0636\u0627\u0641\u0629 \u062A\u0645\u0627\u0631\u064A\u0646 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C. \u064A\u0645\u0643\u0646\u0643 \u0625\u0633\u0646\u0627\u062F\u0647 \u0644\u0639\u0645\u064A\u0644 \u0627\u0644\u0622\u0646.',
  'Step 1 of 2: add at least one exercise. Then assign the program to a client.': '\u0627\u0644\u062E\u0637\u0648\u0629 1 \u0645\u0646 2: \u0623\u0636\u0641 \u062A\u0645\u0631\u064A\u0646\u0627\u064B \u0648\u0627\u062D\u062F\u0627\u064B \u0639\u0644\u0649 \u0627\u0644\u0623\u0642\u0644\u060C \u062B\u0645 \u0623\u0633\u0646\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C \u0644\u0639\u0645\u064A\u0644.',
  'Step 2: assign to a client': '\u0627\u0644\u062E\u0637\u0648\u0629 2: \u0625\u0633\u0646\u0627\u062F \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C \u0644\u0639\u0645\u064A\u0644',
  'Close details': '\u0625\u063A\u0644\u0627\u0642 \u0627\u0644\u062A\u0641\u0627\u0635\u064A\u0644',
  'Loading program exercises...': '\u062C\u0627\u0631\u064D \u062A\u062D\u0645\u064A\u0644 \u062A\u0645\u0627\u0631\u064A\u0646 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C…',
  'Add an exercise above to unlock assignment.': '\u0623\u0636\u0641 \u062A\u0645\u0631\u064A\u0646\u0627\u064B \u0623\u0639\u0644\u0627\u0647 \u0644\u0644\u0627\u0646\u062A\u0642\u0627\u0644 \u0625\u0644\u0649 \u062E\u0637\u0648\u0629 \u0627\u0644\u0625\u0633\u0646\u0627\u062F.',
  'AI draft review': '\u0645\u0631\u0627\u062C\u0639\u0629 \u0645\u0633\u0648\u062F\u0627\u062A \u0627\u0644\u0630\u0643\u0627\u0621 \u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A',
  'Review generated training and meal plans before adding them to your workspace.': '\u0631\u0627\u062C\u0639 \u062E\u0637\u062A\u064A \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0648\u0627\u0644\u0648\u062C\u0628\u0627\u062A \u0627\u0644\u0645\u0648\u0644\u062F\u062A\u064A\u0646 \u0642\u0628\u0644 \u0625\u0636\u0627\u0641\u062A\u0647\u0645\u0627 \u0625\u0644\u0649 \u0627\u0644\u0646\u0627\u062F\u064A.',
  'Saved AI drafts': '\u0645\u0633\u0648\u062F\u0627\u062A \u0627\u0644\u0630\u0643\u0627\u0621 \u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A \u0627\u0644\u0645\u062D\u0641\u0648\u0638\u0629',
  'Choose a draft to inspect its plan, resolve unmatched exercises, and approve it.': '\u0627\u062E\u062A\u0631 \u0645\u0633\u0648\u062F\u0629 \u0644\u0645\u0631\u0627\u062C\u0639\u0629 \u0627\u0644\u062E\u0637\u0629\u060C \u0648\u062D\u0644 \u0645\u0634\u0643\u0644\u0629 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u063A\u064A\u0631 \u0627\u0644\u0645\u0637\u0627\u0628\u0642\u0629\u060C \u062B\u0645 \u0627\u0639\u062A\u0645\u0627\u062F\u0647\u0627.',
  'Back to AI tools': '\u0627\u0644\u0639\u0648\u062F\u0629 \u0625\u0644\u0649 \u0623\u062F\u0648\u0627\u062A \u0627\u0644\u0630\u0643\u0627\u0621 \u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A',
  'No saved drafts yet. Generate a program first.': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0645\u0633\u0648\u062F\u0627\u062A \u0645\u062D\u0641\u0648\u0638\u0629. \u0623\u0646\u0634\u0626 \u0628\u0631\u0646\u0627\u0645\u062C\u0627\u064B \u0623\u0648\u0644\u0627\u064B.',
  'Generated with': '\u0627\u0644\u0646\u0645\u0648\u0630\u062C', 'AI model': '\u0646\u0645\u0648\u0630\u062C \u0627\u0644\u0630\u0643\u0627\u0621 \u0627\u0644\u0627\u0635\u0637\u0646\u0627\u0639\u064A',
  'Review draft for': '\u0645\u0631\u0627\u062C\u0639\u0629 \u0645\u0633\u0648\u062F\u0629 \u0627\u0644\u0639\u0645\u064A\u0644', 'Prompt version': '\u0646\u0633\u062E\u0629 \u0627\u0644\u062A\u0639\u0644\u064A\u0645\u0627\u062A', 'Model': '\u0627\u0644\u0646\u0645\u0648\u0630\u062C',
  'Exercises need attention before approval': '\u062A\u062D\u062A\u0627\u062C \u0628\u0639\u0636 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u0625\u0644\u0649 \u0645\u0631\u0627\u062C\u0639\u0629 \u0642\u0628\u0644 \u0627\u0644\u0627\u0639\u062A\u0645\u0627\u062F',
  'Replace unmatched exercises with items from the exercise library, then save the draft.': '\u0627\u0633\u062A\u0628\u062F\u0644 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u063A\u064A\u0631 \u0627\u0644\u0645\u0637\u0627\u0628\u0642\u0629 \u0628\u062A\u0645\u0627\u0631\u064A\u0646 \u0645\u0646 \u0627\u0644\u0645\u0643\u062A\u0628\u0629\u060C \u062B\u0645 \u0627\u062D\u0641\u0638 \u0627\u0644\u0645\u0633\u0648\u062F\u0629.',
  'Training plan preview': '\u0645\u0639\u0627\u064A\u0646\u0629 \u062E\u0637\u0629 \u0627\u0644\u062A\u062F\u0631\u064A\u0628', 'Meal plan preview': '\u0645\u0639\u0627\u064A\u0646\u0629 \u062E\u0637\u0629 \u0627\u0644\u0648\u062C\u0628\u0627\u062A',
  'Advanced draft editing': '\u062A\u0639\u062F\u064A\u0644 \u0645\u062A\u0642\u062F\u0645 \u0644\u0644\u0645\u0633\u0648\u062F\u0629', 'Edit structured plan JSON only when needed. Invalid JSON will not be saved.': '\u0644\u0627 \u062A\u0639\u062F\u0651\u0644 \u0628\u064A\u0627\u0646\u0627\u062A JSON \u0625\u0644\u0627 \u0639\u0646\u062F \u0627\u0644\u062D\u0627\u062C\u0629. \u0644\u0646 \u064A\u064F\u062D\u0641\u0638 \u0627\u0644\u062A\u0646\u0633\u064A\u0642 \u063A\u064A\u0631 \u0627\u0644\u0635\u0627\u0644\u062D.',
  'Training plan JSON': 'JSON \u062E\u0637\u0629 \u0627\u0644\u062A\u062F\u0631\u064A\u0628', 'Meal plan JSON': 'JSON \u062E\u0637\u0629 \u0627\u0644\u0648\u062C\u0628\u0627\u062A', 'Unmatched exercise names, one per line': '\u0623\u0633\u0645\u0627\u0621 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u063A\u064A\u0631 \u0627\u0644\u0645\u0637\u0627\u0628\u0642\u0629\u060C \u0627\u0633\u0645 \u0641\u064A \u0643\u0644 \u0633\u0637\u0631', 'Save draft changes': '\u062D\u0641\u0638 \u062A\u0639\u062F\u064A\u0644\u0627\u062A \u0627\u0644\u0645\u0633\u0648\u062F\u0629',
  'Request a revision': '\u0637\u0644\u0628 \u062A\u0639\u062F\u064A\u0644', 'A revised draft is created separately. This draft remains available.': '\u0633\u064A\u064F\u0646\u0634\u0623 \u0625\u0635\u062F\u0627\u0631 \u0645\u0639\u062F\u0651\u0644 \u0643\u0645\u0633\u0648\u062F\u0629 \u062C\u062F\u064A\u062F\u0629\u060C \u0648\u0633\u062A\u0628\u0642\u0649 \u0647\u0630\u0647 \u0627\u0644\u0645\u0633\u0648\u062F\u0629 \u0645\u062A\u0627\u062D\u0629.',
  'Notes for the coach assistant': '\u0645\u0644\u0627\u062D\u0638\u0627\u062A \u0644\u0644\u0645\u0633\u0627\u0639\u062F \u0627\u0644\u062A\u062F\u0631\u064A\u0628\u064A', 'Create revised draft': '\u0625\u0646\u0634\u0627\u0621 \u0645\u0633\u0648\u062F\u0629 \u0645\u0639\u062F\u0651\u0644\u0629',
  'Approve and create program': '\u0627\u0639\u062A\u0645\u0627\u062F \u0648\u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0628\u0631\u0646\u0627\u0645\u062C', 'Reject draft': '\u0631\u0641\u0636 \u0627\u0644\u0645\u0633\u0648\u062F\u0629',
  'Created successfully': '\u062A\u0645 \u0627\u0644\u0625\u0646\u0634\u0627\u0621 \u0628\u0646\u062C\u0627\u062D', 'The training program and meal plan are saved as drafts. Open them to review details, add exercises if needed, and assign them to a client.': '\u062D\u064F\u0641\u0638 \u0628\u0631\u0646\u0627\u0645\u062C \u0627\u0644\u062A\u062F\u0631\u064A\u0628 \u0648\u062E\u0637\u0629 \u0627\u0644\u0648\u062C\u0628\u0627\u062A \u0643\u0645\u0633\u0648\u062F\u062A\u064A\u0646. \u0627\u0641\u062A\u062D\u0647\u0645\u0627 \u0644\u0645\u0631\u0627\u062C\u0639\u0629 \u0627\u0644\u062A\u0641\u0627\u0635\u064A\u0644\u060C \u0648\u0623\u0636\u0641 \u062A\u0645\u0627\u0631\u064A\u0646 \u0625\u0630\u0627 \u0644\u0632\u0645\u060C \u062B\u0645 \u0623\u0633\u0646\u062F\u0647\u0645\u0627 \u0644\u0639\u0645\u064A\u0644.',
  'Manage training program': '\u0625\u062F\u0627\u0631\u0629 \u0628\u0631\u0646\u0627\u0645\u062C \u0627\u0644\u062A\u062F\u0631\u064A\u0628', 'Manage meal plan': '\u0625\u062F\u0627\u0631\u0629 \u062E\u0637\u0629 \u0627\u0644\u0648\u062C\u0628\u0627\u062A',
  'Delete this draft?': '\u0647\u0644 \u062A\u0631\u064A\u062F \u062D\u0630\u0641 \u0647\u0630\u0647 \u0627\u0644\u0645\u0633\u0648\u062F\u0629\u061F',   'Draft changes saved.': '\\u062A\\u0645 \\u062D\\u0641\\u0638 \\u062A\\u0639\\u062F\\u064A\\u0644\\u0627\\u062A \\u0627\\u0644\\u0645\\u0633\\u0648\\u062F\\u0629.', 'A new draft was created. The original draft is preserved.': '\\u062A\\u0645 \\u0625\\u0646\\u0634\\u0627\\u0621 \\u0645\\u0633\\u0648\\u062F\\u0629 \\u062C\\u062F\\u064A\\u062F\\u0629\\u060C \\u0648\\u0627\\u0644\\u0645\\u0633\\u0648\\u062F\\u0629 \\u0627\\u0644\\u0623\\u0635\\u0644\\u064A\\u0629 \\u0645\\u0627\\u0632\\u0627\\u0644\\u062A \\u0645\\u062D\\u0641\\u0648\\u0645\\u0629.', 'Approved. A draft training program and meal plan were created. They still need to be assigned to a client.': '\\u062A\\u0645 \\u0627\\u0644\\u0627\\u0639\\u062A\\u0645\\u0627\\u062F. \\u0623\\u064F\\u0646\\u0634\\u0626\\u062A \\u0645\\u0633\\u0648\\u062F\\u0629 \\u0644\\u0644\\u062A\\u062F\\u0631\\u064A\\u0628 \\u0648\\u0623\\u062E\\u0631\\u0649 \\u0644\\u0644\\u0648\\u062C\\u0628\\u0627\\u062A. \\u0623\\u0633\\u0646\\u062F\\u0647\\u0645\\u0627 \\u0644\\u0639\\u0645\\u064A\\u0644 \\u0644\\u0628\\u062F\\u0621 \\u0627\\u0644\\u0627\\u0633\\u062A\\u062E\\u062F\\u0627\\u0645.', 'Draft rejected.': '\\u062A\\u0645 \\u0631\\u0641\\u0636 \\u0627\\u0644\\u0645\\u0633\\u0648\\u062F\\u0629.', 'Progress summary created.': '\\u062A\\u0645 \\u0625\\u0646\\u0634\\u0627\\u0621 \\u0645\\u0644\\u062E\\u0635 \\u0627\\u0644\\u062A\\u0642\\u062F\\u0645.',   'Select a draft above.': '\u0627\u062E\u062A\u0631 \u0645\u0633\u0648\u062F\u0629 \u0645\u0646 \u0627\u0644\u0623\u0639\u0644\u0649.', 'Client progress summary': '\u0645\u0644\u062E\u0635 \u062A\u0642\u062F\u0645 \u0627\u0644\u0639\u0645\u064A\u0644', 'Choose a client': '\u0627\u062E\u062A\u0631 \u0639\u0645\u064A\u0644\u0627\u064B', 'Coach notes': '\u0645\u0644\u0627\u062D\u0638\u0627\u062A \u0627\u0644\u0645\u062F\u0631\u0628', 'Generate summary': '\u0625\u0646\u0634\u0627\u0621 \u0627\u0644\u0645\u0644\u062E\u0635', 'Churn risk history': '\u0633\u062C\u0644 \u0645\u062E\u0627\u0637\u0631 \u0627\u0644\u0645\u063A\u0627\u062F\u0631\u0629', 'Select a client to see historical scores.': '\u0627\u062E\u062A\u0631 \u0639\u0645\u064A\u0644\u0627\u064B \u0644\u0639\u0631\u0636 \u062A\u0627\u0631\u064A\u062E \u0627\u0644\u062E\u0637\u0631.'
})
Object.assign(arabic, {
  'Exercise library': '\u0645\u0643\u062A\u0628\u0629 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646',
  'Find, add, and maintain the exercises used in your training programs.': '\u0627\u0628\u062D\u062B \u0639\u0646 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646 \u0627\u0644\u0645\u0633\u062A\u062E\u062F\u0645\u0629 \u0641\u064A \u0628\u0631\u0627\u0645\u062C\u0643 \u0648\u0623\u0636\u0641\u0647\u0627 \u0648\u062D\u062F\u0651\u062B\u0647\u0627.',
  'Filter exercises': '\u062A\u0635\u0641\u064A\u0629 \u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646', 'Narrow the library by muscle group, equipment, or name.': '\u062D\u062F\u062F \u0627\u0644\u0646\u062A\u0627\u0626\u062C \u0628\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\u064A\u0629 \u0623\u0648 \u0627\u0644\u0645\u0639\u062F\u0627\u062A \u0623\u0648 \u0627\u0633\u0645 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.',
  'Muscle groups': '\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0627\u062A \u0627\u0644\u0639\u0636\u0644\u064A\u0629', 'Equipment types': '\u0623\u0646\u0648\u0627\u0639 \u0627\u0644\u0645\u0639\u062F\u0627\u062A',
  'Search exercises': '\u0627\u0644\u0628\u062D\u062B \u0639\u0646 \u062A\u0645\u0631\u064A\u0646', 'Search by exercise name': '\u0627\u0628\u062D\u062B \u0628\u0627\u0633\u0645 \u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Muscle group': '\u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0629 \u0627\u0644\u0639\u0636\u0644\u064A\u0629', 'All muscle groups': '\u0643\u0644 \u0627\u0644\u0645\u062C\u0645\u0648\u0639\u0627\u062A \u0627\u0644\u0639\u0636\u0644\u064A\u0629', 'Equipment': '\u0627\u0644\u0645\u0639\u062F\u0627\u062A', 'All equipment': '\u0643\u0644 \u0627\u0644\u0645\u0639\u062F\u0627\u062A',
  'Add an exercise': '\u0625\u0636\u0627\u0641\u0629 \u062A\u0645\u0631\u064A\u0646', 'Add a name and muscle group. An HTTPS video link is optional.': '\u0623\u062F\u062E\u0644 \u0627\u0633\u0645 \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0648\u0645\u062C\u0645\u0648\u0639\u062A\u0647 \u0627\u0644\u0639\u0636\u0644\u064A\u0629. \u0631\u0627\u0628\u0637 \u0627\u0644\u0641\u064A\u062F\u064A\u0648 \u0627\u062E\u062A\u064A\u0627\u0631\u064A \u0648\u064A\u062C\u0628 \u0623\u0646 \u064A\u0628\u062F\u0623 \u0628\u0640 HTTPS.',
  'Exercise name': '\u0627\u0633\u0645 \u0627\u0644\u062A\u0645\u0631\u064A\u0646', 'Video link (optional)': '\u0631\u0627\u0628\u0637 \u0641\u064A\u062F\u064A\u0648 (\u0627\u062E\u062A\u064A\u0627\u0631\u064A)', 'Add exercise': '\u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u062A\u0645\u0631\u064A\u0646',
  'Exercise added to your library.': '\u0623\u064F\u0636\u064A\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646 \u0625\u0644\u0649 \u0627\u0644\u0645\u0643\u062A\u0628\u0629.', 'Could not add the exercise.': '\u062A\u0639\u0630\u0631\u062A \u0625\u0636\u0627\u0641\u0629 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.', 'Exercise details saved.': '\u062A\u0645 \u062D\u0641\u0638 \u062A\u0639\u062F\u064A\u0644\u0627\u062A \u0627\u0644\u062A\u0645\u0631\u064A\u0646.', 'Could not save the exercise.': '\u062A\u0639\u0630\u0631 \u062D\u0641\u0638 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.', 'Exercise deleted.': '\u062A\u0645 \u062D\u0630\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.', 'Could not delete the exercise.': '\u062A\u0639\u0630\u0631 \u062D\u0630\u0641 \u0627\u0644\u062A\u0645\u0631\u064A\u0646.',
  'Exercises': '\u0627\u0644\u062A\u0645\u0627\u0631\u064A\u0646', 'Select an exercise to view its video and edit its details.': '\u0627\u062E\u062A\u0631 \u062A\u0645\u0631\u064A\u0646\u0627\u064B \u0644\u0645\u0634\u0627\u0647\u062F\u0629 \u0631\u0627\u0628\u0637 \u0627\u0644\u0641\u064A\u062F\u064A\u0648 \u0648\u062A\u0639\u062F\u064A\u0644 \u062A\u0641\u0627\u0635\u064A\u0644\u0647.', 'exercises': '\u062A\u0645\u0627\u0631\u064A\u0646', 'No exercises match these filters.': '\u0644\u0627 \u062A\u0648\u062C\u062F \u062A\u0645\u0627\u0631\u064A\u0646 \u062A\u0637\u0627\u0628\u0642 \u0647\u0630\u0647 \u0627\u0644\u0641\u0644\u0627\u062A\u0631.', 'No muscle group selected': '\u0644\u0645 \u062A\u062D\u062F\u062F \u0645\u062C\u0645\u0648\u0639\u0629 \u0639\u0636\u0644\u064A\u0629', 'No equipment specified': '\u0644\u0627 \u062A\u0648\u062C\u062F \u0645\u0639\u062F\u0627\u062A \u0645\u062D\u062F\u062F\u0629', 'Video available': '\u064A\u062A\u0648\u0641\u0631 \u0641\u064A\u062F\u064A\u0648', 'Previous': '\u0627\u0644\u0633\u0627\u0628\u0642', 'Next': '\u0627\u0644\u062A\u0627\u0644\u064A',
  'Exercise details': '\u062A\u0641\u0627\u0635\u064A\u0644 \u0627\u0644\u062A\u0645\u0631\u064A\u0646', 'Update its information, video link, or remove it from the library.': '\u062D\u062F\u0651\u062B \u0628\u064A\u0627\u0646\u0627\u062A\u0647 \u0623\u0648 \u0631\u0627\u0628\u0637 \u0627\u0644\u0641\u064A\u062F\u064A\u0648\u060C \u0623\u0648 \u0627\u062D\u0630\u0641\u0647 \u0645\u0646 \u0627\u0644\u0645\u0643\u062A\u0628\u0629.', 'Close details': '\u0625\u063A\u0644\u0627\u0642 \u0627\u0644\u062A\u0641\u0627\u0635\u064A\u0644', 'Delete this exercise?': '\u0647\u0644 \u062A\u0631\u064A\u062F \u062D\u0630\u0641 \u0647\u0630\u0627 \u0627\u0644\u062A\u0645\u0631\u064A\u0646\u061F'
})
Object.assign(arabic, {
  'No video link': '\u0644\u0627 \u064A\u0648\u062C\u062F \u0631\u0627\u0628\u0637 \u0641\u064A\u062F\u064A\u0648',
  'Selected': '\u0645\u062D\u062F\u062F',
  'View details': '\u0639\u0631\u0636 \u0627\u0644\u062A\u0641\u0627\u0635\u064A\u0644'
})
Object.assign(arabic, {
  'Muscle': '\u0627\u0644\u0639\u0636\u0644\u0629',
  'Not specified': '\u063A\u064A\u0631 \u0645\u062D\u062F\u062F'
})
