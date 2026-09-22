USE `sales_management`;

SET FOREIGN_KEY_CHECKS = 0;

-- 1. ROLES
INSERT INTO `roles` (`id`, `name`, `display_name_ar`, `display_name_fr`, `description`, `is_system`) VALUES
(1, 'Administrator', 'مدير النظام (كامل الصلاحيات)', 'Administrateur', 'صلاحيات كاملة وغير محدودة للنظام', TRUE),
(2, 'Manager', 'مدير المبيعات والمخزون', 'Gérant Commercial', 'إدارة المبيعات والمشتريات والمخزون والتقارير', TRUE),
(3, 'Salesperson', 'بائع / كاشير', 'Caissier / Vendeur', 'نقطة البيع والعملاء واستخراج الفواتير', TRUE),
(4, 'WarehouseKeeper', 'أمين المخزن', 'Magasinier', 'استلام البضائع والتحويلات والجرد', TRUE),
(5, 'Accountant', 'محاسب مالي', 'Comptable', 'إدارة المقبوضات والمدفوعات والمصاريف والتقارير المالية', TRUE);

-- 2. PERMISSIONS
INSERT INTO `permissions` (`code`, `module`, `name_ar`, `name_fr`, `description`) VALUES
-- Users & Roles
('USERS_VIEW', 'Users', 'عرض المستخدمين', 'Voir Utilisateurs', 'الاطلاع على قائمة المستخدمين'),
('USERS_MANAGE', 'Users', 'إدارة المستخدمين', 'Gérer Utilisateurs', 'إضافة وتعديل وحذف المستخدمين وتعيين الأدوار'),
('ROLES_MANAGE', 'Roles', 'إدارة الأدوار والصلاحيات', 'Gérer Rôles', 'تعديل صلاحيات الأدوار في النظام'),
-- Products & Catalog
('PRODUCTS_VIEW', 'Products', 'عرض المنتجات', 'Voir Produits', 'استعراض قائمة المنتجات والأسعار'),
('PRODUCTS_CREATE', 'Products', 'إضافة منتج', 'Ajouter Produit', 'إدخال أصناف ومنتجات جديدة'),
('PRODUCTS_EDIT', 'Products', 'تعديل منتج', 'Modifier Produit', 'تعديل الأسعار والبيانات وتكلفة الشراء'),
('PRODUCTS_DELETE', 'Products', 'حذف/تعطيل منتج', 'Supprimer Produit', 'حذف أو إلغاء تفعيل المنتجات'),
-- Warehouses & Stock
('STOCK_VIEW', 'Inventory', 'عرض المخزون', 'Voir Stock', 'متابعة كميات المخزون وحركاته'),
('STOCK_TRANSFER', 'Inventory', 'تحويل بين المستودعات', 'Transfert Stock', 'إنشاء أوامر تحويل بين المستودعات'),
('STOCK_ADJUST', 'Inventory', 'تعديل المخزون والجرد', 'Ajustement Stock', 'تسجيل الجرد وتعديل الفروقات'),
-- Sales & POS
('POS_ACCESS', 'POS', 'استخدام نقطة البيع (POS)', 'Accès Caisse POS', 'فتح شاشة الكاشير وتسجيل المبيعات'),
('SALES_VIEW', 'Sales', 'عرض سجل المبيعات', 'Voir Ventes', 'الاطلاع على فواتير المبيعات السابقة'),
('SALES_DISCOUNT', 'Sales', 'منح تخفيض في الفاتورة', 'Accorder Remise', 'إمكانية إعطاء خصم تجاري للزبون'),
('SALES_CANCEL', 'Sales', 'إلغاء فاتورة بيع', 'Annuler Vente', 'إلغاء فاتورة مبيعات معتمدة'),
('SALES_RETURN', 'Sales', 'تسجيل مرتجع مبيعات', 'Retour Vente', 'معالجة إرجاع البضائع واسترجاع المبالغ'),
-- Purchases
('PURCHASES_VIEW', 'Purchases', 'عرض المشتريات', 'Voir Achats', 'الاطلاع على فواتير وسندات الموردين'),
('PURCHASES_MANAGE', 'Purchases', 'إدارة المشتريات', 'Gérer Achats', 'تسجيل فواتير الشراء وسندات التوريد'),
('PURCHASES_RETURN', 'Purchases', 'تسجيل مرتجع مشتريات', 'Retour Achat', 'إرجاع بضاعة إلى المورد'),
-- Partners
('CUSTOMERS_MANAGE', 'Customers', 'إدارة الزبائن', 'Gérer Clients', 'إضافة وتعديل الزبائن ومتابعة الديون'),
('SUPPLIERS_MANAGE', 'Suppliers', 'إدارة الممونين', 'Gérer Fournisseurs', 'إدارة الموردين ومستحقاتهم'),
-- Financials
('EXPENSES_MANAGE', 'Expenses', 'إدارة المصاريف', 'Gérer Dépenses', 'تسجيل المصاريف التشغيلية والإدارية'),
('CASH_REGISTER_CLOSE', 'Cash', 'إغلاق الصندوق (Rapport Z)', 'Clôture Caisse Z', 'إغلاق يومية الكاشير ومطابقة النقدية'),
('REPORTS_VIEW', 'Reports', 'عرض التقارير والأرباح', 'Voir Rapports & Profits', 'الاطلاع على التقارير المالية والأرباح الصافية'),
('BACKUP_MANAGE', 'System', 'النسخ الاحتياطي والاستعادة', 'Sauvegarde & Restauration', 'إنشاء واستعادة النسخ الاحتياطية لـ MySQL'),
('SETTINGS_MANAGE', 'System', 'إدارة إعدادات النظام', 'Gérer Paramètres', 'تخصيص بيانات المتجر والطباعة');

-- Assign all permissions to Administrator
INSERT INTO `role_permissions` (`role_id`, `permission_id`)
SELECT 1, `id` FROM `permissions`;

-- Assign manager permissions
INSERT INTO `role_permissions` (`role_id`, `permission_id`)
SELECT 2, `id` FROM `permissions`
WHERE `code` NOT IN ('USERS_MANAGE', 'ROLES_MANAGE', 'BACKUP_MANAGE');

-- Assign salesperson permissions
INSERT INTO `role_permissions` (`role_id`, `permission_id`)
SELECT 3, `id` FROM `permissions`
WHERE `code` IN ('POS_ACCESS', 'SALES_VIEW', 'PRODUCTS_VIEW', 'CUSTOMERS_MANAGE', 'SALES_RETURN');

-- 3. DEFAULT USERS
-- Default admin password: "Admin@123456" (BCrypt hash with work factor 12)
INSERT INTO `users` (`id`, `username`, `password_hash`, `full_name`, `email`, `phone`, `role_id`, `is_active`) VALUES
(1, 'admin', '$2a$12$w3d5U8HkI11m7QdFv4Xz1u2pG4R3vXkXf5bV3zKz9zC3dG5xK7Y1e', 'المدير العام للمؤسسة', 'admin@store.dz', '0550 12 34 56', 1, TRUE),
(2, 'manager', '$2a$12$w3d5U8HkI11m7QdFv4Xz1u2pG4R3vXkXf5bV3zKz9zC3dG5xK7Y1e', 'مسؤول المبيعات والتوريد', 'manager@store.dz', '0661 22 33 44', 2, TRUE),
(3, 'cashier1', '$2a$12$w3d5U8HkI11m7QdFv4Xz1u2pG4R3vXkXf5bV3zKz9zC3dG5xK7Y1e', 'كاشير - نقطة بيع 1', 'cashier1@store.dz', '0770 99 88 77', 3, TRUE);

-- 4. UNITS
INSERT INTO `units` (`id`, `code`, `name_ar`, `name_fr`, `allow_decimal`) VALUES
(1, 'PCS', 'قطعة / وحدة', 'Pièce', FALSE),
(2, 'BOX', 'علبة', 'Boîte', FALSE),
(3, 'CTN', 'كرتونة', 'Carton', FALSE),
(4, 'KG', 'كيلوغرام', 'Kilogramme', TRUE),
(5, 'LTR', 'لتر', 'Litre', TRUE),
(6, 'MTR', 'متر', 'Mètre', TRUE);

-- 5. CATEGORIES
INSERT INTO `categories` (`id`, `code`, `name_ar`, `name_fr`, `name_en`, `parent_id`) VALUES
(1, 'CAT-FOOD', 'مواد غذائية عامة', 'Alimentation Générale', 'General Food', NULL),
(2, 'CAT-DRINKS', 'مشروبات وعصائر', 'Boissons & Jus', 'Drinks & Juices', NULL),
(3, 'CAT-DAIRY', 'ألبان وأجبان', 'Produits Laitiers', 'Dairy Products', NULL),
(4, 'CAT-SWEETS', 'حلويات وبسكويت', 'Biscuiterie & Chocolat', 'Sweets & Biscuits', NULL),
(5, 'CAT-CLEAN', 'منظفات ومستلزمات منزلية', 'Produits d Entretien', 'Cleaning & Household', NULL);

-- 6. BRANDS
INSERT INTO `brands` (`id`, `name`, `origin_country`) VALUES
(1, 'سيفيتال (Cevital)', 'الجزائر'),
(2, 'صومام (Soummam)', 'الجزائر'),
(3, 'حمود بوعلام (Hamoud Boualem)', 'الجزائر'),
(4, 'الرويبة (NCA Rouiba)', 'الجزائر'),
(5, 'كانديا (Candia Tchin-Lait)', 'الجزائر'),
(6, 'إفروشان (Ifri)', 'الجزائر'),
(7, 'أوريو (Oreo - Mondelēz)', 'الجزائر'),
(8, 'أريال (Ariel)', 'الجزائر');

-- 7. WAREHOUSES
INSERT INTO `warehouses` (`id`, `code`, `name`, `location`, `manager_name`, `is_primary`, `is_active`) VALUES
(1, 'WH-MAIN', 'المستودع الرئيسي المركزي', 'المنطقة الصناعية، الخروب', 'عبد القادر بن عيسى', TRUE, TRUE),
(2, 'WH-STORE', 'مخزن صالة العرض والمحل', 'شارع الاستقلال، وسط المدينة', 'مراد شريف', FALSE, TRUE);

-- 8. CASH REGISTERS
INSERT INTO `cash_registers` (`id`, `code`, `name`, `assigned_warehouse_id`, `current_balance`, `is_open`, `opening_float`) VALUES
(1, 'POS-01', 'صندوق الكاشير الرئيسي 01', 2, 10000.0000, TRUE, 10000.0000),
(2, 'POS-02', 'صندوق الكاشير الاحتياطي 02', 2, 0.0000, FALSE, 0.0000);

-- 9. PRODUCTS
INSERT INTO `products` (`id`, `sku`, `barcode`, `name_ar`, `name_fr`, `category_id`, `brand_id`, `unit_id`, `purchase_price`, `sale_price`, `wholesale_price`, `minimum_stock`, `tax_percent`) VALUES
(1, 'SKU-OIL-01', '6130001001011', 'زيت المائدة إيليو 5 لتر (Elio)', 'Huile Elio 5L', 1, 1, 1, 600.0000, 650.0000, 630.0000, 20.0000, 0.00),
(2, 'SKU-OIL-02', '6130001001028', 'زيت زيتون بكر ممتاز بلدي 1 لتر', 'Huile d Olive Vierge 1L', 1, 1, 1, 900.0000, 1200.0000, 1100.0000, 10.0000, 0.00),
(3, 'SKU-MILK-01', '6130001001035', 'حليب كانديا معقم كامل الدسم 1 لتر', 'Lait Candia UHT 1L', 3, 5, 1, 100.0000, 130.0000, 120.0000, 30.0000, 0.00),
(4, 'SKU-DAIRY-02', '6130001001042', 'ياغورت ممزوج صومام 100 غرام', 'Yaourt Soummam Brassé', 3, 2, 1, 20.0000, 30.0000, 25.0000, 50.0000, 0.00),
(5, 'SKU-DRK-01', '6130001001059', 'مشروب غازي سيلكتو حمود 1 لتر', 'Selecto Hamoud 1L', 2, 3, 1, 85.0000, 110.0000, 98.0000, 24.0000, 0.00),
(6, 'SKU-DRK-02', '6130001001066', 'عصير الرويبة برتقال وجزر 1 لتر', 'Jus Rouiba Orange Carotte 1L', 2, 4, 1, 140.0000, 190.0000, 175.0000, 15.0000, 0.00),
(7, 'SKU-WATER-01', '6130001001073', 'ماء معدني طبيعي إفري 1.5 لتر', 'Eau Minérale Ifri 1.5L', 2, 6, 1, 35.0000, 50.0000, 42.0000, 60.0000, 0.00),
(8, 'SKU-SWT-01', '6130001001080', 'بسكويت أوريو شوكولاتة علبة', 'Biscuits Oreo Boîte', 4, 7, 2, 120.0000, 160.0000, 145.0000, 20.0000, 0.00),
(9, 'SKU-CLN-01', '6130001001097', 'مسحوق غسيل أوتوماتيك أريال 3 كلغ', 'Lessive Ariel 3KG', 5, 8, 1, 750.0000, 950.0000, 890.0000, 10.0000, 0.00);

-- 10. INITIAL STOCK IN WAREHOUSE 2 (STORE)
INSERT INTO `warehouse_products` (`warehouse_id`, `product_id`, `current_quantity`, `reserved_quantity`) VALUES
(2, 1, 45.0000, 0.0000),
(2, 2, 25.0000, 0.0000),
(2, 3, 80.0000, 0.0000),
(2, 4, 120.0000, 0.0000),
(2, 5, 60.0000, 0.0000),
(2, 6, 35.0000, 0.0000),
(2, 7, 150.0000, 0.0000),
(2, 8, 40.0000, 0.0000),
(2, 9, 18.0000, 0.0000);

-- Initial Stock Movement records
INSERT INTO `stock_movements` (`product_id`, `warehouse_id`, `movement_type`, `quantity_change`, `resulting_quantity`, `unit_cost`, `reference_document_type`, `notes`, `user_id`)
SELECT `product_id`, `warehouse_id`, 'InitialBalance', `current_quantity`, `current_quantity`, 0.0000, 'SystemInit', 'الرصيد الافتتاحي للنظام', 1
FROM `warehouse_products`;

-- 11. CUSTOMERS
INSERT INTO `customers` (`id`, `code`, `name`, `company_name`, `phone`, `email`, `address`, `tax_number`, `credit_limit`, `current_debt`, `notes`) VALUES
(1, 'CUST-001', 'زبون عابر (نقدي)', NULL, NULL, NULL, 'نقطة البيع المباشرة', NULL, 0.0000, 0.0000, 'حساب الزبائن العابرين للمبيعات النقدية الفورية'),
(2, 'CUST-002', 'محمد بلقاسم', 'مقهى الأمل', '0661 22 33 44', 'belkacem@gmail.com', 'حي النصر، عمارة 4، قسنطينة', '18920038841', 50000.0000, 8500.0000, 'زبون دائم، تسديد أسبوعي كل يوم خميس'),
(3, 'CUST-003', 'مطعم وقاعة السعادة', 'SARL Saada Resto', '0770 55 66 77', 'resto.saada@yahoo.fr', 'شارع جيش التحرير، وسط المدينة', '000216098234855', 100000.0000, 24000.0000, 'طلبيات مواد غذائية وزيوت نصف شهرية'),
(4, 'CUST-004', 'أمينة شريف', NULL, '0555 88 99 00', 'amina.cherif@outlook.com', 'حي الزهور، فيلا 12، الخروب', NULL, 20000.0000, 0.0000, 'تسديد فوري عند الاستلام');

-- 12. SUPPLIERS
INSERT INTO `suppliers` (`id`, `code`, `name`, `company_name`, `phone`, `email`, `address`, `tax_number`, `current_debt`, `notes`) VALUES
(1, 'SUPP-001', 'مجمع سيفيتال للصناعات الغذائية', 'Cevital Agro-Alimentaire', '034 21 22 23', 'contact@cevital.com', 'المنطقة الصناعية، ميناء بجاية', '000016001234567', 45000.0000, 'توريد زيوت المائدة والمواد الأساسية'),
(2, 'SUPP-002', 'ملبنة وادي الصومام', 'Laiterie Soummam', '034 35 11 22', 'commercial@soummam.dz', 'المنطقة الصناعية، أقبو - بجاية', '000206019876543', 18500.0000, 'توريد منتجات الحليب والألبان مرتين أسبوعياً'),
(3, 'SUPP-003', 'مؤسسة النور لتوزيع المواد الغذائية بالجملة', 'EURL Ennour Distribution', '031 92 44 55', 'ennour.dist@gmail.com', 'المنطقة الصناعية، الخروب - قسنطينة', '000925012398456', 0.0000, 'موزع جملة معتمد لمختلف المواد الغذائية والمنظفات'),
(4, 'SUPP-004', 'شركة الرويبة للمشروبات والعصائر', 'NCA Rouiba', '021 81 12 34', 'ventes@nca-rouiba.com', 'المنطقة الصناعية، الرويبة - الجزائر', '000116045678912', 12000.0000, 'عصائر ومشروبات طبيعية');

-- 13. EXPENSE CATEGORIES
INSERT INTO `expense_categories` (`id`, `name_ar`, `name_fr`, `description`) VALUES
(1, 'إيجار المحل والمستودع', 'Loyer Magasin & Dépôt', 'تكاليف إيجار العقارات الشهرية'),
(2, 'كهرباء وغاز ومياه', 'Électricité, Gaz & Eau', 'فواتير سونلغاز وشركة المياه'),
(3, 'رواتب وأجور العمال', 'Salaires & Rémunérations', 'رواتب الموظفين والعمال اليومية والشهرية'),
(4, 'نقل ومحروقات', 'Transport & Carburant', 'مصاريف توزيع البضائع ووقود الشاحنات'),
(5, 'صيانة وإصلاحات', 'Maintenance & Réparations', 'صيانة ثلاجات ومعدات ومكيفات المحل'),
(6, 'أدوات تغليف ومطبوعات', 'Emballage & Fournitures', 'أكياس بلاستيكية وفواتير ورقية وأشرطة حرارية'),
(7, 'مصاريف ضريبية وبنكية', 'Frais Fiscaux & Bancaires', 'رسوم وعمولات الحسابات البنكية والضرائب'),
(8, 'مصاريف عامة أخرى', 'Autres Dépenses Diverses', 'مصاريف طارئة وضيافة');

-- 14. SETTINGS
INSERT INTO `settings` (`setting_key`, `setting_value`, `category`, `description`) VALUES
('Enterprise.Name', 'مؤسسة النور للتجارة والتوزيع', 'Enterprise', 'اسم المؤسسة أو المحل التجاري'),
('Enterprise.Activity', 'تجارة عامة بالجملة والتجزئة ومواد غذائية', 'Enterprise', 'نوع النشاط التجاري'),
('Enterprise.Address', 'شارع الاستقلال رقم 45، الخروب - قسنطينة', 'Enterprise', 'عنوان المؤسسة الرئيسي'),
('Enterprise.Phone', '0550 12 34 56 / 031 92 11 22', 'Enterprise', 'أرقام الهاتف للتواصل'),
('Enterprise.Email', 'contact@ennour-pos.dz', 'Enterprise', 'البريد الإلكتروني الرسمي'),
('Enterprise.TaxId', '000216091234567', 'Enterprise', 'رقم التعريف الجبائي (NIF)'),
('Enterprise.TradeRegister', '25/00-0987654B16', 'Enterprise', 'رقم السجل التجاري (RC)'),
('Enterprise.CurrencySymbol', 'دج', 'Enterprise', 'رمز العملة الرئيسي'),
('Enterprise.CurrencyCode', 'DZD', 'Enterprise', 'كود العملة الدولي (الدينار الجزائري)'),
('Enterprise.ReceiptFooter', 'شكراً لزيارتكم! البضاعة المباعة ترد أو تستبدل خلال 48 ساعة مع إحضار الفاتورة.', 'Enterprise', 'تذييل الفاتورة النقطية'),
('POS.DefaultWarehouseId', '2', 'POS', 'المستودع الافتراضي لنقطة البيع'),
('POS.DefaultCashRegisterId', '1', 'POS', 'الصندوق الافتراضي للنظام'),
('POS.PrinterType', 'Thermal80mm', 'Printing', 'نوع الطابعة الافتراضية (Thermal80mm, Thermal58mm, A4)'),
('POS.AutoPrintAfterSale', 'true', 'Printing', 'الطباعة التلقائية فور تأكيد عملية البيع'),
('Backup.AutoScheduleEnabled', 'true', 'Backup', 'تفعيل النسخ الاحتياطي التلقائي عند إغلاق النظام'),
('Backup.DirectoryPath', 'C:\\SalesManagement_Backups', 'Backup', 'مسار مجلد حفظ النسخ الاحتياطية لـ MySQL');

SET FOREIGN_KEY_CHECKS = 1;
