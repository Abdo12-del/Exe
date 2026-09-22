-- =============================================================================
-- Database Schema for Sales & Inventory Management System (Desktop WPF / .NET 8)
-- Target DBMS: MySQL 8.0+
-- Storage Engine: InnoDB | Character Set: utf8mb4 | Collation: utf8mb4_unicode_ci
-- =============================================================================

CREATE DATABASE IF NOT EXISTS `sales_management`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `sales_management`;

SET FOREIGN_KEY_CHECKS = 0;

-- -----------------------------------------------------------------------------
-- 1. SECURITY & ACCESS CONTROL (RBAC)
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `role_permissions`;
DROP TABLE IF EXISTS `permissions`;
DROP TABLE IF EXISTS `users`;
DROP TABLE IF EXISTS `roles`;

CREATE TABLE `roles` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(50) NOT NULL UNIQUE,
    `display_name_ar` VARCHAR(100) NOT NULL,
    `display_name_fr` VARCHAR(100) NULL,
    `description` VARCHAR(255) NULL,
    `is_system` BOOLEAN NOT NULL DEFAULT FALSE,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

CREATE TABLE `permissions` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `code` VARCHAR(80) NOT NULL UNIQUE,
    `module` VARCHAR(50) NOT NULL,
    `name_ar` VARCHAR(100) NOT NULL,
    `name_fr` VARCHAR(100) NULL,
    `description` VARCHAR(255) NULL
) ENGINE=InnoDB;

CREATE TABLE `role_permissions` (
    `role_id` INT NOT NULL,
    `permission_id` INT NOT NULL,
    PRIMARY KEY (`role_id`, `permission_id`),
    CONSTRAINT `fk_rp_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_rp_permission` FOREIGN KEY (`permission_id`) REFERENCES `permissions` (`id`) ON DELETE CASCADE
) ENGINE=InnoDB;

CREATE TABLE `users` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `username` VARCHAR(50) NOT NULL UNIQUE,
    `password_hash` VARCHAR(255) NOT NULL,
    `full_name` VARCHAR(120) NOT NULL,
    `email` VARCHAR(100) NULL,
    `phone` VARCHAR(30) NULL,
    `role_id` INT NOT NULL,
    `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `last_login` DATETIME NULL,
    CONSTRAINT `fk_users_role` FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 2. CATALOG: CATEGORIES, BRANDS, UNITS, PRODUCTS
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `products`;
DROP TABLE IF EXISTS `units`;
DROP TABLE IF EXISTS `brands`;
DROP TABLE IF EXISTS `categories`;

CREATE TABLE `categories` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `code` VARCHAR(30) NOT NULL UNIQUE,
    `name_ar` VARCHAR(100) NOT NULL,
    `name_fr` VARCHAR(100) NULL,
    `name_en` VARCHAR(100) NULL,
    `parent_id` INT NULL,
    `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT `fk_categories_parent` FOREIGN KEY (`parent_id`) REFERENCES `categories` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE `brands` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(100) NOT NULL UNIQUE,
    `origin_country` VARCHAR(50) NULL,
    `is_active` BOOLEAN NOT NULL DEFAULT TRUE
) ENGINE=InnoDB;

CREATE TABLE `units` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `code` VARCHAR(20) NOT NULL UNIQUE,
    `name_ar` VARCHAR(50) NOT NULL,
    `name_fr` VARCHAR(50) NULL,
    `allow_decimal` BOOLEAN NOT NULL DEFAULT FALSE
) ENGINE=InnoDB;

CREATE TABLE `products` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `sku` VARCHAR(50) NOT NULL UNIQUE,
    `barcode` VARCHAR(80) NOT NULL UNIQUE,
    `name_ar` VARCHAR(150) NOT NULL,
    `name_fr` VARCHAR(150) NULL,
    `name_en` VARCHAR(150) NULL,
    `category_id` INT NOT NULL,
    `brand_id` INT NULL,
    `unit_id` INT NOT NULL,
    `purchase_price` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `sale_price` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `wholesale_price` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `minimum_stock` DECIMAL(18, 4) NOT NULL DEFAULT 5.0000,
    `tax_percent` DECIMAL(5, 2) NOT NULL DEFAULT 0.00,
    `description` TEXT NULL,
    `image_path` VARCHAR(255) NULL,
    `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX `idx_products_barcode` (`barcode`),
    INDEX `idx_products_sku` (`sku`),
    INDEX `idx_products_name_ar` (`name_ar`),
    CONSTRAINT `fk_products_category` FOREIGN KEY (`category_id`) REFERENCES `categories` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_products_brand` FOREIGN KEY (`brand_id`) REFERENCES `brands` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_products_unit` FOREIGN KEY (`unit_id`) REFERENCES `units` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 3. WAREHOUSES & STOCK MANAGEMENT (MOVEMENT-BASED)
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `stock_movements`;
DROP TABLE IF EXISTS `warehouse_products`;
DROP TABLE IF EXISTS `warehouses`;

CREATE TABLE `warehouses` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `code` VARCHAR(30) NOT NULL UNIQUE,
    `name` VARCHAR(100) NOT NULL,
    `location` VARCHAR(200) NULL,
    `manager_name` VARCHAR(100) NULL,
    `is_primary` BOOLEAN NOT NULL DEFAULT FALSE,
    `is_active` BOOLEAN NOT NULL DEFAULT TRUE
) ENGINE=InnoDB;

CREATE TABLE `warehouse_products` (
    `warehouse_id` INT NOT NULL,
    `product_id` INT NOT NULL,
    `current_quantity` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `reserved_quantity` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    PRIMARY KEY (`warehouse_id`, `product_id`),
    CONSTRAINT `fk_wp_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `warehouses` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_wp_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `stock_movements` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `product_id` INT NOT NULL,
    `warehouse_id` INT NOT NULL,
    `movement_type` ENUM(
        'Purchase',
        'Sale',
        'SaleReturn',
        'PurchaseReturn',
        'TransferIn',
        'TransferOut',
        'InventoryAdjustment',
        'DamageWaste',
        'InitialBalance'
    ) NOT NULL,
    `quantity_change` DECIMAL(18, 4) NOT NULL, -- Positive for in, Negative for out
    `resulting_quantity` DECIMAL(18, 4) NOT NULL,
    `unit_cost` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `reference_document_type` VARCHAR(50) NULL, -- e.g. "Sale", "Purchase", "Transfer", "Adjustment"
    `reference_document_id` BIGINT NULL,
    `notes` VARCHAR(255) NULL,
    `user_id` INT NOT NULL,
    INDEX `idx_sm_product` (`product_id`),
    INDEX `idx_sm_warehouse` (`warehouse_id`),
    INDEX `idx_sm_timestamp` (`timestamp`),
    INDEX `idx_sm_ref` (`reference_document_type`, `reference_document_id`),
    CONSTRAINT `fk_sm_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_sm_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `warehouses` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_sm_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 4. PARTNERS: CUSTOMERS & SUPPLIERS
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `suppliers`;
DROP TABLE IF EXISTS `customers`;

CREATE TABLE `customers` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `code` VARCHAR(30) NOT NULL UNIQUE,
    `name` VARCHAR(150) NOT NULL,
    `company_name` VARCHAR(150) NULL,
    `phone` VARCHAR(40) NULL,
    `email` VARCHAR(100) NULL,
    `address` VARCHAR(255) NULL,
    `tax_number` VARCHAR(50) NULL, -- NIF / NIS / RC
    `credit_limit` DECIMAL(18, 4) NOT NULL DEFAULT 50000.0000,
    `current_debt` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `notes` TEXT NULL,
    `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_customers_name` (`name`),
    INDEX `idx_customers_phone` (`phone`)
) ENGINE=InnoDB;

CREATE TABLE `suppliers` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `code` VARCHAR(30) NOT NULL UNIQUE,
    `name` VARCHAR(150) NOT NULL,
    `company_name` VARCHAR(150) NULL,
    `phone` VARCHAR(40) NULL,
    `email` VARCHAR(100) NULL,
    `address` VARCHAR(255) NULL,
    `tax_number` VARCHAR(50) NULL,
    `current_debt` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000, -- Amount we owe the supplier
    `notes` TEXT NULL,
    `is_active` BOOLEAN NOT NULL DEFAULT TRUE,
    `created_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    INDEX `idx_suppliers_name` (`name`),
    INDEX `idx_suppliers_phone` (`phone`)
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 5. CASH REGISTERS & CASH TRANSACTIONS
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `cash_transactions`;
DROP TABLE IF EXISTS `cash_registers`;

CREATE TABLE `cash_registers` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `code` VARCHAR(30) NOT NULL UNIQUE,
    `name` VARCHAR(100) NOT NULL,
    `assigned_warehouse_id` INT NOT NULL,
    `current_balance` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `is_open` BOOLEAN NOT NULL DEFAULT FALSE,
    `opened_at` DATETIME NULL,
    `closed_at` DATETIME NULL,
    `opened_by_user_id` INT NULL,
    `opening_float` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    CONSTRAINT `fk_cr_warehouse` FOREIGN KEY (`assigned_warehouse_id`) REFERENCES `warehouses` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_cr_user` FOREIGN KEY (`opened_by_user_id`) REFERENCES `users` (`id`) ON DELETE SET NULL
) ENGINE=InnoDB;

CREATE TABLE `cash_transactions` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `cash_register_id` INT NOT NULL,
    `user_id` INT NOT NULL,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `transaction_type` ENUM(
        'OpeningFloat',
        'SaleReceipt',
        'SaleRefund',
        'CustomerDebtPayment',
        'SupplierDebtPayment',
        'ExpensePayment',
        'CashInDeposit',
        'CashOutWithdrawal',
        'ClosingBalance'
    ) NOT NULL,
    `amount` DECIMAL(18, 4) NOT NULL, -- Positive adds to drawer, Negative removes from drawer
    `balance_after` DECIMAL(18, 4) NOT NULL,
    `reference_document_type` VARCHAR(50) NULL,
    `reference_document_id` BIGINT NULL,
    `notes` VARCHAR(255) NULL,
    INDEX `idx_ct_register_time` (`cash_register_id`, `timestamp`),
    CONSTRAINT `fk_ct_register` FOREIGN KEY (`cash_register_id`) REFERENCES `cash_registers` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_ct_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 6. SALES, SALE ITEMS, SALE PAYMENTS
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `sale_payments`;
DROP TABLE IF EXISTS `sale_items`;
DROP TABLE IF EXISTS `sales`;

CREATE TABLE `sales` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `invoice_number` VARCHAR(50) NOT NULL UNIQUE,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `user_id` INT NOT NULL,
    `customer_id` INT NOT NULL,
    `warehouse_id` INT NOT NULL,
    `cash_register_id` INT NOT NULL,
    `subtotal` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `discount_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `tax_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `total_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `total_cost` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `net_profit` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `paid_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `remaining_debt` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `payment_status` ENUM('Paid', 'Partial', 'Unpaid') NOT NULL DEFAULT 'Paid',
    `payment_method` ENUM('Cash', 'Card', 'BankTransfer', 'Credit', 'Mixed') NOT NULL DEFAULT 'Cash',
    `notes` VARCHAR(255) NULL,
    `is_cancelled` BOOLEAN NOT NULL DEFAULT FALSE,
    `cancelled_at` DATETIME NULL,
    INDEX `idx_sales_invoice` (`invoice_number`),
    INDEX `idx_sales_time` (`timestamp`),
    INDEX `idx_sales_customer` (`customer_id`),
    CONSTRAINT `fk_sales_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_sales_customer` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_sales_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `warehouses` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_sales_register` FOREIGN KEY (`cash_register_id`) REFERENCES `cash_registers` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `sale_items` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `sale_id` BIGINT NOT NULL,
    `product_id` INT NOT NULL,
    `unit_id` INT NOT NULL,
    `quantity` DECIMAL(18, 4) NOT NULL,
    `unit_sale_price` DECIMAL(18, 4) NOT NULL,
    `unit_cost_price` DECIMAL(18, 4) NOT NULL,
    `discount_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `tax_percent` DECIMAL(5, 2) NOT NULL DEFAULT 0.00,
    `total_line_amount` DECIMAL(18, 4) NOT NULL,
    `line_profit` DECIMAL(18, 4) NOT NULL,
    CONSTRAINT `fk_si_sale` FOREIGN KEY (`sale_id`) REFERENCES `sales` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_si_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_si_unit` FOREIGN KEY (`unit_id`) REFERENCES `units` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `sale_payments` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `sale_id` BIGINT NOT NULL,
    `cash_register_id` INT NOT NULL,
    `user_id` INT NOT NULL,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `payment_method` ENUM('Cash', 'Card', 'BankTransfer', 'Cheque', 'Credit') NOT NULL,
    `amount` DECIMAL(18, 4) NOT NULL,
    `notes` VARCHAR(150) NULL,
    CONSTRAINT `fk_sp_sale` FOREIGN KEY (`sale_id`) REFERENCES `sales` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_sp_register` FOREIGN KEY (`cash_register_id`) REFERENCES `cash_registers` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_sp_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 7. SALES RETURNS
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `sales_return_items`;
DROP TABLE IF EXISTS `sales_returns`;

CREATE TABLE `sales_returns` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `return_number` VARCHAR(50) NOT NULL UNIQUE,
    `original_sale_id` BIGINT NOT NULL,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `user_id` INT NOT NULL,
    `customer_id` INT NOT NULL,
    `warehouse_id` INT NOT NULL,
    `cash_register_id` INT NOT NULL,
    `total_refund_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `refund_method` ENUM('Cash', 'CustomerCreditDeduction', 'BankTransfer') NOT NULL DEFAULT 'Cash',
    `reason` VARCHAR(255) NULL,
    CONSTRAINT `fk_sr_sale` FOREIGN KEY (`original_sale_id`) REFERENCES `sales` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_sr_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_sr_customer` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_sr_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `warehouses` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_sr_register` FOREIGN KEY (`cash_register_id`) REFERENCES `cash_registers` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `sales_return_items` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `sales_return_id` BIGINT NOT NULL,
    `product_id` INT NOT NULL,
    `quantity_returned` DECIMAL(18, 4) NOT NULL,
    `refund_unit_price` DECIMAL(18, 4) NOT NULL,
    `total_line_refund` DECIMAL(18, 4) NOT NULL,
    `return_to_stock` BOOLEAN NOT NULL DEFAULT TRUE,
    CONSTRAINT `fk_sri_return` FOREIGN KEY (`sales_return_id`) REFERENCES `sales_returns` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_sri_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 8. PURCHASES, PURCHASE ITEMS, PURCHASE PAYMENTS
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `purchase_payments`;
DROP TABLE IF EXISTS `purchase_items`;
DROP TABLE IF EXISTS `purchases`;

CREATE TABLE `purchases` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `purchase_number` VARCHAR(50) NOT NULL UNIQUE,
    `supplier_invoice_number` VARCHAR(50) NULL,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `user_id` INT NOT NULL,
    `supplier_id` INT NOT NULL,
    `warehouse_id` INT NOT NULL,
    `subtotal` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `discount_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `tax_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `total_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `paid_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `remaining_debt` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `payment_status` ENUM('Paid', 'Partial', 'Unpaid') NOT NULL DEFAULT 'Paid',
    `notes` VARCHAR(255) NULL,
    INDEX `idx_purchases_number` (`purchase_number`),
    INDEX `idx_purchases_supplier` (`supplier_id`),
    CONSTRAINT `fk_purchases_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_purchases_supplier` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_purchases_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `warehouses` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `purchase_items` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `purchase_id` BIGINT NOT NULL,
    `product_id` INT NOT NULL,
    `quantity` DECIMAL(18, 4) NOT NULL,
    `unit_buy_price` DECIMAL(18, 4) NOT NULL,
    `discount_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `tax_percent` DECIMAL(5, 2) NOT NULL DEFAULT 0.00,
    `total_line_amount` DECIMAL(18, 4) NOT NULL,
    CONSTRAINT `fk_pi_purchase` FOREIGN KEY (`purchase_id`) REFERENCES `purchases` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_pi_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `purchase_payments` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `purchase_id` BIGINT NOT NULL,
    `cash_register_id` INT NULL,
    `user_id` INT NOT NULL,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `payment_method` ENUM('Cash', 'BankTransfer', 'Cheque', 'Credit') NOT NULL,
    `amount` DECIMAL(18, 4) NOT NULL,
    `notes` VARCHAR(150) NULL,
    CONSTRAINT `fk_pp_purchase` FOREIGN KEY (`purchase_id`) REFERENCES `purchases` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_pp_register` FOREIGN KEY (`cash_register_id`) REFERENCES `cash_registers` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_pp_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 9. PURCHASE RETURNS
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `purchase_return_items`;
DROP TABLE IF EXISTS `purchase_returns`;

CREATE TABLE `purchase_returns` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `return_number` VARCHAR(50) NOT NULL UNIQUE,
    `original_purchase_id` BIGINT NOT NULL,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `user_id` INT NOT NULL,
    `supplier_id` INT NOT NULL,
    `warehouse_id` INT NOT NULL,
    `total_refund_amount` DECIMAL(18, 4) NOT NULL DEFAULT 0.0000,
    `refund_method` ENUM('Cash', 'SupplierDebtDeduction', 'BankTransfer') NOT NULL DEFAULT 'SupplierDebtDeduction',
    `reason` VARCHAR(255) NULL,
    CONSTRAINT `fk_pr_purchase` FOREIGN KEY (`original_purchase_id`) REFERENCES `purchases` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_pr_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_pr_supplier` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_pr_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `warehouses` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `purchase_return_items` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `purchase_return_id` BIGINT NOT NULL,
    `product_id` INT NOT NULL,
    `quantity_returned` DECIMAL(18, 4) NOT NULL,
    `unit_buy_price` DECIMAL(18, 4) NOT NULL,
    `total_line_amount` DECIMAL(18, 4) NOT NULL,
    CONSTRAINT `fk_pri_return` FOREIGN KEY (`purchase_return_id`) REFERENCES `purchase_returns` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_pri_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 10. CUSTOMER & SUPPLIER DEBT PAYMENTS (REGLEMENTS)
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `customer_payments`;
DROP TABLE IF EXISTS `supplier_payments`;

CREATE TABLE `customer_payments` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `payment_number` VARCHAR(50) NOT NULL UNIQUE,
    `customer_id` INT NOT NULL,
    `cash_register_id` INT NULL,
    `user_id` INT NOT NULL,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `amount` DECIMAL(18, 4) NOT NULL,
    `previous_debt` DECIMAL(18, 4) NOT NULL,
    `remaining_debt` DECIMAL(18, 4) NOT NULL,
    `payment_method` ENUM('Cash', 'BankTransfer', 'Cheque') NOT NULL DEFAULT 'Cash',
    `notes` VARCHAR(255) NULL,
    INDEX `idx_cp_customer` (`customer_id`),
    CONSTRAINT `fk_cp_customer` FOREIGN KEY (`customer_id`) REFERENCES `customers` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_cp_register` FOREIGN KEY (`cash_register_id`) REFERENCES `cash_registers` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_cp_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `supplier_payments` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `payment_number` VARCHAR(50) NOT NULL UNIQUE,
    `supplier_id` INT NOT NULL,
    `cash_register_id` INT NULL,
    `user_id` INT NOT NULL,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `amount` DECIMAL(18, 4) NOT NULL,
    `previous_debt` DECIMAL(18, 4) NOT NULL,
    `remaining_debt` DECIMAL(18, 4) NOT NULL,
    `payment_method` ENUM('Cash', 'BankTransfer', 'Cheque') NOT NULL DEFAULT 'Cash',
    `notes` VARCHAR(255) NULL,
    INDEX `idx_supp_pay_supplier` (`supplier_id`),
    CONSTRAINT `fk_supp_pay_supplier` FOREIGN KEY (`supplier_id`) REFERENCES `suppliers` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_supp_pay_register` FOREIGN KEY (`cash_register_id`) REFERENCES `cash_registers` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_supp_pay_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 11. EXPENSES & EXPENSE CATEGORIES
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `expenses`;
DROP TABLE IF EXISTS `expense_categories`;

CREATE TABLE `expense_categories` (
    `id` INT AUTO_INCREMENT PRIMARY KEY,
    `name_ar` VARCHAR(100) NOT NULL UNIQUE,
    `name_fr` VARCHAR(100) NULL,
    `description` VARCHAR(255) NULL,
    `is_active` BOOLEAN NOT NULL DEFAULT TRUE
) ENGINE=InnoDB;

CREATE TABLE `expenses` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `expense_number` VARCHAR(50) NOT NULL UNIQUE,
    `category_id` INT NOT NULL,
    `cash_register_id` INT NULL,
    `user_id` INT NOT NULL,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `amount` DECIMAL(18, 4) NOT NULL,
    `title` VARCHAR(150) NOT NULL,
    `notes` TEXT NULL,
    INDEX `idx_expenses_category` (`category_id`),
    INDEX `idx_expenses_timestamp` (`timestamp`),
    CONSTRAINT `fk_expenses_category` FOREIGN KEY (`category_id`) REFERENCES `expense_categories` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_expenses_register` FOREIGN KEY (`cash_register_id`) REFERENCES `cash_registers` (`id`) ON DELETE SET NULL,
    CONSTRAINT `fk_expenses_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 12. INVENTORY COUNTS & STOCK ADJUSTMENTS (الجرد الدوري)
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `inventory_count_items`;
DROP TABLE IF EXISTS `inventory_counts`;

CREATE TABLE `inventory_counts` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `count_number` VARCHAR(50) NOT NULL UNIQUE,
    `warehouse_id` INT NOT NULL,
    `user_id` INT NOT NULL,
    `start_date` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `completed_date` DATETIME NULL,
    `status` ENUM('Draft', 'InProgress', 'Completed', 'Cancelled') NOT NULL DEFAULT 'Draft',
    `notes` TEXT NULL,
    CONSTRAINT `fk_ic_warehouse` FOREIGN KEY (`warehouse_id`) REFERENCES `warehouses` (`id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_ic_user` FOREIGN KEY (`user_id`) REFERENCES `users` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE `inventory_count_items` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `inventory_count_id` BIGINT NOT NULL,
    `product_id` INT NOT NULL,
    `system_quantity` DECIMAL(18, 4) NOT NULL,
    `actual_quantity` DECIMAL(18, 4) NOT NULL,
    `difference` DECIMAL(18, 4) NOT NULL, -- actual - system
    `unit_cost` DECIMAL(18, 4) NOT NULL,
    `difference_value` DECIMAL(18, 4) NOT NULL,
    `notes` VARCHAR(255) NULL,
    CONSTRAINT `fk_ici_count` FOREIGN KEY (`inventory_count_id`) REFERENCES `inventory_counts` (`id`) ON DELETE CASCADE,
    CONSTRAINT `fk_ici_product` FOREIGN KEY (`product_id`) REFERENCES `products` (`id`) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 13. AUDIT LOGS & SETTINGS
-- -----------------------------------------------------------------------------

DROP TABLE IF EXISTS `audit_logs`;
DROP TABLE IF EXISTS `settings`;

CREATE TABLE `audit_logs` (
    `id` BIGINT AUTO_INCREMENT PRIMARY KEY,
    `user_id` INT NULL,
    `username` VARCHAR(50) NOT NULL,
    `action` VARCHAR(80) NOT NULL, -- e.g. "CreateSale", "UpdateProduct", "Login", "CloseRegister"
    `entity_name` VARCHAR(50) NOT NULL,
    `entity_id` VARCHAR(50) NULL,
    `timestamp` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `computer_name` VARCHAR(80) NULL,
    `ip_address` VARCHAR(50) NULL,
    `old_values` JSON NULL,
    `new_values` JSON NULL,
    INDEX `idx_al_action` (`action`),
    INDEX `idx_al_timestamp` (`timestamp`),
    INDEX `idx_al_user` (`user_id`)
) ENGINE=InnoDB;

CREATE TABLE `settings` (
    `setting_key` VARCHAR(80) PRIMARY KEY,
    `setting_value` TEXT NOT NULL,
    `category` VARCHAR(50) NOT NULL DEFAULT 'General',
    `description` VARCHAR(255) NULL,
    `updated_at` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 14. VIEWS FOR FINANCIAL ANALYSIS & PROFITABILITY
-- -----------------------------------------------------------------------------

CREATE OR REPLACE VIEW `v_product_stock_valuation` AS
SELECT 
    p.id AS product_id,
    p.barcode,
    p.sku,
    p.name_ar,
    c.name_ar AS category_name,
    p.purchase_price,
    p.sale_price,
    COALESCE(SUM(wp.current_quantity), 0) AS total_stock,
    COALESCE(SUM(wp.current_quantity), 0) * p.purchase_price AS total_cost_value,
    COALESCE(SUM(wp.current_quantity), 0) * p.sale_price AS total_retail_value,
    (COALESCE(SUM(wp.current_quantity), 0) * p.sale_price) - (COALESCE(SUM(wp.current_quantity), 0) * p.purchase_price) AS potential_profit
FROM products p
LEFT JOIN categories c ON p.category_id = c.id
LEFT JOIN warehouse_products wp ON p.id = wp.product_id
WHERE p.is_active = TRUE
GROUP BY p.id, p.barcode, p.sku, p.name_ar, c.name_ar, p.purchase_price, p.sale_price;

CREATE OR REPLACE VIEW `v_daily_sales_stats` AS
SELECT 
    DATE(s.timestamp) AS sale_date,
    COUNT(s.id) AS total_invoices,
    SUM(s.subtotal) AS gross_sales,
    SUM(s.discount_amount) AS total_discounts,
    SUM(s.total_amount) AS net_sales,
    SUM(s.total_cost) AS total_cogs,
    SUM(s.net_profit) AS total_profit,
    SUM(s.paid_amount) AS total_cash_collected,
    SUM(s.remaining_debt) AS total_credit_sales
FROM sales s
WHERE s.is_cancelled = FALSE
GROUP BY DATE(s.timestamp);

SET FOREIGN_KEY_CHECKS = 1;
