// Modern Arabic POS System Pro - Client Logic

let state = {
  settings: {},
  categories: [],
  products: [],
  customers: [],
  suppliers: [],
  purchases: [],
  cart: [],
  activeCategory: 'all',
  currentTab: 'pos',
  sales: [],
  stats: null,
  registerData: null,
  currentReceiptSale: null,
  charts: {
    timeline: null,
    topProducts: null
  }
};

// DOM References
const elements = {
  headerStoreName: document.getElementById('headerStoreName'),
  liveClock: document.getElementById('liveClock'),
  navTabs: document.querySelectorAll('.nav-tab'),
  tabPanes: document.querySelectorAll('.tab-pane'),

  // POS
  posSearchInput: document.getElementById('posSearchInput'),
  btnFocusSearch: document.getElementById('btnFocusSearch'),
  posCategoryPills: document.getElementById('posCategoryPills'),
  posProductsGrid: document.getElementById('posProductsGrid'),
  posCustomerSelect: document.getElementById('posCustomerSelect'),
  posPaymentMethod: document.getElementById('posPaymentMethod'),
  cartItemsList: document.getElementById('cartItemsList'),
  cartCount: document.getElementById('cartCount'),
  cartSubtotal: document.getElementById('cartSubtotal'),
  cartDiscount: document.getElementById('cartDiscount'),
  cartPaid: document.getElementById('cartPaid'),
  paidInputContainer: document.getElementById('paidInputContainer'),
  cartTotal: document.getElementById('cartTotal'),
  cartChange: document.getElementById('cartChange'),
  cartChangeRow: document.getElementById('cartChangeRow'),
  cartDebtRow: document.getElementById('cartDebtRow'),
  cartDebtAmount: document.getElementById('cartDebtAmount'),
  btnCheckout: document.getElementById('btnCheckout'),
  btnClearCart: document.getElementById('btnClearCart'),

  // Sales History
  salesTableBody: document.getElementById('salesTableBody'),
  salesSearchInput: document.getElementById('salesSearchInput'),
  salesDateFilter: document.getElementById('salesDateFilter'),
  btnResetSalesFilter: document.getElementById('btnResetSalesFilter'),

  // Customers & Debts
  customersTableBody: document.getElementById('customersTableBody'),
  custSearchInput: document.getElementById('custSearchInput'),
  btnFilterCustWithDebt: document.getElementById('btnFilterCustWithDebt'),
  btnOpenAddCustomer: document.getElementById('btnOpenAddCustomer'),
  kpiTotalDebts: document.getElementById('kpiTotalDebts'),
  kpiDebtorsCount: document.getElementById('kpiDebtorsCount'),

  // Customer Modal
  customerModal: document.getElementById('customerModal'),
  customerModalTitle: document.getElementById('customerModalTitle'),
  customerForm: document.getElementById('customerForm'),
  btnCloseCustomerModal: document.getElementById('btnCloseCustomerModal'),
  btnCancelCustomerModal: document.getElementById('btnCancelCustomerModal'),
  custId: document.getElementById('custId'),
  custName: document.getElementById('custName'),
  custPhone: document.getElementById('custPhone'),
  custCreditLimit: document.getElementById('custCreditLimit'),
  custAddress: document.getElementById('custAddress'),
  custNotes: document.getElementById('custNotes'),

  // Repay Modal
  repayModal: document.getElementById('repayModal'),
  btnCloseRepayModal: document.getElementById('btnCloseRepayModal'),
  btnCancelRepayModal: document.getElementById('btnCancelRepayModal'),
  repayForm: document.getElementById('repayForm'),
  repayCustId: document.getElementById('repayCustId'),
  repayCustName: document.getElementById('repayCustName'),
  repayCustCurrentDebt: document.getElementById('repayCustCurrentDebt'),
  repayAmount: document.getElementById('repayAmount'),
  repayNote: document.getElementById('repayNote'),

  // Statement Modal
  statementModal: document.getElementById('statementModal'),
  btnCloseStatementModal: document.getElementById('btnCloseStatementModal'),
  btnCloseStmtBtn: document.getElementById('btnCloseStmtBtn'),
  stmtCustName: document.getElementById('stmtCustName'),
  stmtCustPhone: document.getElementById('stmtCustPhone'),
  stmtCustDebt: document.getElementById('stmtCustDebt'),
  stmtHistoryBody: document.getElementById('stmtHistoryBody'),

  // Suppliers & Purchases
  suppliersTableBody: document.getElementById('suppliersTableBody'),
  purchasesTableBody: document.getElementById('purchasesTableBody'),
  suppSearchInput: document.getElementById('suppSearchInput'),
  btnFilterSuppWithDebt: document.getElementById('btnFilterSuppWithDebt'),
  btnOpenAddSupplier: document.getElementById('btnOpenAddSupplier'),
  btnOpenNewPurchase: document.getElementById('btnOpenNewPurchase'),
  kpiTotalSuppDebts: document.getElementById('kpiTotalSuppDebts'),
  kpiSuppliersCount: document.getElementById('kpiSuppliersCount'),
  kpiCreditorSuppliersCount: document.getElementById('kpiCreditorSuppliersCount'),

  // Supplier Modal
  supplierModal: document.getElementById('supplierModal'),
  supplierModalTitle: document.getElementById('supplierModalTitle'),
  supplierForm: document.getElementById('supplierForm'),
  btnCloseSupplierModal: document.getElementById('btnCloseSupplierModal'),
  btnCancelSupplierModal: document.getElementById('btnCancelSupplierModal'),
  suppId: document.getElementById('suppId'),
  suppName: document.getElementById('suppName'),
  suppCompany: document.getElementById('suppCompany'),
  suppPhone: document.getElementById('suppPhone'),
  suppAddress: document.getElementById('suppAddress'),
  suppDebt: document.getElementById('suppDebt'),
  suppNotes: document.getElementById('suppNotes'),

  // Supplier Repay Modal
  suppRepayModal: document.getElementById('suppRepayModal'),
  btnCloseSuppRepayModal: document.getElementById('btnCloseSuppRepayModal'),
  btnCancelSuppRepayModal: document.getElementById('btnCancelSuppRepayModal'),
  suppRepayForm: document.getElementById('suppRepayForm'),
  suppRepayId: document.getElementById('suppRepayId'),
  suppRepayName: document.getElementById('suppRepayName'),
  suppRepayCurrentDebt: document.getElementById('suppRepayCurrentDebt'),
  suppRepayAmount: document.getElementById('suppRepayAmount'),
  suppRepayNote: document.getElementById('suppRepayNote'),

  // Supplier Statement Modal
  suppStatementModal: document.getElementById('suppStatementModal'),
  btnCloseSuppStatementModal: document.getElementById('btnCloseSuppStatementModal'),
  btnCloseSuppStmtBtn: document.getElementById('btnCloseSuppStmtBtn'),
  suppStmtName: document.getElementById('suppStmtName'),
  suppStmtCompany: document.getElementById('suppStmtCompany'),
  suppStmtDebt: document.getElementById('suppStmtDebt'),
  suppStmtHistoryBody: document.getElementById('suppStmtHistoryBody'),

  // Purchase Modal
  purchaseModal: document.getElementById('purchaseModal'),
  btnClosePurchaseModal: document.getElementById('btnClosePurchaseModal'),
  btnCancelPurchaseModal: document.getElementById('btnCancelPurchaseModal'),
  purchaseForm: document.getElementById('purchaseForm'),
  purchSupplierSelect: document.getElementById('purchSupplierSelect'),
  purchPaymentMethod: document.getElementById('purchPaymentMethod'),
  purchProductSelect: document.getElementById('purchProductSelect'),
  purchQty: document.getElementById('purchQty'),
  purchUnitPrice: document.getElementById('purchUnitPrice'),
  purchItemTotal: document.getElementById('purchItemTotal'),
  purchPaidAmount: document.getElementById('purchPaidAmount'),
  purchNotes: document.getElementById('purchNotes'),

  // Inventory
  inventoryTableBody: document.getElementById('inventoryTableBody'),
  invSearchInput: document.getElementById('invSearchInput'),
  invCategoryFilter: document.getElementById('invCategoryFilter'),
  btnFilterLowStock: document.getElementById('btnFilterLowStock'),
  btnOpenAddProduct: document.getElementById('btnOpenAddProduct'),

  // Product Modal
  productModal: document.getElementById('productModal'),
  productModalTitle: document.getElementById('productModalTitle'),
  productForm: document.getElementById('productForm'),
  btnCloseProductModal: document.getElementById('btnCloseProductModal'),
  btnCancelProductModal: document.getElementById('btnCancelProductModal'),
  prodId: document.getElementById('prodId'),
  prodName: document.getElementById('prodName'),
  prodBarcode: document.getElementById('prodBarcode'),
  prodCategory: document.getElementById('prodCategory'),
  prodBuyPrice: document.getElementById('prodBuyPrice'),
  prodSellPrice: document.getElementById('prodSellPrice'),
  prodStock: document.getElementById('prodStock'),
  prodMinStock: document.getElementById('prodMinStock'),

  // Register Tab
  regTodayDate: document.getElementById('regTodayDate'),
  regOpeningFloat: document.getElementById('regOpeningFloat'),
  regCashSales: document.getElementById('regCashSales'),
  regDebtCollections: document.getElementById('regDebtCollections'),
  regSupplierPaid: document.getElementById('regSupplierPaid'),
  regCreditSales: document.getElementById('regCreditSales'),
  regExpectedCash: document.getElementById('regExpectedCash'),
  regInvoicesCount: document.getElementById('regInvoicesCount'),
  regTodayProfit: document.getElementById('regTodayProfit'),
  btnRefreshRegister: document.getElementById('btnRefreshRegister'),
  btnPrintZReport: document.getElementById('btnPrintZReport'),

  // Stats
  kpiTodaySales: document.getElementById('kpiTodaySales'),
  kpiTodayCount: document.getElementById('kpiTodayCount'),
  kpiTodayProfit: document.getElementById('kpiTodayProfit'),
  kpiTotalSales: document.getElementById('kpiTotalSales'),
  kpiTotalInvoices: document.getElementById('kpiTotalInvoices'),
  kpiStockAlerts: document.getElementById('kpiStockAlerts'),
  statTotalItems: document.getElementById('statTotalItems'),
  statBuyVal: document.getElementById('statBuyVal'),
  statSellVal: document.getElementById('statSellVal'),
  statPotentialProfit: document.getElementById('statPotentialProfit'),
  btnRefreshStats: document.getElementById('btnRefreshStats'),

  // Settings
  settingsForm: document.getElementById('settingsForm'),
  setStoreName: document.getElementById('setStoreName'),
  setPhone: document.getElementById('setPhone'),
  setCurrency: document.getElementById('setCurrency'),
  setAddress: document.getElementById('setAddress'),
  setReceiptFooter: document.getElementById('setReceiptFooter'),
  importFileInput: document.getElementById('importFileInput'),
  btnResetDemoData: document.getElementById('btnResetDemoData'),

  // Receipt Modal
  receiptModal: document.getElementById('receiptModal'),
  btnCloseReceiptModal: document.getElementById('btnCloseReceiptModal'),
  btnDoneReceipt: document.getElementById('btnDoneReceipt'),
  btnPrintReceipt: document.getElementById('btnPrintReceipt'),
  btnViewThermal: document.getElementById('btnViewThermal'),
  btnViewA4: document.getElementById('btnViewA4'),
  receiptPaper: document.getElementById('receiptPaper'),
  a4InvoiceContainer: document.getElementById('a4InvoiceContainer'),

  // Thermal Fields
  recStoreName: document.getElementById('recStoreName'),
  recStoreAddress: document.getElementById('recStoreAddress'),
  recStorePhone: document.getElementById('recStorePhone'),
  recInvoiceNum: document.getElementById('recInvoiceNum'),
  recDate: document.getElementById('recDate'),
  recCustName: document.getElementById('recCustName'),
  recItemsBody: document.getElementById('recItemsBody'),
  recSubtotal: document.getElementById('recSubtotal'),
  recDiscount: document.getElementById('recDiscount'),
  recDiscountRow: document.getElementById('recDiscountRow'),
  recTotal: document.getElementById('recTotal'),
  recPaid: document.getElementById('recPaid'),
  recChange: document.getElementById('recChange'),
  recChangeRow: document.getElementById('recChangeRow'),
  recDebt: document.getElementById('recDebt'),
  recDebtRow: document.getElementById('recDebtRow'),
  recBarcodeCode: document.getElementById('recBarcodeCode'),
  recFooterMsg: document.getElementById('recFooterMsg'),

  // A4 Fields
  a4StoreName: document.getElementById('a4StoreName'),
  a4StoreAddress: document.getElementById('a4StoreAddress'),
  a4StorePhone: document.getElementById('a4StorePhone'),
  a4InvoiceNum: document.getElementById('a4InvoiceNum'),
  a4Date: document.getElementById('a4Date'),
  a4CustName: document.getElementById('a4CustName'),
  a4PayMethod: document.getElementById('a4PayMethod'),
  a4ItemsBody: document.getElementById('a4ItemsBody'),
  a4Subtotal: document.getElementById('a4Subtotal'),
  a4DiscountRow: document.getElementById('a4DiscountRow'),
  a4Discount: document.getElementById('a4Discount'),
  a4Total: document.getElementById('a4Total'),
  a4Paid: document.getElementById('a4Paid'),
  a4DebtRow: document.getElementById('a4DebtRow'),
  a4Debt: document.getElementById('a4Debt'),

  toastContainer: document.getElementById('toastContainer')
};

// Utilities
function formatMoney(amount) {
  const currency = state.settings.currency || 'دج';
  const val = Number(amount) || 0;
  return `${val.toLocaleString('ar-DZ', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} ${currency}`;
}

function showToast(message, isError = false) {
  const toast = document.createElement('div');
  toast.className = `toast ${isError ? 'toast-error' : ''}`;
  toast.innerHTML = `
    <span style="font-size: 1.2rem;">${isError ? '⚠️' : '✅'}</span>
    <span>${message}</span>
  `;
  elements.toastContainer.appendChild(toast);
  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateX(-100%)';
    toast.style.transition = 'all 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  }, 3500);
}

// Live Clock
function updateClock() {
  const now = new Date();
  elements.liveClock.textContent = now.toLocaleDateString('ar-DZ', {
    weekday: 'long',
    year: 'numeric',
    month: 'numeric',
    day: 'numeric'
  }) + ' | ' + now.toLocaleTimeString('ar-DZ');
}
setInterval(updateClock, 1000);
updateClock();

// Tab Navigation
elements.navTabs.forEach(tab => {
  tab.addEventListener('click', () => {
    const target = tab.dataset.tab;
    switchTab(target);
  });
});

function switchTab(tabId) {
  state.currentTab = tabId;
  elements.navTabs.forEach(t => t.classList.toggle('active', t.dataset.tab === tabId));
  elements.tabPanes.forEach(p => p.classList.toggle('active', p.id === `pane-${tabId}`));

  if (tabId === 'pos') {
    elements.posSearchInput.focus();
    renderPosCatalog();
  } else if (tabId === 'sales') {
    loadSales();
  } else if (tabId === 'customers') {
    loadCustomers();
  } else if (tabId === 'suppliers') {
    loadSuppliers();
    loadPurchases();
  } else if (tabId === 'products') {
    loadProducts();
  } else if (tabId === 'register') {
    loadRegister();
  } else if (tabId === 'stats') {
    loadStats();
  } else if (tabId === 'settings') {
    populateSettingsForm();
  }
}

// API Calls Helper
async function fetchAPI(url, options = {}) {
  try {
    const res = await fetch(url, {
      headers: { 'Content-Type': 'application/json' },
      ...options
    });
    if (!res.ok) {
      const err = await res.json().catch(() => ({ error: 'فشل الطلب' }));
      throw new Error(err.error || `خطأ ${res.status}`);
    }
    return await res.json();
  } catch (err) {
    showToast(err.message, true);
    throw err;
  }
}

// Initial Data Load
async function initApp() {
  try {
    const [settings, categories, customers, suppliers] = await Promise.all([
      fetchAPI('/api/settings'),
      fetchAPI('/api/categories'),
      fetchAPI('/api/customers'),
      fetchAPI('/api/suppliers')
    ]);
    state.settings = settings;
    state.categories = categories;
    state.customers = customers;
    state.suppliers = suppliers;

    elements.headerStoreName.textContent = settings.storeName || 'متجر النور للمبيعات والتوزيع';
    renderCategoryPills();
    populateCategorySelects();
    populateCustomerSelects();
    populateSupplierSelects();
    await loadProducts();
  } catch (err) {
    console.error("Initialization error:", err);
  }
}

// Render Category Pills (POS)
function renderCategoryPills() {
  elements.posCategoryPills.innerHTML = state.categories.map(cat => `
    <button class="category-pill ${state.activeCategory === cat.id ? 'active' : ''}" data-cat="${cat.id}">
      ${cat.name}
    </button>
  `).join('');

  elements.posCategoryPills.querySelectorAll('.category-pill').forEach(btn => {
    btn.addEventListener('click', () => {
      state.activeCategory = btn.dataset.cat;
      renderCategoryPills();
      renderPosCatalog();
    });
  });
}

function populateCategorySelects() {
  const cats = state.categories.filter(c => c.id !== 'all');
  elements.prodCategory.innerHTML = cats.map(c => `<option value="${c.id}">${c.name}</option>`).join('');
  elements.invCategoryFilter.innerHTML = '<option value="all">كل التصنيفات</option>' +
    cats.map(c => `<option value="${c.id}">${c.name}</option>`).join('');
}

function populateCustomerSelects() {
  elements.posCustomerSelect.innerHTML = state.customers.map(c => `
    <option value="${c.id}">
      ${c.name} ${c.debt > 0 ? `(دين سابق: ${formatMoney(c.debt)})` : ''}
    </option>
  `).join('');
}

function populateSupplierSelects() {
  elements.purchSupplierSelect.innerHTML = state.suppliers.map(s => `
    <option value="${s.id}">
      ${s.name} ${s.company ? `(${s.company})` : ''} ${s.debt > 0 ? `[دين علينا: ${formatMoney(s.debt)}]` : ''}
    </option>
  `).join('');
}

function populatePurchaseProductSelects() {
  elements.purchProductSelect.innerHTML = state.products.map(p => `
    <option value="${p.id}" data-buy="${p.buyPrice}">
      ${p.name} (المخزون الحالي: ${p.stock} | تكلفة الشراء السابقة: ${formatMoney(p.buyPrice)})
    </option>
  `).join('');

  if (state.products.length > 0) {
    elements.purchUnitPrice.value = state.products[0].buyPrice;
    calculatePurchaseTotal();
  }
}

// Load Products
async function loadProducts() {
  try {
    const products = await fetchAPI('/api/products');
    state.products = products;
    renderPosCatalog();
    populatePurchaseProductSelects();
    if (state.currentTab === 'products') {
      renderInventoryTable();
    }
  } catch (err) {
    console.error(err);
  }
}

// Render POS Catalog Grid
function renderPosCatalog() {
  const query = elements.posSearchInput.value.trim().toLowerCase();
  let list = state.products;

  if (state.activeCategory !== 'all') {
    list = list.filter(p => p.category === state.activeCategory);
  }
  if (query) {
    list = list.filter(p => 
      p.name.toLowerCase().includes(query) || 
      (p.barcode && p.barcode.toLowerCase().includes(query))
    );
  }

  if (list.length === 0) {
    elements.posProductsGrid.innerHTML = `
      <div style="grid-column: 1 / -1; text-align: center; padding: 3rem 1rem; color: var(--text-light);">
        <p style="font-size: 1.1rem; margin-bottom: 0.5rem;">لا توجد منتجات مطابقة للبحث</p>
        <span style="font-size: 0.85rem;">جرّب كلمة بحث أخرى أو تصنيفاً مختلفاً</span>
      </div>
    `;
    return;
  }

  elements.posProductsGrid.innerHTML = list.map(p => {
    const isLow = p.stock <= (p.minStock || 5) && p.stock > 0;
    const isOut = p.stock <= 0;
    return `
      <div class="product-card" data-id="${p.id}">
        ${isOut ? '<span class="product-badge-low" style="background: #fee2e2; color: #b91c1c;">نفد المخزون</span>' : ''}
        ${isLow ? '<span class="product-badge-low">مخزون قليل</span>' : ''}
        <div class="product-card-barcode">${p.barcode || '—'}</div>
        <div class="product-card-title">${p.name}</div>
        <div class="product-card-footer">
          <div class="product-card-price">${formatMoney(p.sellPrice)}</div>
          <div class="product-card-stock">المتاح: ${p.stock}</div>
        </div>
      </div>
    `;
  }).join('');

  elements.posProductsGrid.querySelectorAll('.product-card').forEach(card => {
    card.addEventListener('click', () => {
      const id = parseInt(card.dataset.id, 10);
      addToCart(id);
    });
  });
}

// Search input handling & barcode enter key
elements.posSearchInput.addEventListener('input', () => {
  renderPosCatalog();
});

elements.posSearchInput.addEventListener('keydown', (e) => {
  if (e.key === 'Enter') {
    e.preventDefault();
    const query = elements.posSearchInput.value.trim().toLowerCase();
    if (!query) return;

    let found = state.products.find(p => p.barcode && p.barcode.toLowerCase() === query);
    if (!found) {
      found = state.products.find(p => p.name.toLowerCase().includes(query));
    }

    if (found) {
      addToCart(found.id);
      elements.posSearchInput.value = '';
      renderPosCatalog();
    } else {
      showToast("لم يتم العثور على أي منتج يطابق الباركود المدخل", true);
    }
  }
});

elements.btnFocusSearch.addEventListener('click', () => {
  elements.posSearchInput.focus();
  elements.posSearchInput.select();
});

// CART OPERATIONS
function addToCart(productId) {
  const product = state.products.find(p => p.id === productId);
  if (!product) return;

  if (product.stock <= 0) {
    showToast(`المنتج "${product.name}" نفد من المخزون!`, true);
    return;
  }

  const existing = state.cart.find(item => item.product.id === productId);
  if (existing) {
    if (existing.quantity + 1 > product.stock) {
      showToast(`لا يمكنك طلب أكثر من المخزون المتوفر (${product.stock}) للمنتج "${product.name}"`, true);
      return;
    }
    existing.quantity += 1;
  } else {
    state.cart.push({ product, quantity: 1 });
  }

  renderCart();
}

function updateCartQty(productId, delta) {
  const item = state.cart.find(it => it.product.id === productId);
  if (!item) return;

  const newQty = item.quantity + delta;
  if (newQty <= 0) {
    removeFromCart(productId);
    return;
  }
  if (newQty > item.product.stock) {
    showToast(`المخزون المتوفر هو ${item.product.stock} فقط`, true);
    return;
  }

  item.quantity = newQty;
  renderCart();
}

function removeFromCart(productId) {
  state.cart = state.cart.filter(it => it.product.id !== productId);
  renderCart();
}

elements.btnClearCart.addEventListener('click', () => {
  if (state.cart.length === 0) return;
  if (confirm("هل تريد تفريغ سلة المبيعات؟")) {
    state.cart = [];
    renderCart();
  }
});

elements.posPaymentMethod.addEventListener('change', () => {
  const method = elements.posPaymentMethod.value;
  if (method === 'credit') {
    elements.paidInputContainer.style.opacity = '0.5';
    elements.cartPaid.value = '0';
    elements.cartPaid.disabled = true;
  } else {
    elements.paidInputContainer.style.opacity = '1';
    elements.cartPaid.disabled = false;
  }
  const subtotal = state.cart.reduce((acc, it) => acc + (it.quantity * it.product.sellPrice), 0);
  calculateCheckoutTotals(subtotal);
});

function renderCart() {
  if (state.cart.length === 0) {
    elements.cartItemsList.innerHTML = `
      <div class="cart-empty-state">
        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><circle cx="8" cy="21" r="1"/><circle cx="19" cy="21" r="1"/><path d="M2.05 2.05h2l2.66 12.42a2 2 0 0 0 2 1.58h9.78a2 2 0 0 0 1.95-1.57l1.65-7.43H5.12"/></svg>
        <p style="font-weight: 600;">السلة فارغة حالياً</p>
        <span style="font-size: 0.8rem;">اختر منتجات من القائمة أو امسح الباركود للبدء</span>
      </div>
    `;
    elements.cartCount.textContent = '0 مواد';
    elements.cartSubtotal.textContent = formatMoney(0);
    elements.cartTotal.textContent = formatMoney(0);
    elements.cartChange.textContent = formatMoney(0);
    elements.cartDebtRow.style.display = 'none';
    elements.btnCheckout.disabled = true;
    return;
  }

  let subtotal = 0;
  let totalItemsCount = 0;

  elements.cartItemsList.innerHTML = state.cart.map(item => {
    const itemTotal = item.quantity * item.product.sellPrice;
    subtotal += itemTotal;
    totalItemsCount += item.quantity;

    return `
      <div class="cart-item-row">
        <div class="cart-item-info">
          <div class="cart-item-name">${item.product.name}</div>
          <div class="cart-item-price">${formatMoney(item.product.sellPrice)}</div>
        </div>
        <div class="cart-item-controls">
          <button class="btn-qty btn-minus" data-id="${item.product.id}">-</button>
          <span class="cart-item-qty">${item.quantity}</span>
          <button class="btn-qty btn-plus" data-id="${item.product.id}">+</button>
          <div class="cart-item-total">${formatMoney(itemTotal)}</div>
          <button class="btn-item-del" data-id="${item.product.id}" title="حذف">&times;</button>
        </div>
      </div>
    `;
  }).join('');

  elements.cartItemsList.querySelectorAll('.btn-minus').forEach(b => {
    b.addEventListener('click', () => updateCartQty(parseInt(b.dataset.id, 10), -1));
  });
  elements.cartItemsList.querySelectorAll('.btn-plus').forEach(b => {
    b.addEventListener('click', () => updateCartQty(parseInt(b.dataset.id, 10), 1));
  });
  elements.cartItemsList.querySelectorAll('.btn-item-del').forEach(b => {
    b.addEventListener('click', () => removeFromCart(parseInt(b.dataset.id, 10)));
  });

  elements.cartCount.textContent = `${totalItemsCount} مواد`;
  elements.cartSubtotal.textContent = formatMoney(subtotal);

  calculateCheckoutTotals(subtotal);
  elements.btnCheckout.disabled = false;
}

function calculateCheckoutTotals(subtotal) {
  const discount = Math.max(0, parseFloat(elements.cartDiscount.value) || 0);
  const total = Math.max(0, subtotal - discount);
  elements.cartTotal.textContent = formatMoney(total);

  const method = elements.posPaymentMethod.value;
  let paid = parseFloat(elements.cartPaid.value) || 0;
  let change = 0;
  let debt = 0;

  if (method === 'credit') {
    paid = 0;
    debt = total;
    elements.cartChangeRow.style.display = 'none';
    elements.cartDebtRow.style.display = 'flex';
    elements.cartDebtAmount.textContent = formatMoney(debt);
  } else if (method === 'partial') {
    paid = Math.min(total, paid);
    debt = Math.max(0, total - paid);
    elements.cartChangeRow.style.display = 'none';
    elements.cartDebtRow.style.display = 'flex';
    elements.cartDebtAmount.textContent = formatMoney(debt);
  } else {
    if (paid > total) {
      change = paid - total;
      debt = 0;
    } else {
      change = 0;
      debt = total - paid;
    }
    elements.cartChangeRow.style.display = 'flex';
    elements.cartChange.textContent = formatMoney(change);
    if (debt > 0 && paid > 0) {
      elements.cartDebtRow.style.display = 'flex';
      elements.cartDebtAmount.textContent = formatMoney(debt);
    } else {
      elements.cartDebtRow.style.display = 'none';
    }
  }

  return { subtotal, discount, total, paid, change, debt };
}

elements.cartDiscount.addEventListener('input', () => {
  const subtotal = state.cart.reduce((acc, it) => acc + (it.quantity * it.product.sellPrice), 0);
  calculateCheckoutTotals(subtotal);
});

elements.cartPaid.addEventListener('input', () => {
  const subtotal = state.cart.reduce((acc, it) => acc + (it.quantity * it.product.sellPrice), 0);
  calculateCheckoutTotals(subtotal);
});

// CHECKOUT OPERATION
elements.btnCheckout.addEventListener('click', async () => {
  if (state.cart.length === 0) return;

  const subtotal = state.cart.reduce((acc, it) => acc + (it.quantity * it.product.sellPrice), 0);
  const calc = calculateCheckoutTotals(subtotal);
  const customerId = parseInt(elements.posCustomerSelect.value, 10);
  const method = elements.posPaymentMethod.value;

  const payload = {
    items: state.cart.map(it => ({
      productId: it.product.id,
      quantity: it.quantity
    })),
    discount: calc.discount,
    paidAmount: calc.paid,
    paymentMethod: method,
    customerId: customerId
  };

  try {
    elements.btnCheckout.disabled = true;
    elements.btnCheckout.innerHTML = 'جاري تسجيل البيع...';

    const res = await fetchAPI('/api/sales', {
      method: 'POST',
      body: JSON.stringify(payload)
    });

    showToast("✔ تمت عملية البيع بنجاح وتحديث المخزون");

    state.cart = [];
    elements.cartDiscount.value = 0;
    elements.cartPaid.value = '';
    elements.posPaymentMethod.value = 'cash';
    elements.paidInputContainer.style.opacity = '1';
    elements.cartPaid.disabled = false;
    renderCart();

    await Promise.all([loadProducts(), loadCustomers()]);
    showReceiptModal(res.sale);
  } catch (err) {
    console.error(err);
  } finally {
    elements.btnCheckout.disabled = false;
    elements.btnCheckout.innerHTML = `
      <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5"><polyline points="20 6 9 17 4 12"/></svg>
      إتمام البيع واستخراج الفاتورة
    `;
  }
});

// UNIFIED RECEIPT DISPLAY (Thermal + A4)
function showReceiptModal(sale) {
  state.currentReceiptSale = sale;

  // Thermal Fields
  elements.recStoreName.textContent = state.settings.storeName || 'متجر النور للمبيعات والتوزيع';
  elements.recStoreAddress.textContent = state.settings.address || '';
  elements.recStorePhone.textContent = state.settings.phone ? `هاتف: ${state.settings.phone}` : '';
  elements.recInvoiceNum.textContent = `فاتورة رقم: ${sale.invoiceNumber}`;
  elements.recDate.textContent = `التاريخ: ${sale.dateStr}`;
  elements.recCustName.textContent = `الزبون: ${sale.customerName || 'زبون عابر'}`;

  elements.recItemsBody.innerHTML = sale.items.map(it => `
    <tr>
      <td>${it.name}</td>
      <td style="text-align: center;">${it.quantity}</td>
      <td style="text-align: left;">${it.sellPrice}</td>
      <td style="text-align: left;">${it.total}</td>
    </tr>
  `).join('');

  elements.recSubtotal.textContent = formatMoney(sale.subtotal);
  if (sale.discount > 0) {
    elements.recDiscountRow.style.display = 'flex';
    elements.recDiscount.textContent = formatMoney(sale.discount);
  } else {
    elements.recDiscountRow.style.display = 'none';
  }
  elements.recTotal.textContent = formatMoney(sale.total);
  elements.recPaid.textContent = formatMoney(sale.paidAmount);

  if (sale.changeAmount > 0) {
    elements.recChangeRow.style.display = 'flex';
    elements.recChange.textContent = formatMoney(sale.changeAmount);
  } else {
    elements.recChangeRow.style.display = 'none';
  }

  if (sale.debtAmount > 0) {
    elements.recDebtRow.style.display = 'flex';
    elements.recDebt.textContent = formatMoney(sale.debtAmount);
  } else {
    elements.recDebtRow.style.display = 'none';
  }

  elements.recBarcodeCode.textContent = sale.invoiceNumber;
  elements.recFooterMsg.textContent = state.settings.receiptFooter || 'شكراً لزيارتكم! مرحباً بكم دائماً.';

  // A4 Fields
  elements.a4StoreName.textContent = state.settings.storeName;
  elements.a4StoreAddress.textContent = state.settings.address;
  elements.a4StorePhone.textContent = `الهاتف: ${state.settings.phone}`;
  elements.a4InvoiceNum.textContent = sale.invoiceNumber;
  elements.a4Date.textContent = sale.dateStr;
  elements.a4CustName.textContent = sale.customerName || 'زبون عابر';
  elements.a4PayMethod.textContent = sale.paymentMethod === 'cash' ? 'نقداً (Cash)' : (sale.paymentMethod === 'credit' ? 'على الحساب بالكامل (دين)' : 'دفع جزئي');

  elements.a4ItemsBody.innerHTML = sale.items.map((it, idx) => `
    <tr style="border-bottom: 1px solid #e2e8f0;">
      <td style="padding: 0.5rem 0.75rem;">${idx + 1}</td>
      <td style="padding: 0.5rem 0.75rem; font-weight: 600;">${it.name}</td>
      <td style="padding: 0.5rem 0.75rem; text-align: center;">${it.quantity}</td>
      <td style="padding: 0.5rem 0.75rem; text-align: left;">${formatMoney(it.sellPrice)}</td>
      <td style="padding: 0.5rem 0.75rem; text-align: left; font-weight: 700;">${formatMoney(it.total)}</td>
    </tr>
  `).join('');

  elements.a4Subtotal.textContent = formatMoney(sale.subtotal);
  if (sale.discount > 0) {
    elements.a4DiscountRow.style.display = 'flex';
    elements.a4Discount.textContent = formatMoney(sale.discount);
  } else {
    elements.a4DiscountRow.style.display = 'none';
  }
  elements.a4Total.textContent = formatMoney(sale.total);
  elements.a4Paid.textContent = formatMoney(sale.paidAmount);
  if (sale.debtAmount > 0) {
    elements.a4DebtRow.style.display = 'flex';
    elements.a4Debt.textContent = formatMoney(sale.debtAmount);
  } else {
    elements.a4DebtRow.style.display = 'none';
  }

  setReceiptView('thermal');
  elements.receiptModal.classList.add('active');
}

function setReceiptView(mode) {
  if (mode === 'thermal') {
    elements.receiptPaper.style.display = 'block';
    elements.a4InvoiceContainer.style.display = 'none';
    elements.btnViewThermal.classList.add('active');
    elements.btnViewA4.classList.remove('active');
  } else {
    elements.receiptPaper.style.display = 'none';
    elements.a4InvoiceContainer.style.display = 'block';
    elements.btnViewA4.classList.add('active');
    elements.btnViewThermal.classList.remove('active');
  }
}

elements.btnViewThermal.addEventListener('click', () => setReceiptView('thermal'));
elements.btnViewA4.addEventListener('click', () => setReceiptView('a4'));
elements.btnCloseReceiptModal.addEventListener('click', () => elements.receiptModal.classList.remove('active'));
elements.btnDoneReceipt.addEventListener('click', () => elements.receiptModal.classList.remove('active'));
elements.btnPrintReceipt.addEventListener('click', () => window.print());

// ----------------- SALES HISTORY TAB -----------------

async function loadSales() {
  try {
    const query = elements.salesSearchInput.value.trim();
    const date = elements.salesDateFilter.value;
    let url = '/api/sales?';
    if (query) url += `q=${encodeURIComponent(query)}&`;
    if (date) url += `date=${encodeURIComponent(date)}&`;

    const sales = await fetchAPI(url);
    state.sales = sales;
    renderSalesTable();
  } catch (err) {
    console.error(err);
  }
}

function renderSalesTable() {
  if (state.sales.length === 0) {
    elements.salesTableBody.innerHTML = `
      <tr>
        <td colspan="10" style="text-align: center; padding: 2.5rem; color: var(--text-muted);">
          لا توجد فواتير مبيعات مسجلة
        </td>
      </tr>
    `;
    return;
  }

  elements.salesTableBody.innerHTML = state.sales.map(s => {
    const totalItems = s.items.reduce((acc, it) => acc + it.quantity, 0);
    const methodNames = {
      cash: 'نقداً',
      credit: 'دين بالكامل',
      partial: 'دفع جزئي'
    };

    return `
      <tr>
        <td style="font-family: monospace; font-weight: 700; color: var(--primary-dark);">${s.invoiceNumber}</td>
        <td style="font-size: 0.85rem; color: var(--text-muted);">${s.dateStr}</td>
        <td style="font-weight: 600;">${s.customerName || 'زبون عابر'}</td>
        <td style="text-align: center;">${totalItems} مواد</td>
        <td>${formatMoney(s.subtotal)}</td>
        <td style="color: var(--primary); font-weight: 700;">${formatMoney(s.paidAmount)}</td>
        <td style="color: ${s.debtAmount > 0 ? 'var(--danger)' : 'var(--text-muted)'}; font-weight: 700;">
          ${s.debtAmount > 0 ? formatMoney(s.debtAmount) : '—'}
        </td>
        <td style="color: var(--secondary); font-weight: 600;">+${formatMoney(s.profit)}</td>
        <td><span class="badge ${s.paymentMethod === 'cash' ? 'badge-success' : 'badge-warning'}">${methodNames[s.paymentMethod] || s.paymentMethod}</span></td>
        <td>
          <button class="btn btn-secondary btn-view-sale" data-id="${s.id}" style="padding: 0.35rem 0.75rem; font-size: 0.8rem;">
            معاينة
          </button>
        </td>
      </tr>
    `;
  }).join('');

  elements.salesTableBody.querySelectorAll('.btn-view-sale').forEach(b => {
    b.addEventListener('click', () => {
      const id = parseInt(b.dataset.id, 10);
      const sale = state.sales.find(s => s.id === id);
      if (sale) showReceiptModal(sale);
    });
  });
}

elements.salesSearchInput.addEventListener('input', loadSales);
elements.salesDateFilter.addEventListener('change', loadSales);
elements.btnResetSalesFilter.addEventListener('click', () => {
  elements.salesSearchInput.value = '';
  elements.salesDateFilter.value = '';
  loadSales();
});

// ----------------- CUSTOMERS & DEBTS (قائمة الزبائن والكريدي) -----------------

async function loadCustomers() {
  try {
    const query = elements.custSearchInput.value.trim();
    let url = '/api/customers?';
    if (query) url += `q=${encodeURIComponent(query)}&`;

    const customers = await fetchAPI(url);
    state.customers = customers;
    populateCustomerSelects();
    renderCustomersTable();
  } catch (err) {
    console.error(err);
  }
}

let filterDebtActive = false;
elements.btnFilterCustWithDebt.addEventListener('click', () => {
  filterDebtActive = !filterDebtActive;
  elements.btnFilterCustWithDebt.classList.toggle('btn-primary', filterDebtActive);
  elements.btnFilterCustWithDebt.classList.toggle('btn-secondary', !filterDebtActive);
  renderCustomersTable();
});

function renderCustomersTable() {
  let list = state.customers;
  if (filterDebtActive) {
    list = list.filter(c => (c.debt || 0) > 0);
  }

  let totalDebts = 0;
  let debtorsCount = 0;

  state.customers.forEach(c => {
    if (c.debt > 0) {
      totalDebts += c.debt;
      debtorsCount++;
    }
  });

  elements.kpiTotalDebts.textContent = formatMoney(totalDebts);
  elements.kpiDebtorsCount.textContent = `${debtorsCount} زبائن`;

  if (list.length === 0) {
    elements.customersTableBody.innerHTML = `
      <tr>
        <td colspan="8" style="text-align: center; padding: 2rem; color: var(--text-muted);">
          لا يوجد زبائن مطابقين
        </td>
      </tr>
    `;
    return;
  }

  elements.customersTableBody.innerHTML = list.map(c => `
    <tr>
      <td>${c.id}</td>
      <td style="font-weight: 700;">${c.name}</td>
      <td style="font-family: monospace;">${c.phone || '—'}</td>
      <td>${c.address || '—'}</td>
      <td style="font-weight: 800; color: ${c.debt > 0 ? 'var(--danger)' : 'var(--primary)'}; font-size: 1.05rem;">
        ${formatMoney(c.debt || 0)}
      </td>
      <td style="color: var(--text-muted); font-size: 0.85rem;">${formatMoney(c.creditLimit || 0)}</td>
      <td style="font-size: 0.85rem; color: var(--text-muted);">${c.notes || '—'}</td>
      <td>
        <div style="display: flex; gap: 0.4rem;">
          ${c.debt > 0 ? `
            <button class="btn btn-primary btn-repay-debt" data-id="${c.id}" style="padding: 0.3rem 0.6rem; font-size: 0.8rem;">
              تسديد دفعة
            </button>
          ` : ''}
          <button class="btn btn-secondary btn-cust-stmt" data-id="${c.id}" style="padding: 0.3rem 0.6rem; font-size: 0.8rem;">
            كشف حساب
          </button>
          <button class="btn btn-secondary btn-edit-cust" data-id="${c.id}" style="padding: 0.3rem 0.5rem; font-size: 0.8rem;">
            تعديل
          </button>
        </div>
      </td>
    </tr>
  `).join('');

  elements.customersTableBody.querySelectorAll('.btn-repay-debt').forEach(b => {
    b.addEventListener('click', () => openRepayModal(parseInt(b.dataset.id, 10)));
  });
  elements.customersTableBody.querySelectorAll('.btn-cust-stmt').forEach(b => {
    b.addEventListener('click', () => openStatementModal(parseInt(b.dataset.id, 10)));
  });
  elements.customersTableBody.querySelectorAll('.btn-edit-cust').forEach(b => {
    b.addEventListener('click', () => openEditCustomerModal(parseInt(b.dataset.id, 10)));
  });
}

elements.custSearchInput.addEventListener('input', loadCustomers);

// Add / Edit Customer Modal
elements.btnOpenAddCustomer.addEventListener('click', () => {
  elements.customerModalTitle.textContent = "إضافة زبون جديد";
  elements.custId.value = "";
  elements.customerForm.reset();
  elements.custCreditLimit.value = "20000";
  elements.customerModal.classList.add('active');
  elements.custName.focus();
});

function openEditCustomerModal(custId) {
  const c = state.customers.find(x => x.id === custId);
  if (!c) return;

  elements.customerModalTitle.textContent = "تعديل بيانات الزبون";
  elements.custId.value = c.id;
  elements.custName.value = c.name;
  elements.custPhone.value = c.phone || "";
  elements.custCreditLimit.value = c.creditLimit || 20000;
  elements.custAddress.value = c.address || "";
  elements.custNotes.value = c.notes || "";

  elements.customerModal.classList.add('active');
}

elements.btnCloseCustomerModal.addEventListener('click', () => elements.customerModal.classList.remove('active'));
elements.btnCancelCustomerModal.addEventListener('click', () => elements.customerModal.classList.remove('active'));

elements.customerForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  const id = elements.custId.value;
  const payload = {
    name: elements.custName.value.trim(),
    phone: elements.custPhone.value.trim(),
    creditLimit: Number(elements.custCreditLimit.value),
    address: elements.custAddress.value.trim(),
    notes: elements.custNotes.value.trim()
  };

  try {
    if (id) {
      await fetchAPI(`/api/customers/${id}`, {
        method: 'PUT',
        body: JSON.stringify(payload)
      });
      showToast("تم تحديث بيانات الزبون");
    } else {
      await fetchAPI('/api/customers', {
        method: 'POST',
        body: JSON.stringify(payload)
      });
      showToast("تمت إضافة الزبون بنجاح");
    }
    elements.customerModal.classList.remove('active');
    await loadCustomers();
  } catch (err) {
    console.error(err);
  }
});

// Repay Debt Modal
function openRepayModal(custId) {
  const c = state.customers.find(x => x.id === custId);
  if (!c) return;

  elements.repayCustId.value = c.id;
  elements.repayCustName.textContent = c.name;
  elements.repayCustCurrentDebt.textContent = formatMoney(c.debt);
  elements.repayAmount.value = c.debt;
  elements.repayAmount.max = c.debt;
  elements.repayModal.classList.add('active');
  elements.repayAmount.focus();
}

elements.btnCloseRepayModal.addEventListener('click', () => elements.repayModal.classList.remove('active'));
elements.btnCancelRepayModal.addEventListener('click', () => elements.repayModal.classList.remove('active'));

elements.repayForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  const id = elements.repayCustId.value;
  const amount = Number(elements.repayAmount.value);
  const note = elements.repayNote.value;

  try {
    await fetchAPI(`/api/customers/${id}/pay`, {
      method: 'POST',
      body: JSON.stringify({ amount, note })
    });
    showToast("✔ تم تسجيل دفعة التسديد وتحديث رصيد الزبون");
    elements.repayModal.classList.remove('active');
    await loadCustomers();
  } catch (err) {
    console.error(err);
  }
});

// Statement Modal (كشف حساب)
async function openStatementModal(custId) {
  try {
    const res = await fetchAPI(`/api/customers/${custId}/history`);
    elements.stmtCustName.textContent = res.customer.name;
    elements.stmtCustPhone.textContent = res.customer.phone ? `هاتف: ${res.customer.phone}` : '';
    elements.stmtCustDebt.textContent = formatMoney(res.customer.debt);

    const timeline = [];
    (res.sales || []).forEach(s => {
      timeline.push({
        date: s.dateStr,
        desc: `فاتورة مبيعات (${s.invoiceNumber})`,
        amount: s.total,
        paid: s.paidAmount,
        debt: s.debtAmount || 0,
        type: 'sale'
      });
    });
    (res.payments || []).forEach(p => {
      timeline.push({
        date: p.dateStr,
        desc: `تسديد نقدي: ${p.note || ''}`,
        amount: 0,
        paid: p.amount,
        debt: -p.amount,
        type: 'payment'
      });
    });

    timeline.sort((a, b) => b.date.localeCompare(a.date));

    if (timeline.length === 0) {
      elements.stmtHistoryBody.innerHTML = `
        <tr><td colspan="5" style="text-align: center; padding: 1.5rem;">لا توجد حركات مسجلة لهذا الزبون</td></tr>
      `;
    } else {
      elements.stmtHistoryBody.innerHTML = timeline.map(t => `
        <tr>
          <td>${t.date}</td>
          <td style="font-weight: 600;">${t.desc}</td>
          <td>${t.amount ? formatMoney(t.amount) : '—'}</td>
          <td style="color: var(--primary); font-weight: 700;">${formatMoney(t.paid)}</td>
          <td style="color: ${t.debt > 0 ? 'var(--danger)' : 'var(--secondary)'}; font-weight: 700;">
            ${t.debt > 0 ? '+' + formatMoney(t.debt) : (t.debt < 0 ? '-' + formatMoney(Math.abs(t.debt)) : '0')}
          </td>
        </tr>
      `).join('');
    }

    elements.statementModal.classList.add('active');
  } catch (err) {
    console.error(err);
  }
}

elements.btnCloseStatementModal.addEventListener('click', () => elements.statementModal.classList.remove('active'));
elements.btnCloseStmtBtn.addEventListener('click', () => elements.statementModal.classList.remove('active'));

// ----------------- SUPPLIERS & PURCHASES (الممونين والمشتريات) -----------------

async function loadSuppliers() {
  try {
    const query = elements.suppSearchInput.value.trim();
    let url = '/api/suppliers?';
    if (query) url += `q=${encodeURIComponent(query)}&`;

    const suppliers = await fetchAPI(url);
    state.suppliers = suppliers;
    populateSupplierSelects();
    renderSuppliersTable();
  } catch (err) {
    console.error(err);
  }
}

let filterSuppDebtActive = false;
elements.btnFilterSuppWithDebt.addEventListener('click', () => {
  filterSuppDebtActive = !filterSuppDebtActive;
  elements.btnFilterSuppWithDebt.classList.toggle('btn-primary', filterSuppDebtActive);
  elements.btnFilterSuppWithDebt.classList.toggle('btn-secondary', !filterSuppDebtActive);
  renderSuppliersTable();
});

function renderSuppliersTable() {
  let list = state.suppliers;
  if (filterSuppDebtActive) {
    list = list.filter(s => (s.debt || 0) > 0);
  }

  let totalDebt = 0;
  let creditorCount = 0;

  state.suppliers.forEach(s => {
    if (s.debt > 0) {
      totalDebt += s.debt;
      creditorCount++;
    }
  });

  elements.kpiTotalSuppDebts.textContent = formatMoney(totalDebt);
  elements.kpiSuppliersCount.textContent = `${state.suppliers.length} ممونين`;
  elements.kpiCreditorSuppliersCount.textContent = `${creditorCount} شركات دائنة`;

  if (list.length === 0) {
    elements.suppliersTableBody.innerHTML = `
      <tr>
        <td colspan="8" style="text-align: center; padding: 2rem; color: var(--text-muted);">
          لا يوجد ممونين مطابقين
        </td>
      </tr>
    `;
    return;
  }

  elements.suppliersTableBody.innerHTML = list.map(s => `
    <tr>
      <td>${s.id}</td>
      <td style="font-weight: 700;">${s.name}</td>
      <td style="color: var(--secondary); font-weight: 600;">${s.company || '—'}</td>
      <td style="font-family: monospace;">${s.phone || '—'}</td>
      <td>${s.address || '—'}</td>
      <td style="font-weight: 800; color: ${s.debt > 0 ? 'var(--danger)' : 'var(--primary)'}; font-size: 1.05rem;">
        ${formatMoney(s.debt || 0)}
      </td>
      <td style="font-size: 0.85rem; color: var(--text-muted);">${s.notes || '—'}</td>
      <td>
        <div style="display: flex; gap: 0.4rem;">
          ${s.debt > 0 ? `
            <button class="btn btn-primary btn-repay-supp" data-id="${s.id}" style="padding: 0.3rem 0.6rem; font-size: 0.8rem;">
              تسديد دفعة
            </button>
          ` : ''}
          <button class="btn btn-secondary btn-supp-stmt" data-id="${s.id}" style="padding: 0.3rem 0.6rem; font-size: 0.8rem;">
            كشف حساب
          </button>
          <button class="btn btn-secondary btn-edit-supp" data-id="${s.id}" style="padding: 0.3rem 0.5rem; font-size: 0.8rem;">
            تعديل
          </button>
        </div>
      </td>
    </tr>
  `).join('');

  elements.suppliersTableBody.querySelectorAll('.btn-repay-supp').forEach(b => {
    b.addEventListener('click', () => openSuppRepayModal(parseInt(b.dataset.id, 10)));
  });
  elements.suppliersTableBody.querySelectorAll('.btn-supp-stmt').forEach(b => {
    b.addEventListener('click', () => openSuppStatementModal(parseInt(b.dataset.id, 10)));
  });
  elements.suppliersTableBody.querySelectorAll('.btn-edit-supp').forEach(b => {
    b.addEventListener('click', () => openEditSupplierModal(parseInt(b.dataset.id, 10)));
  });
}

elements.suppSearchInput.addEventListener('input', loadSuppliers);

// Add / Edit Supplier Modal
elements.btnOpenAddSupplier.addEventListener('click', () => {
  elements.supplierModalTitle.textContent = "إضافة ممون جديد";
  elements.suppId.value = "";
  elements.supplierForm.reset();
  elements.suppDebt.value = "0";
  elements.supplierModal.classList.add('active');
  elements.suppName.focus();
});

function openEditSupplierModal(suppId) {
  const s = state.suppliers.find(x => x.id === suppId);
  if (!s) return;

  elements.supplierModalTitle.textContent = "تعديل بيانات الممون";
  elements.suppId.value = s.id;
  elements.suppName.value = s.name;
  elements.suppCompany.value = s.company || "";
  elements.suppPhone.value = s.phone || "";
  elements.suppAddress.value = s.address || "";
  elements.suppDebt.value = s.debt || 0;
  elements.suppNotes.value = s.notes || "";

  elements.supplierModal.classList.add('active');
}

elements.btnCloseSupplierModal.addEventListener('click', () => elements.supplierModal.classList.remove('active'));
elements.btnCancelSupplierModal.addEventListener('click', () => elements.supplierModal.classList.remove('active'));

elements.supplierForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  const id = elements.suppId.value;
  const payload = {
    name: elements.suppName.value.trim(),
    company: elements.suppCompany.value.trim(),
    phone: elements.suppPhone.value.trim(),
    address: elements.suppAddress.value.trim(),
    debt: Number(elements.suppDebt.value),
    notes: elements.suppNotes.value.trim()
  };

  try {
    if (id) {
      await fetchAPI(`/api/suppliers/${id}`, {
        method: 'PUT',
        body: JSON.stringify(payload)
      });
      showToast("تم تحديث بيانات الممون");
    } else {
      await fetchAPI('/api/suppliers', {
        method: 'POST',
        body: JSON.stringify(payload)
      });
      showToast("تمت إضافة الممون بنجاح");
    }
    elements.supplierModal.classList.remove('active');
    await loadSuppliers();
  } catch (err) {
    console.error(err);
  }
});

// Repay Supplier Modal
function openSuppRepayModal(suppId) {
  const s = state.suppliers.find(x => x.id === suppId);
  if (!s) return;

  elements.suppRepayId.value = s.id;
  elements.suppRepayName.textContent = `${s.name} ${s.company ? `(${s.company})` : ''}`;
  elements.suppRepayCurrentDebt.textContent = formatMoney(s.debt);
  elements.suppRepayAmount.value = s.debt;
  elements.suppRepayAmount.max = s.debt;
  elements.suppRepayModal.classList.add('active');
  elements.suppRepayAmount.focus();
}

elements.btnCloseSuppRepayModal.addEventListener('click', () => elements.suppRepayModal.classList.remove('active'));
elements.btnCancelSuppRepayModal.addEventListener('click', () => elements.suppRepayModal.classList.remove('active'));

elements.suppRepayForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  const id = elements.suppRepayId.value;
  const amount = Number(elements.suppRepayAmount.value);
  const note = elements.suppRepayNote.value;

  try {
    await fetchAPI(`/api/suppliers/${id}/pay`, {
      method: 'POST',
      body: JSON.stringify({ amount, note })
    });
    showToast("✔ تم تسجيل تسديد الدفعة للممون بنجاح");
    elements.suppRepayModal.classList.remove('active');
    await loadSuppliers();
  } catch (err) {
    console.error(err);
  }
});

// Supplier Statement Modal (كشف حساب الممون)
async function openSuppStatementModal(suppId) {
  try {
    const res = await fetchAPI(`/api/suppliers/${suppId}/history`);
    elements.suppStmtName.textContent = res.supplier.name;
    elements.suppStmtCompany.textContent = res.supplier.company ? `الشركة: ${res.supplier.company}` : '';
    elements.suppStmtDebt.textContent = formatMoney(res.supplier.debt);

    const timeline = [];
    (res.purchases || []).forEach(p => {
      timeline.push({
        date: p.dateStr,
        desc: `فاتورة توريد وشراء (${p.invoiceNumber})`,
        amount: p.total,
        paid: p.paidAmount,
        debt: p.debtAmount,
        type: 'purchase'
      });
    });
    (res.payments || []).forEach(pm => {
      timeline.push({
        date: pm.dateStr,
        desc: `تسديد نقدي للممون: ${pm.note || ''}`,
        amount: 0,
        paid: pm.amount,
        debt: -pm.amount,
        type: 'payment'
      });
    });

    timeline.sort((a, b) => b.date.localeCompare(a.date));

    if (timeline.length === 0) {
      elements.suppStmtHistoryBody.innerHTML = `
        <tr><td colspan="5" style="text-align: center; padding: 1.5rem;">لا توجد حركات مسجلة لهذا الممون</td></tr>
      `;
    } else {
      elements.suppStmtHistoryBody.innerHTML = timeline.map(t => `
        <tr>
          <td>${t.date}</td>
          <td style="font-weight: 600;">${t.desc}</td>
          <td>${t.amount ? formatMoney(t.amount) : '—'}</td>
          <td style="color: var(--primary); font-weight: 700;">${formatMoney(t.paid)}</td>
          <td style="color: ${t.debt > 0 ? 'var(--danger)' : 'var(--secondary)'}; font-weight: 700;">
            ${t.debt > 0 ? '+' + formatMoney(t.debt) : (t.debt < 0 ? '-' + formatMoney(Math.abs(t.debt)) : '0')}
          </td>
        </tr>
      `).join('');
    }

    elements.suppStatementModal.classList.add('active');
  } catch (err) {
    console.error(err);
  }
}

elements.btnCloseSuppStatementModal.addEventListener('click', () => elements.suppStatementModal.classList.remove('active'));
elements.btnCloseSuppStmtBtn.addEventListener('click', () => elements.suppStatementModal.classList.remove('active'));

// Purchases List & New Purchase Order
async function loadPurchases() {
  try {
    const list = await fetchAPI('/api/purchases');
    state.purchases = list;
    renderPurchasesTable();
  } catch (err) {
    console.error(err);
  }
}

function renderPurchasesTable() {
  if (state.purchases.length === 0) {
    elements.purchasesTableBody.innerHTML = `
      <tr><td colspan="9" style="text-align: center; padding: 1.5rem; color: var(--text-muted);">لا توجد فواتير شراء مسجلة</td></tr>
    `;
    return;
  }

  elements.purchasesTableBody.innerHTML = state.purchases.map(p => {
    const itemsDesc = (p.items || []).map(i => `${i.name} × ${i.quantity}`).join('، ');
    return `
      <tr>
        <td style="font-family: monospace; font-weight: 700; color: var(--secondary);">${p.invoiceNumber}</td>
        <td style="font-size: 0.85rem; color: var(--text-muted);">${p.dateStr}</td>
        <td style="font-weight: 700;">${p.supplierName}</td>
        <td style="font-size: 0.85rem; max-width: 250px; text-overflow: ellipsis; overflow: hidden; white-space: nowrap;">${itemsDesc}</td>
        <td style="font-weight: 700;">${formatMoney(p.total)}</td>
        <td style="color: var(--primary); font-weight: 700;">${formatMoney(p.paidAmount)}</td>
        <td style="color: ${p.debtAmount > 0 ? 'var(--danger)' : 'var(--text-muted)'}; font-weight: 700;">${p.debtAmount > 0 ? formatMoney(p.debtAmount) : '—'}</td>
        <td><span class="badge ${p.paymentMethod === 'cash' ? 'badge-success' : 'badge-warning'}">${p.paymentMethod === 'cash' ? 'نقداً' : (p.paymentMethod === 'credit' ? 'دين' : 'جزئي')}</span></td>
        <td style="font-size: 0.8rem; color: var(--text-muted);">${p.notes || '—'}</td>
      </tr>
    `;
  }).join('');
}

// New Purchase Order Modal
elements.btnOpenNewPurchase.addEventListener('click', () => {
  populateSupplierSelects();
  populatePurchaseProductSelects();
  elements.purchQty.value = 10;
  elements.purchPaidAmount.value = '';
  elements.purchNotes.value = '';
  calculatePurchaseTotal();
  elements.purchaseModal.classList.add('active');
});

elements.btnClosePurchaseModal.addEventListener('click', () => elements.purchaseModal.classList.remove('active'));
elements.btnCancelPurchaseModal.addEventListener('click', () => elements.purchaseModal.classList.remove('active'));

function calculatePurchaseTotal() {
  const qty = parseInt(elements.purchQty.value, 10) || 0;
  const unitPrice = parseFloat(elements.purchUnitPrice.value) || 0;
  const total = qty * unitPrice;
  elements.purchItemTotal.textContent = formatMoney(total);
  if (elements.purchPaymentMethod.value === 'cash') {
    elements.purchPaidAmount.value = total;
  }
}

elements.purchProductSelect.addEventListener('change', () => {
  const selectedOpt = elements.purchProductSelect.options[elements.purchProductSelect.selectedIndex];
  if (selectedOpt) {
    elements.purchUnitPrice.value = selectedOpt.dataset.buy || 0;
    calculatePurchaseTotal();
  }
});
elements.purchQty.addEventListener('input', calculatePurchaseTotal);
elements.purchUnitPrice.addEventListener('input', calculatePurchaseTotal);
elements.purchPaymentMethod.addEventListener('change', () => {
  const m = elements.purchPaymentMethod.value;
  if (m === 'credit') {
    elements.purchPaidAmount.value = 0;
    elements.purchPaidAmount.disabled = true;
  } else {
    elements.purchPaidAmount.disabled = false;
    calculatePurchaseTotal();
  }
});

elements.purchaseForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  const supplierId = parseInt(elements.purchSupplierSelect.value, 10);
  const productId = parseInt(elements.purchProductSelect.value, 10);
  const qty = parseInt(elements.purchQty.value, 10);
  const unitBuy = parseFloat(elements.purchUnitPrice.value);
  const method = elements.purchPaymentMethod.value;
  const total = qty * unitBuy;
  const paid = method === 'credit' ? 0 : (parseFloat(elements.purchPaidAmount.value) || 0);

  const payload = {
    supplierId,
    paymentMethod: method,
    paidAmount: paid,
    notes: elements.purchNotes.value.trim(),
    items: [
      {
        productId,
        quantity: qty,
        buyPrice: unitBuy
      }
    ]
  };

  try {
    await fetchAPI('/api/purchases', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    showToast("✔ تم تسجيل فاتورة الشراء وزيادة كمية المخزون");
    elements.purchaseModal.classList.remove('active');
    await Promise.all([loadProducts(), loadSuppliers(), loadPurchases()]);
  } catch (err) {
    console.error(err);
  }
});

// ----------------- INVENTORY MANAGEMENT TAB -----------------

function renderInventoryTable() {
  const query = elements.invSearchInput.value.trim().toLowerCase();
  const cat = elements.invCategoryFilter.value;
  let list = state.products;

  if (cat !== 'all') {
    list = list.filter(p => p.category === cat);
  }
  if (query) {
    list = list.filter(p => 
      p.name.toLowerCase().includes(query) || 
      (p.barcode && p.barcode.toLowerCase().includes(query))
    );
  }

  if (list.length === 0) {
    elements.inventoryTableBody.innerHTML = `
      <tr>
        <td colspan="9" style="text-align: center; padding: 2rem; color: var(--text-muted);">
          لا توجد منتجات مطابقة للبحث
        </td>
      </tr>
    `;
    return;
  }

  const categoryNames = {
    food: "مواد غذائية",
    drinks: "مشروبات وعصائر",
    dairy: "ألبان وأجبان",
    sweets: "حلويات وبسكويت",
    cleaning: "منظفات ومستلزمات"
  };

  elements.inventoryTableBody.innerHTML = list.map(p => {
    const profitMargin = p.sellPrice - p.buyPrice;
    const profitPercent = p.buyPrice > 0 ? Math.round((profitMargin / p.buyPrice) * 100) : 0;
    const isOut = p.stock <= 0;
    const isLow = p.stock <= (p.minStock || 5) && !isOut;

    return `
      <tr>
        <td style="font-family: monospace; font-size: 0.85rem; color: var(--text-muted);">${p.barcode || '—'}</td>
        <td style="font-weight: 600;">${p.name}</td>
        <td><span class="badge badge-info">${categoryNames[p.category] || p.category}</span></td>
        <td>${formatMoney(p.buyPrice)}</td>
        <td style="font-weight: 700; color: var(--primary);">${formatMoney(p.sellPrice)}</td>
        <td style="color: var(--secondary); font-size: 0.85rem;">+${profitMargin} (${profitPercent}%)</td>
        <td style="font-weight: 700; font-size: 1rem;">${p.stock}</td>
        <td>
          ${isOut ? '<span class="badge badge-danger">نفد المخزون</span>' : ''}
          ${isLow ? '<span class="badge badge-warning">مخزون منخفض</span>' : ''}
          ${!isOut && !isLow ? '<span class="badge badge-success">متوفر</span>' : ''}
        </td>
        <td>
          <div style="display: flex; gap: 0.4rem;">
            <button class="btn btn-secondary btn-edit-prod" data-id="${p.id}" style="padding: 0.3rem 0.6rem; font-size: 0.8rem;">تعديل</button>
            <button class="btn btn-danger btn-del-prod" data-id="${p.id}" style="padding: 0.3rem 0.6rem; font-size: 0.8rem;">حذف</button>
          </div>
        </td>
      </tr>
    `;
  }).join('');

  elements.inventoryTableBody.querySelectorAll('.btn-edit-prod').forEach(b => {
    b.addEventListener('click', () => openEditProductModal(parseInt(b.dataset.id, 10)));
  });
  elements.inventoryTableBody.querySelectorAll('.btn-del-prod').forEach(b => {
    b.addEventListener('click', () => deleteProduct(parseInt(b.dataset.id, 10)));
  });
}

elements.invSearchInput.addEventListener('input', renderInventoryTable);
elements.invCategoryFilter.addEventListener('change', renderInventoryTable);

let lowStockFilterActive = false;
elements.btnFilterLowStock.addEventListener('click', () => {
  lowStockFilterActive = !lowStockFilterActive;
  elements.btnFilterLowStock.classList.toggle('btn-primary', lowStockFilterActive);
  elements.btnFilterLowStock.classList.toggle('btn-secondary', !lowStockFilterActive);
  if (lowStockFilterActive) {
    state.products = state.products.filter(p => p.stock <= (p.minStock || 5));
  } else {
    loadProducts();
  }
  renderInventoryTable();
});

// Product Add/Edit Modal
elements.btnOpenAddProduct.addEventListener('click', () => {
  elements.productModalTitle.textContent = "إضافة منتج جديد";
  elements.prodId.value = "";
  elements.productForm.reset();
  elements.prodMinStock.value = "5";
  elements.productModal.classList.add('active');
  elements.prodName.focus();
});

function openEditProductModal(productId) {
  const p = state.products.find(x => x.id === productId);
  if (!p) return;

  elements.productModalTitle.textContent = "تعديل بيانات المنتج";
  elements.prodId.value = p.id;
  elements.prodName.value = p.name;
  elements.prodBarcode.value = p.barcode || "";
  elements.prodCategory.value = p.category || "food";
  elements.prodBuyPrice.value = p.buyPrice;
  elements.prodSellPrice.value = p.sellPrice;
  elements.prodStock.value = p.stock;
  elements.prodMinStock.value = p.minStock || 5;

  elements.productModal.classList.add('active');
  elements.prodName.focus();
}

function closeProductModal() {
  elements.productModal.classList.remove('active');
}
elements.btnCloseProductModal.addEventListener('click', closeProductModal);
elements.btnCancelProductModal.addEventListener('click', closeProductModal);

elements.productForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  const id = elements.prodId.value;
  const payload = {
    name: elements.prodName.value.trim(),
    barcode: elements.prodBarcode.value.trim(),
    category: elements.prodCategory.value,
    buyPrice: Number(elements.prodBuyPrice.value),
    sellPrice: Number(elements.prodSellPrice.value),
    stock: Number(elements.prodStock.value),
    minStock: Number(elements.prodMinStock.value)
  };

  try {
    if (id) {
      await fetchAPI(`/api/products/${id}`, {
        method: 'PUT',
        body: JSON.stringify(payload)
      });
      showToast("تم تحديث المنتج بنجاح");
    } else {
      await fetchAPI('/api/products', {
        method: 'POST',
        body: JSON.stringify(payload)
      });
      showToast("تمت إضافة المنتج بنجاح");
    }
    closeProductModal();
    await loadProducts();
  } catch (err) {
    console.error(err);
  }
});

async function deleteProduct(productId) {
  const p = state.products.find(x => x.id === productId);
  if (!p) return;

  if (confirm(`هل أنت متأكد من حذف المنتج "${p.name}"؟`)) {
    try {
      await fetchAPI(`/api/products/${productId}`, { method: 'DELETE' });
      showToast("تم حذف المنتج");
      await loadProducts();
    } catch (err) {
      console.error(err);
    }
  }
}

// ----------------- REGISTER & CASH DRAWER (يومية الصندوق) -----------------

async function loadRegister() {
  try {
    const reg = await fetchAPI('/api/register/today');
    state.registerData = reg;

    elements.regTodayDate.textContent = reg.date;
    elements.regOpeningFloat.textContent = formatMoney(reg.openingFloat);
    elements.regCashSales.textContent = formatMoney(reg.cashSalesTotal);
    elements.regDebtCollections.textContent = formatMoney(reg.debtCollections);
    elements.regSupplierPaid.textContent = `-${formatMoney(reg.supplierPaidCash || 0)}`;
    elements.regCreditSales.textContent = formatMoney(reg.creditSalesTotal);
    elements.regExpectedCash.textContent = formatMoney(reg.expectedCashInDrawer);
    elements.regInvoicesCount.textContent = `${reg.invoicesCount} فاتورة (و ${reg.paymentsCount} تسديد)`;
    elements.regTodayProfit.textContent = formatMoney(reg.todayProfit);
  } catch (err) {
    console.error(err);
  }
}

elements.btnRefreshRegister.addEventListener('click', loadRegister);

elements.btnPrintZReport.addEventListener('click', () => {
  if (!state.registerData) return;
  const reg = state.registerData;

  const zHtml = `
    <div style="font-family: monospace; padding: 20px; width: 300px; margin: 0 auto; line-height: 1.5;">
      <div style="text-align: center; border-bottom: 1px dashed #000; padding-bottom: 8px;">
        <h2 style="margin: 0; font-size: 16px;">تقرير إغلاق الصندوق (Z)</h2>
        <div>${state.settings.storeName}</div>
        <div>التاريخ: ${reg.date}</div>
      </div>
      <div style="padding: 10px 0; border-bottom: 1px dashed #000;">
        <div>الرصيد الافتتاحي: ${formatMoney(reg.openingFloat)}</div>
        <div>المبيعات النقدية: ${formatMoney(reg.cashSalesTotal)}</div>
        <div>تحصيل ديون الزبائن: ${formatMoney(reg.debtCollections)}</div>
        <div>مدفوعات الممونين نقدية: -${formatMoney(reg.supplierPaidCash || 0)}</div>
        <div style="font-weight: bold; margin-top: 6px;">إجمالي المقبوضات النقدية: ${formatMoney(reg.cashSalesTotal + reg.debtCollections - (reg.supplierPaidCash || 0))}</div>
        <div style="margin-top: 4px; color: #666;">مبيعات بالآجل (ديون زبائن): ${formatMoney(reg.creditSalesTotal)}</div>
      </div>
      <div style="padding: 10px 0; border-bottom: 1px dashed #000; font-weight: bold; font-size: 14px;">
        <div>النقد الواجب توفره في الصندوق: ${formatMoney(reg.expectedCashInDrawer)}</div>
      </div>
      <div style="padding: 8px 0; font-size: 11px;">
        <div>عدد فواتير البيع: ${reg.invoicesCount}</div>
        <div>صافي أرباح اليوم: ${formatMoney(reg.todayProfit)}</div>
      </div>
      <div style="text-align: center; margin-top: 15px; font-size: 10px;">
        *** نهاية تقرير اليومية ***
      </div>
    </div>
  `;

  const win = window.open('', '_blank');
  win.document.write(`<html><head><title>Z-Report</title></head><body dir="rtl">${zHtml}</body></html>`);
  win.document.close();
  win.focus();
  win.print();
});

// ----------------- STATS & DASHBOARD TAB -----------------

async function loadStats() {
  try {
    const stats = await fetchAPI('/api/stats');
    state.stats = stats;

    elements.kpiTodaySales.textContent = formatMoney(stats.today.total);
    elements.kpiTodayCount.textContent = `${stats.today.count} فواتير`;
    elements.kpiTodayProfit.textContent = formatMoney(stats.today.profit);
    elements.kpiTotalSales.textContent = formatMoney(stats.overall.totalSales);
    elements.kpiTotalInvoices.textContent = `${stats.overall.totalSalesCount} عملية بيع (متوسط: ${formatMoney(stats.overall.averageTicket)})`;
    elements.kpiStockAlerts.textContent = stats.inventory.lowStockCount + stats.inventory.outOfStockCount;

    elements.statTotalItems.textContent = stats.inventory.totalProducts;
    elements.statBuyVal.textContent = formatMoney(stats.inventory.buyValue);
    elements.statSellVal.textContent = formatMoney(stats.inventory.sellValue);
    elements.statPotentialProfit.textContent = formatMoney(stats.inventory.potentialProfit);

    renderCharts(stats);
  } catch (err) {
    console.error(err);
  }
}

elements.btnRefreshStats.addEventListener('click', loadStats);

function renderCharts(stats) {
  if (typeof Chart === 'undefined') return;

  const ctxTimeline = document.getElementById('salesTimelineChart').getContext('2d');
  if (state.charts.timeline) state.charts.timeline.destroy();

  const labels = stats.last7Days.map(d => d.label);
  const salesData = stats.last7Days.map(d => d.total);
  const profitData = stats.last7Days.map(d => d.profit);

  state.charts.timeline = new Chart(ctxTimeline, {
    type: 'bar',
    data: {
      labels: labels,
      datasets: [
        {
          label: 'المبيعات (دج)',
          data: salesData,
          backgroundColor: '#059669',
          borderRadius: 6
        },
        {
          label: 'الأرباح (دج)',
          data: profitData,
          backgroundColor: '#0284c7',
          borderRadius: 6
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { position: 'top', rtl: true, labels: { font: { family: 'Segoe UI' } } }
      },
      scales: {
        y: { beginAtZero: true, grid: { color: '#f1f5f9' } },
        x: { grid: { display: false } }
      }
    }
  });

  const ctxTop = document.getElementById('topProductsChart').getContext('2d');
  if (state.charts.topProducts) state.charts.topProducts.destroy();

  const topLabels = stats.topProducts.map(p => p.name.length > 15 ? p.name.slice(0, 15) + '...' : p.name);
  const topCounts = stats.topProducts.map(p => p.count);

  state.charts.topProducts = new Chart(ctxTop, {
    type: 'doughnut',
    data: {
      labels: topLabels.length ? topLabels : ['لا توجد مبيعات بعد'],
      datasets: [{
        data: topCounts.length ? topCounts : [1],
        backgroundColor: [
          '#059669', '#0284c7', '#f59e0b', '#8b5cf6', '#ec4899', '#14b8a6'
        ]
      }]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: { position: 'bottom', rtl: true, labels: { boxWidth: 12, font: { family: 'Segoe UI', size: 11 } } }
      }
    }
  });
}

// ----------------- SETTINGS & BACKUP TAB -----------------

function populateSettingsForm() {
  elements.setStoreName.value = state.settings.storeName || '';
  elements.setPhone.value = state.settings.phone || '';
  elements.setCurrency.value = state.settings.currency || 'دج';
  elements.setAddress.value = state.settings.address || '';
  elements.setReceiptFooter.value = state.settings.receiptFooter || '';
}

elements.settingsForm.addEventListener('submit', async (e) => {
  e.preventDefault();
  const payload = {
    storeName: elements.setStoreName.value.trim(),
    phone: elements.setPhone.value.trim(),
    currency: elements.setCurrency.value.trim() || 'دج',
    address: elements.setAddress.value.trim(),
    receiptFooter: elements.setReceiptFooter.value.trim()
  };

  try {
    const res = await fetchAPI('/api/settings', {
      method: 'POST',
      body: JSON.stringify(payload)
    });
    state.settings = res.settings;
    elements.headerStoreName.textContent = res.settings.storeName;
    showToast("✔ تم حفظ الإعدادات بنجاح");
  } catch (err) {
    console.error(err);
  }
});

elements.importFileInput.addEventListener('change', async (e) => {
  const file = e.target.files[0];
  if (!file) return;

  const reader = new FileReader();
  reader.onload = async (evt) => {
    try {
      const data = JSON.parse(evt.target.result);
      await fetchAPI('/api/import', {
        method: 'POST',
        body: JSON.stringify(data)
      });
      showToast("✔ تم استيراد قاعدة البيانات بنجاح!");
      await initApp();
      switchTab('pos');
    } catch (err) {
      showToast("ملف غير صالح أو تالف", true);
    }
  };
  reader.readAsText(file);
});

elements.btnResetDemoData.addEventListener('click', async () => {
  if (confirm("هل تريد استعادة البيانات النموذجية الافتراضية؟ سيتم تحديث المنتجات والزبائن والممونين.")) {
    try {
      await fetchAPI('/api/reset-demo', { method: 'POST' });
      showToast("✔ تمت استعادة البيانات النموذجية");
      await initApp();
      switchTab('pos');
    } catch (err) {
      console.error(err);
    }
  }
});

// Start application
initApp();
